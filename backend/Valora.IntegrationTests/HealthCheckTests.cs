using System.Net;
using System.Net.Http.Json;
using Xunit;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Valora.IntegrationTests;

public class HealthCheckResponse
{
    public bool IsHealthy { get; set; }
    public string? Status { get; set; }
    public bool Database { get; set; }
    public int ApiLatency { get; set; }
    public int ApiLatencyP50 { get; set; }
    public int ApiLatencyP95 { get; set; }
    public int ApiLatencyP99 { get; set; }
    public int ActiveJobs { get; set; }
    public int QueuedJobs { get; set; }
    public int FailedJobs { get; set; }
    public DateTime? LastPipelineSuccess { get; set; }
    public DateTime Timestamp { get; set; }
}

public class HealthCheckTests : BaseIntegrationTest
{
    public HealthCheckTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Get_Health_ReturnsOk_And_Correct_Fields()
    {
        // Arrange: Seed some jobs
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            // Clean up existing jobs to ensure counts are predictable
            db.BatchJobs.RemoveRange(db.BatchJobs);

            db.BatchJobs.Add(new BatchJob { Type = BatchJobType.CityIngestion, Target = "Pending", Status = BatchJobStatus.Pending });
            db.BatchJobs.Add(new BatchJob { Type = BatchJobType.CityIngestion, Target = "Processing", Status = BatchJobStatus.Processing });
            db.BatchJobs.Add(new BatchJob { Type = BatchJobType.CityIngestion, Target = "Processing 2", Status = BatchJobStatus.Processing });
            db.BatchJobs.Add(new BatchJob { Type = BatchJobType.CityIngestion, Target = "Failed", Status = BatchJobStatus.Failed, Error = "Boom" });
            db.BatchJobs.Add(new BatchJob { Type = BatchJobType.CityIngestion, Target = "Completed", Status = BatchJobStatus.Completed, CompletedAt = DateTime.UtcNow.AddMinutes(-5) });
            await db.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        Assert.NotNull(content);
        Assert.True(content.IsHealthy);
        Assert.Equal("Healthy", content.Status);
        Assert.True(content.Database);
        Assert.Equal(2, content.ActiveJobs);
        Assert.Equal(1, content.QueuedJobs);
        Assert.Equal(1, content.FailedJobs);
        Assert.NotNull(content.LastPipelineSuccess);
        Assert.True(content.ApiLatencyP50 >= 0);
        Assert.True(content.ApiLatencyP95 >= 0);
        Assert.True(content.ApiLatencyP99 >= 0);
    }
}
