using System.Net;
using System.Net.Http.Json;
using Valora.Application.DTOs;
using Valora.Application.Scraping;
using Xunit;

namespace Valora.IntegrationTests;

public class InputValidationTests : BaseIntegrationTest
{
    public InputValidationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task SearchListings_WithInvalidPage_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        // Page > 10000 should fail
        var query = "page=10001";
        var response = await Client.GetAsync($"/api/listings?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchListings_WithNegativePrice_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var query = "minPrice=-100";
        var response = await Client.GetAsync($"/api/listings?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchListings_WithNegativeBedrooms_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var query = "minBedrooms=-1";
        var response = await Client.GetAsync($"/api/listings?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchListings_WithNegativeLivingArea_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var query = "minLivingArea=-1";
        var response = await Client.GetAsync($"/api/listings?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FundaSearch_WithNegativePrice_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var query = "region=amsterdam&minPrice=-100";
        var response = await Client.GetAsync($"/api/search?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FundaSearch_WithNegativeBedrooms_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var query = "region=amsterdam&minBedrooms=-1";
        var response = await Client.GetAsync($"/api/search?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
