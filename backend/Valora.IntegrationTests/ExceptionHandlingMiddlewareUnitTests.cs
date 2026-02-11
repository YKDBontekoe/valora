using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
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

    private class MockHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Valora";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = default!;
    }
}
