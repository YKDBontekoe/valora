using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Valora.IntegrationTests;

public class NotificationEndpointTests : BaseIntegrationTest
{
    public NotificationEndpointTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetNotifications_WithValidLimit_ReturnsOk()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/notifications?limit=10");

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");
    }

    [Fact]
    public async Task GetNotifications_WithLimitOver100_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/notifications?limit=101");

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Limit must be between 0 and 100", content);
    }

    [Fact]
    public async Task GetNotifications_WithNegativeLimit_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/notifications?limit=-1");

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Limit must be between 0 and 100", content);
    }
}
