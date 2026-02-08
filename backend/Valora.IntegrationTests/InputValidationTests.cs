using System.Net;
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

}
