using Valora.IntegrationTests;

namespace Valora.IntegrationTests.Middleware;

public class SecurityHeadersIntegrationTests : BaseIntegrationTest
{
    public SecurityHeadersIntegrationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Get_Health_ReturnsSecurityHeaders()
    {
        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        response.EnsureSuccessStatusCode();

        var headers = response.Headers;

        Assert.True(headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", headers.GetValues("X-Content-Type-Options").First());

        Assert.True(headers.Contains("X-Frame-Options"));
        Assert.Equal("DENY", headers.GetValues("X-Frame-Options").First());

        Assert.True(headers.Contains("X-XSS-Protection"));
        Assert.Equal("1; mode=block", headers.GetValues("X-XSS-Protection").First());

        Assert.True(headers.Contains("Referrer-Policy"));
        Assert.Equal("strict-origin-when-cross-origin", headers.GetValues("Referrer-Policy").First());

        Assert.True(headers.Contains("Content-Security-Policy"));
        Assert.Contains("default-src 'self'", headers.GetValues("Content-Security-Policy").First());
    }
}
