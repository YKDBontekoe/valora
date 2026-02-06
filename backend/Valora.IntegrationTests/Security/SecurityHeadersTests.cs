using Xunit;

namespace Valora.IntegrationTests.Security;

[Collection("TestDatabase")]
public class SecurityHeadersTests : BaseIntegrationTest
{
    public SecurityHeadersTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetHealth_ReturnsSecurityHeaders()
    {
        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        response.EnsureSuccessStatusCode();

        Assert.True(response.Headers.Contains("X-Content-Type-Options"), "X-Content-Type-Options header missing");
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());

        Assert.True(response.Headers.Contains("X-Frame-Options"), "X-Frame-Options header missing");
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());

        Assert.True(response.Headers.Contains("Referrer-Policy"), "Referrer-Policy header missing");
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").First());

        Assert.True(response.Headers.Contains("Content-Security-Policy"), "Content-Security-Policy header missing");
        Assert.Equal("default-src 'self';", response.Headers.GetValues("Content-Security-Policy").First());
    }
}
