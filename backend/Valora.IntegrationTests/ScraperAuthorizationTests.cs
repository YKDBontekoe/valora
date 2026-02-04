using System.Net;
using Xunit;

namespace Valora.IntegrationTests;

public class ScraperAuthorizationTests : BaseIntegrationTest
{
    public ScraperAuthorizationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Theory]
    [InlineData("/api/scraper/trigger")]
    [InlineData("/api/scraper/trigger-limited?region=amsterdam&limit=1")]
    [InlineData("/api/scraper/seed?region=amsterdam")]
    public async Task ScraperEndpoints_WithoutAuthentication_ReturnUnauthorized(string url)
    {
        var response = await Client.PostAsync(url, content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/scraper/trigger")]
    [InlineData("/api/scraper/trigger-limited?region=amsterdam&limit=1")]
    [InlineData("/api/scraper/seed?region=amsterdam")]
    public async Task ScraperEndpoints_WithStandardUser_ReturnForbidden(string url)
    {
        // Arrange: Authenticate as a standard user (no Admin role)
        await AuthenticateAsync("user@example.com", "Password123!");

        // Act
        var response = await Client.PostAsync(url, content: null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/scraper/trigger")]
    [InlineData("/api/scraper/trigger-limited?region=amsterdam&limit=1")]
    [InlineData("/api/scraper/seed?region=amsterdam")]
    public async Task ScraperEndpoints_WithAdminUser_ReturnsServiceUnavailable(string url)
    {
        // Arrange: Authenticate as an Admin user
        // The endpoints return 503 Service Unavailable when Hangfire is disabled (which it is in tests)
        // This confirms authorization passed (otherwise we'd get 401/403) and the handler executed.
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.PostAsync(url, content: null);

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
