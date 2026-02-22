using System.Net;
using System.Net.Http.Json;
using Valora.Application.DTOs;
using Xunit;

namespace Valora.IntegrationTests;

public class HealthCheckTests : BaseIntegrationTest
{
    public HealthCheckTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Get_Health_ReturnsOk_WithCorrectSchema()
    {
        var response = await Client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var health = await response.Content.ReadFromJsonAsync<HealthStatusDto>();
        Assert.NotNull(health);
        Assert.Equal("Healthy", health.Status);
        Assert.Equal("Connected", health.DatabaseStatus);
        Assert.True(health.ApiLatencyMs >= 0);
        // DB is empty initially
        Assert.Equal(0, health.ActiveJobs);
        Assert.Equal(0, health.QueuedJobs);
        Assert.Equal(0, health.FailedJobs);
    }
}
