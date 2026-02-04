using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            _logger.LogError(ex, "An unhandled exception occurred processing {Method} {Path}. RequestId: {RequestId}",
                context.Request.Method, context.Request.Path, context.TraceIdentifier);
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
                errors = validationEx.Errors;
                break;
            case NotFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                title = "Not Found";
                break;
            case TaskCanceledException:
            case OperationCanceledException:
                statusCode = 499; // Client Closed Request
                title = "Request Cancelled";
                detail = "The request was cancelled.";
                break;
            case DbUpdateConcurrencyException:
                statusCode = (int)HttpStatusCode.Conflict;
                title = "Concurrency Conflict";
                detail = "The resource you are attempting to update has been modified by another user.";
                break;
            case DbUpdateException:
                statusCode = (int)HttpStatusCode.Conflict;
                title = "Database Error";
                detail = "A database constraint violation occurred.";
                break;
            case BadHttpRequestException:
                statusCode = (int)HttpStatusCode.BadRequest;
                title = "Bad Request";
                break;
        }

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = _env.IsDevelopment() ? detail : (statusCode == 500 ? "An error occurred while processing your request." : detail),
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        if (errors != null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        if (_env.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }
}
