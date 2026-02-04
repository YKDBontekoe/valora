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
    public async Task FundaSearch_WithInvalidPage_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var query = new FundaSearchQuery
        {
            Region = "amsterdam",
            Page = 10001 // Invalid
        };

        var queryString = $"region={query.Region}&page={query.Page}";
        var response = await Client.GetAsync($"/api/search?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FundaSearch_WithInvalidOfferingType_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var queryString = "region=amsterdam&offeringType=invalid_type";
        var response = await Client.GetAsync($"/api/search?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
