using Microsoft.AspNetCore.Http;
using Valora.Api.Middleware;

namespace Valora.UnitTests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsSecurityHeaders()
    {
        // Arrange
        var middleware = new SecurityHeadersMiddleware(
            next: (innerHttpContext) => Task.CompletedTask);

        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var headers = context.Response.Headers;

        Assert.True(headers.ContainsKey("X-Content-Type-Options"));
        Assert.Equal("nosniff", headers["X-Content-Type-Options"]);

        Assert.True(headers.ContainsKey("X-Frame-Options"));
        Assert.Equal("DENY", headers["X-Frame-Options"]);

        Assert.True(headers.ContainsKey("X-XSS-Protection"));
        Assert.Equal("1; mode=block", headers["X-XSS-Protection"]);

        Assert.True(headers.ContainsKey("Referrer-Policy"));
        Assert.Equal("strict-origin-when-cross-origin", headers["Referrer-Policy"]);

        Assert.True(headers.ContainsKey("Content-Security-Policy"));
        Assert.Contains("default-src 'self'", headers["Content-Security-Policy"].ToString());
    }
}
