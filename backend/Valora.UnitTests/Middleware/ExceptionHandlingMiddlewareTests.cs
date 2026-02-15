using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Valora.Api.Middleware;
using Valora.Application.Common.Exceptions;
using Xunit;

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

    private ExceptionHandlingMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new ExceptionHandlingMiddleware(next, _loggerMock.Object, _envMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNext()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware((innerHttpContext) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(new DefaultHttpContext());

        Assert.True(nextCalled);
    }

    [Theory]
    [InlineData(typeof(ValidationException), HttpStatusCode.BadRequest)]
    [InlineData(typeof(NotFoundException), HttpStatusCode.NotFound)]
    [InlineData(typeof(UnauthorizedAccessException), HttpStatusCode.Unauthorized)]
    [InlineData(typeof(OperationCanceledException), (HttpStatusCode)499)]
    [InlineData(typeof(BadHttpRequestException), HttpStatusCode.BadRequest)]
    [InlineData(typeof(ArgumentException), HttpStatusCode.BadRequest)]
    public async Task InvokeAsync_4xxExceptions_LogsWarning(Type exceptionType, HttpStatusCode expectedStatusCode)
    {
        var exception = CreateException(exceptionType);
        var middleware = CreateMiddleware((innerHttpContext) => throw exception);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal((int)expectedStatusCode, context.Response.StatusCode);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(typeof(HttpRequestException), HttpStatusCode.ServiceUnavailable)]
    [InlineData(typeof(SocketException), HttpStatusCode.ServiceUnavailable)]
    [InlineData(typeof(TimeoutException), HttpStatusCode.GatewayTimeout)]
    [InlineData(typeof(JsonException), HttpStatusCode.BadGateway)]
    public async Task InvokeAsync_5xxExceptions_LogsError(Type exceptionType, HttpStatusCode expectedStatusCode)
    {
        var exception = CreateException(exceptionType);
        var middleware = CreateMiddleware((innerHttpContext) => throw exception);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal((int)expectedStatusCode, context.Response.StatusCode);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private Exception CreateException(Type type)
    {
        if (type == typeof(ValidationException)) return new ValidationException(new Dictionary<string, string[]>());
        if (type == typeof(BadHttpRequestException)) return new BadHttpRequestException("bad");
        if (type == typeof(SocketException)) return new SocketException();
        return (Exception)Activator.CreateInstance(type)!;
    }

    [Fact]
    public async Task InvokeAsync_DbUpdateException_Transient_ReturnsServiceUnavailable()
    {
        var transientEx = new PostgresException("Transient error", "ERROR", "ERROR", "53300");
        var dbEx = new DbUpdateException("DB Error", transientEx);
        var middleware = CreateMiddleware((innerHttpContext) => throw dbEx);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, context.Response.StatusCode);
        _loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), dbEx, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_DbUpdateException_NonTransient_ReturnsConflict()
    {
        var dbEx = new DbUpdateException("DB Error");
        var middleware = CreateMiddleware((innerHttpContext) => throw dbEx);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
        _loggerMock.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), dbEx, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_NpgsqlException_Transient_ReturnsServiceUnavailable()
    {
        var transientEx = new PostgresException("Transient error", "ERROR", "ERROR", "53300");
        var middleware = CreateMiddleware((innerHttpContext) => throw transientEx);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_GenericException_ReturnsInternalServerError()
    {
        var exception = new Exception("Boom");
        var middleware = CreateMiddleware((innerHttpContext) => throw exception);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithUser_LogsUserId()
    {
        var exception = new Exception("Boom");
        var middleware = CreateMiddleware((innerHttpContext) => throw exception);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "TestId") };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

        await middleware.InvokeAsync(context);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestId")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Development_IncludesStackTrace()
    {
        _envMock.Setup(m => m.EnvironmentName).Returns(Environments.Development);
        var exception = new Exception("Boom");
        var middleware = CreateMiddleware((innerHttpContext) => throw exception);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("stackTrace", body);
    }
}
