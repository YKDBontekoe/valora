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
    public async Task ScraperEndpoints_WithoutAuthentication_ReturnUnauthorizedOrForbidden(string url)
    {
        var response = await Client.PostAsync(url, content: null);

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected 401/403 but got {(int)response.StatusCode} ({response.StatusCode}).");
    }
}
