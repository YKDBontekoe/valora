using System.Net;
using System.Net.Http.Json;
using Valora.Application.DTOs;
using Valora.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace Valora.IntegrationTests;

public class HealthCheckTests : BaseIntegrationTest
{
    public HealthCheckTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Get_Health_ReturnsOk_WithCorrectSchema()
    {
        // Arrange: Clear batch jobs to ensure clean slate for this test
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            db.BatchJobs.RemoveRange(db.BatchJobs);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert
        var health = await response.Content.ReadFromJsonAsync<HealthStatusDto>();
        Assert.NotNull(health);
        Assert.Equal("Healthy", health.Status);
        Assert.Equal("Connected", health.DatabaseStatus);
        Assert.True(health.ApiLatencyMs >= 0);

        // Assert counts are zero
        Assert.Equal(0, health.ActiveJobs);
        Assert.Equal(0, health.QueuedJobs);
        Assert.Equal(0, health.FailedJobs);
    }
}
