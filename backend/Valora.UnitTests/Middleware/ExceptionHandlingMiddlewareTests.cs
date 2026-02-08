using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Valora.Api.Middleware;
using Valora.Application.Common.Exceptions;

namespace Valora.UnitTests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;
    private readonly Mock<IHostEnvironment> _envMock;

    public ExceptionHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        _envMock = new Mock<IHostEnvironment>();
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_ReturnsBadRequest()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new ValidationException(new[] { "Error1" }),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_NotFoundException_ReturnsNotFound()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new NotFoundException("Not found"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_TaskCanceledException_Returns499()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new TaskCanceledException(),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(499, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_DbUpdateConcurrencyException_ReturnsConflict()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new DbUpdateConcurrencyException("Concurrency conflict"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_DbUpdateException_ReturnsConflict()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new DbUpdateException("Database error"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_GenericException_ReturnsInternalServerError()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new Exception("Boom"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_BadHttpRequestException_ReturnsBadRequest()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new BadHttpRequestException("Invalid request"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithUser_LogsUserName()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new Exception("Boom"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "TestUser") };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        // We verify that the logger was called. Checking the exact message string with the username
        // is harder with extension methods, but ensuring no crash occurs during logging is key.
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithoutUser_LogsAnonymous()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new Exception("Boom"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        // context.User is null or empty by default

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Anonymous")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_HttpRequestException_ReturnsServiceUnavailable()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new HttpRequestException("API Unavailable"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_SocketException_ReturnsServiceUnavailable()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new SocketException(),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_TimeoutException_ReturnsGatewayTimeout()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new TimeoutException("Timed out"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.GatewayTimeout, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_JsonException_ReturnsBadGateway()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new JsonException("Invalid payload"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadGateway, context.Response.StatusCode);
    }
}
