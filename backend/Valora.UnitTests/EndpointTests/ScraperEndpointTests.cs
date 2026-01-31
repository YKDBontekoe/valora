using System.Net;
using Xunit;

namespace Valora.UnitTests.EndpointTests;

public class ScraperEndpointTests : IClassFixture<EndpointTestFactory>
{
    private readonly EndpointTestFactory _factory;

    public ScraperEndpointTests(EndpointTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Trigger_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/scraper/trigger", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Seed_ReturnsBadRequest_WithoutRegion()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/scraper/seed", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Seed_ReturnsOk_WithRegion()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/scraper/seed?region=Amsterdam", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
