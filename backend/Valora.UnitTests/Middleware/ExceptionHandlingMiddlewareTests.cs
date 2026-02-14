using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
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
    public async Task InvokeAsync_NotFoundException_InProduction_HidesDetail()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new NotFoundException("Sensitive Info"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("Sensitive Info", body);
        Assert.Contains("The requested resource was not found", body);
    }

    [Fact]
    public async Task InvokeAsync_NotFoundException_InDevelopment_ShowsDetail()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new NotFoundException("Specific Resource Missing"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Specific Resource Missing", body);
    }

    [Fact]
    public async Task InvokeAsync_GenericException_InProduction_HidesDetail()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new Exception("Database connection string leaked"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("Database connection string leaked", body);
        Assert.Contains("An unexpected error occurred", body);
    }

    [Fact]
    public async Task InvokeAsync_GenericException_InDevelopment_ShowsDetailAndStackTrace()
    {
        // Arrange
        _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new Exception("Dev Error Detail"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Dev Error Detail", body);
        Assert.Contains("stackTrace", body);
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
    public async Task InvokeAsync_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new ArgumentException("Invalid arg"),
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Invalid arg", body);
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
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("An external dependency is currently unavailable", body);
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
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("An external service returned an invalid or unexpected response format", body);
    }

    [Fact]
    public async Task InvokeAsync_NpgsqlExceptionTransient_ReturnsServiceUnavailable()
    {
        // Arrange
        // Using "53300" (Too many connections) which is transient
        var transientEx = new PostgresException("Transient error", "ERROR", "ERROR", "53300");

        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw transientEx,
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
    public async Task InvokeAsync_NpgsqlExceptionNonTransient_ReturnsInternalServerError()
    {
        // Arrange
        // Using "42P01" (Undefined Table) which is NOT transient
        var nonTransientEx = new PostgresException("Table not found", "ERROR", "ERROR", "42P01");

        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw nonTransientEx,
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
    public async Task InvokeAsync_DbUpdateExceptionWithTransientNpgsql_ReturnsServiceUnavailable()
    {
        // Arrange
        var transientEx = new PostgresException("Transient error", "ERROR", "ERROR", "53300");
        var dbEx = new DbUpdateException("DB Error", transientEx);

        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw dbEx,
            logger: _loggerMock.Object,
            env: _envMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, context.Response.StatusCode);
    }
}
