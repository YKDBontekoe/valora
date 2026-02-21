using System.Net;
using System.Net.Http.Json;
using Valora.Application.DTOs;
using Xunit;

namespace Valora.IntegrationTests;

public class SupportEndpointTests : BaseIntegrationTest
{
    public SupportEndpointTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetSupportStatus_ReturnsOkAndDto()
    {
        // Act
        // This endpoint doesn't require auth
        var response = await Client.GetAsync("/api/support/status");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var status = await response.Content.ReadFromJsonAsync<SupportStatusDto>();
        Assert.NotNull(status);
        Assert.NotNull(status.SupportMessage);

        // Assert defaults or configured values
        // Assuming default config from appsettings or defaults in code
        Assert.True(status.IsSupportActive);
        Assert.Equal("Our support team is available 24/7", status.SupportMessage);
        Assert.Equal("https://status.valora.nl", status.StatusPageUrl);
        Assert.Equal("support@valora.nl", status.ContactEmail);
    }
}
