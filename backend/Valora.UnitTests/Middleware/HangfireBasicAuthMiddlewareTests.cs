using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Valora.Api.Middleware;
using Xunit;

namespace Valora.UnitTests.Middleware;

public class HangfireBasicAuthMiddlewareTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly DefaultHttpContext _context;

    public HangfireBasicAuthMiddlewareTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _context = new DefaultHttpContext();
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn401_WhenApiKeyIsNotConfigured()
    {
        // Arrange
        _mockConfig.Setup(c => c["ApiKey"]).Returns((string?)null);
        var middleware = new HangfireBasicAuthMiddleware(next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(_context, _mockConfig.Object);

        // Assert
        Assert.Equal(401, _context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn401_WhenAuthorizationHeaderIsMissing()
    {
        // Arrange
        _mockConfig.Setup(c => c["ApiKey"]).Returns("secret-key");
        var middleware = new HangfireBasicAuthMiddleware(next: (innerHttpContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(_context, _mockConfig.Object);

        // Assert
        Assert.Equal(401, _context.Response.StatusCode);
        Assert.True(_context.Response.Headers.ContainsKey("WWW-Authenticate"));
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn401_WhenCredentialsAreInvalid()
    {
        // Arrange
        _mockConfig.Setup(c => c["ApiKey"]).Returns("secret-key");
        var middleware = new HangfireBasicAuthMiddleware(next: (innerHttpContext) => Task.CompletedTask);

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:wrong-password"));
        _context.Request.Headers["Authorization"] = $"Basic {authValue}";

        // Act
        await middleware.InvokeAsync(_context, _mockConfig.Object);

        // Assert
        Assert.Equal(401, _context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenCredentialsAreValid()
    {
        // Arrange
        _mockConfig.Setup(c => c["ApiKey"]).Returns("secret-key");
        var nextCalled = false;
        var middleware = new HangfireBasicAuthMiddleware(next: (innerHttpContext) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:secret-key"));
        _context.Request.Headers["Authorization"] = $"Basic {authValue}";

        // Act
        await middleware.InvokeAsync(_context, _mockConfig.Object);

        // Assert
        Assert.True(nextCalled);
        Assert.True(_context.User.Identity?.IsAuthenticated);
        Assert.Equal("admin", _context.User.Identity?.Name);
    }
}
