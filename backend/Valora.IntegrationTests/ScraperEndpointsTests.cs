using System.Net;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class ScraperEndpointsTests : BaseIntegrationTest
{
    public ScraperEndpointsTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Trigger_Scraper_ReturnsOk()
    {
        // Act
        var response = await Client.PostAsync("/api/scraper/trigger", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Seed_ReturnsBadRequest_WhenRegionMissing()
    {
        // Act
        var response = await Client.PostAsync("/api/scraper/seed?region=", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Seed_ReturnsOk_WhenRegionProvided()
    {
        // Act
        var response = await Client.PostAsync("/api/scraper/seed?region=Amsterdam", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // We could verify the JSON response contains "queued" or "skipped",
        // but verifying 200 OK is sufficient to hit the code path.
    }
}
