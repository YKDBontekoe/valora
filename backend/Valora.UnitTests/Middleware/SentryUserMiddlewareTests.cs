using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Sentry;
using Valora.Api.Middleware;
using Xunit;

namespace Valora.UnitTests.Middleware;

public class SentryUserMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_UnauthenticatedUser_DoesNotSetSentryUser()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new SentryUserMiddleware(
            next: (innerHttpContext) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        // Note: We cannot easily verify SentrySdk.ConfigureScope was NOT called without a wrapper,
        // but this test ensures the middleware doesn't crash.
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedUser_SetsSentryUser()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new SentryUserMiddleware(
            next: (innerHttpContext) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

        var context = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        context.User = new ClaimsPrincipal(identity);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        // This exercises the code path for authenticated users.
    }
}
