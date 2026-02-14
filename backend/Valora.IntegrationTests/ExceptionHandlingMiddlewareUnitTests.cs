using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Valora.Api.Middleware;
using Xunit;

namespace Valora.IntegrationTests;

public class ExceptionHandlingMiddlewareUnitTests
{
    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ReturnsProblemDetails()
    {
        // Arrange
        var environment = new MockHostEnvironment { EnvironmentName = "Development" };
        var logger = NullLogger<ExceptionHandlingMiddleware>.Instance;
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerContext) => throw new Exception("Test exception"),
            logger: logger,
            env: environment
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Contains("Test exception", body);
        Assert.Contains("traceId", body); // Should be present in Development
    }

    [Fact]
    public async Task InvokeAsync_WhenProduction_SanitizesError()
    {
        // Arrange
        var environment = new MockHostEnvironment { EnvironmentName = "Production" };
        var logger = NullLogger<ExceptionHandlingMiddleware>.Instance;
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerContext) => throw new Exception("Secret exception"),
            logger: logger,
            env: environment
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.DoesNotContain("Secret exception", body);
        Assert.Contains("An unexpected error occurred. Please try again later.", body);
    }


    [Fact]
    public async Task InvokeAsync_WhenBadHttpRequestInProduction_ReturnsGenericBadRequestDetail()
    {
        // Arrange
        var environment = new MockHostEnvironment { EnvironmentName = "Production" };
        var logger = NullLogger<ExceptionHandlingMiddleware>.Instance;
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerContext) => throw new BadHttpRequestException("Failed to parse body at byte position 42."),
            logger: logger,
            env: environment
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problemDetails);
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Equal("Bad Request", problemDetails!.Title);
        Assert.Equal("The request payload is invalid.", problemDetails.Detail);
        Assert.DoesNotContain("Failed to parse body", body);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_ReturnsValidationErrors()
    {
        // Arrange
        var environment = new MockHostEnvironment { EnvironmentName = "Production" };
        var logger = NullLogger<ExceptionHandlingMiddleware>.Instance;
        var validationErrors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required."]
        };

        var middleware = new ExceptionHandlingMiddleware(
            next: (innerContext) => throw new Valora.Application.Common.Exceptions.ValidationException(validationErrors),
            logger: logger,
            env: environment
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        using var document = JsonDocument.Parse(body);

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Equal("Validation Error", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("One or more validation errors occurred.", document.RootElement.GetProperty("detail").GetString());
        Assert.True(document.RootElement.TryGetProperty("errors", out var errorsElement));
        Assert.Equal("Email is required.", errorsElement.GetProperty("Email")[0].GetString());
    }

    private class MockHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Valora";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = default!;
    }
}
