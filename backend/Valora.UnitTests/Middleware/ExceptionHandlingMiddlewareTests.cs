using System.Net;
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
}
