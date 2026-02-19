using System.Security.Claims;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Sentry;
using Valora.Application.Common.Exceptions;

namespace Valora.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var statusCode = (int)HttpStatusCode.InternalServerError;
        var title = "An internal error occurred";
        var detail = exception.Message;
        IDictionary<string, string[]>? errors = null;

        switch (exception)
        {
            case ValidationException validationEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                title = "Validation Error";
                detail = "One or more validation errors occurred.";
                errors = validationEx.Errors;
                break;
            case NotFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                title = "Resource Not Found";
                // Hide specific not found details in production to prevent enumeration/leakage
                detail = _env.IsProduction() ? "The requested resource was not found." : exception.Message;
                break;
            case UnauthorizedAccessException:
                statusCode = (int)HttpStatusCode.Unauthorized;
                title = "Unauthorized";
                detail = "You are not authorized to access this resource.";
                break;
            case TaskCanceledException:
            case OperationCanceledException:
                statusCode = 499; // Client Closed Request
                title = "Request Cancelled";
                detail = "The request was cancelled by the client.";
                break;
            case DbUpdateConcurrencyException:
                statusCode = (int)HttpStatusCode.Conflict;
                title = "Concurrency Conflict";
                detail = "The resource has been modified by another user.";
                break;
            case DbUpdateException dbEx:
                if (dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    statusCode = (int)HttpStatusCode.Conflict;
                    title = "Already Exists";
                    detail = "A record with the same unique identifier already exists.";
                }
                else if (dbEx.InnerException is NpgsqlException innerNpgsqlEx && innerNpgsqlEx.IsTransient)
                {
                    statusCode = (int)HttpStatusCode.ServiceUnavailable;
                    title = "Service Unavailable";
                    detail = "The service is temporarily unavailable due to database connectivity issues.";
                }
                else
                {
                    statusCode = (int)HttpStatusCode.Conflict;
                    title = "Database Conflict";
                    detail = "A database constraint violation occurred.";
                }
                break;
            case NpgsqlException npgsqlEx:
                if (npgsqlEx.IsTransient)
                {
                    statusCode = (int)HttpStatusCode.ServiceUnavailable;
                    title = "Service Unavailable";
                    detail = "The service is temporarily unavailable due to database connectivity issues.";
                }
                else
                {
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    title = "Database Error";
                    detail = "A database error occurred.";
                }
                break;
            case BadHttpRequestException:
                statusCode = (int)HttpStatusCode.BadRequest;
                title = "Bad Request";
                detail = "The request payload is invalid.";
                break;
            case ArgumentException:
                statusCode = (int)HttpStatusCode.BadRequest;
                title = "Invalid Argument";
                detail = exception.Message;
                break;
            case HttpRequestException:
            case SocketException:
                statusCode = (int)HttpStatusCode.ServiceUnavailable;
                title = "External Service Error";
                detail = "An external dependency is currently unavailable. Please try again later.";
                break;
            case TimeoutException:
                statusCode = (int)HttpStatusCode.GatewayTimeout;
                title = "Request Timeout";
                detail = "The request timed out waiting for an upstream service.";
                break;
            case JsonException:
                statusCode = (int)HttpStatusCode.BadGateway;
                title = "Upstream Response Error";
                detail = "An external service returned an invalid or unexpected response format.";
                break;
            default:
                // For unhandled exceptions, hide details in production
                statusCode = (int)HttpStatusCode.InternalServerError;
                title = "Internal Server Error";
                detail = _env.IsDevelopment() ? exception.Message : "An unexpected error occurred. Please try again later.";
                break;
        }

        context.Response.StatusCode = statusCode;

        // Add breadcrumb for the handled error
        SentrySdk.AddBreadcrumb(
            message: $"Request failed with status {statusCode}: {title}",
            category: "exception.handler",
            level: statusCode >= 500 ? BreadcrumbLevel.Error : BreadcrumbLevel.Info);

        // Log and capture exceptions
        if (statusCode >= 500)
        {
            var user = context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
            _logger.LogError(exception, "Unhandled exception: {ExceptionType} processing {Method} {Path} for user {User}. RequestId: {RequestId}",
                exception.GetType().Name, context.Request.Method, context.Request.Path, user, context.TraceIdentifier);

            // Explicitly capture in Sentry since the middleware "handles" it (swallows it)
            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("status_code", statusCode.ToString());
                scope.SetTag("exception_type", exception.GetType().Name);
                scope.SetExtra("request_id", context.TraceIdentifier);
            });
            SentrySdk.CaptureException(exception);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with {StatusCode}: {ExceptionType} processing {Method} {Path}",
                statusCode, exception.GetType().Name, context.Request.Method, context.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = "https://httpstatuses.com/" + statusCode
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        problemDetails.Extensions["requestId"] = context.TraceIdentifier;

        if (errors != null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        if (_env.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.ToString();
        }

        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }
}
