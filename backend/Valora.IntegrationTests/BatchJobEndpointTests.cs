using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class BatchJobEndpointTests : BaseIntegrationTest
{
    public BatchJobEndpointTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetJobDetails_ReturnsJob_WhenExists()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "TestCity",
            Status = BatchJobStatus.Pending,
            Progress = 0,
            ExecutionLog = "Log entry"
        };

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            context.BatchJobs.Add(job);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync($"/api/admin/jobs/{job.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<BatchJobDto>();
        dto.ShouldNotBeNull();
        dto!.Target.ShouldBe("TestCity");
        dto.ExecutionLog.ShouldBe("Log entry");
    }

    [Fact]
    public async Task RetryJob_Succeeds_WhenFailed()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "RetryCity",
            Status = BatchJobStatus.Failed,
            Progress = 50,
            Error = "Something failed",
            ExecutionLog = "Failed log"
        };

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            context.BatchJobs.Add(job);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await Client.PostAsync($"/api/admin/jobs/{job.Id}/retry", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<BatchJobDto>();
        dto.ShouldNotBeNull();
        dto!.Status.ShouldBe(BatchJobStatus.Pending.ToString());
        dto.Progress.ShouldBe(0);
        dto.Error.ShouldBeNull();
        dto.ExecutionLog.ShouldBeNull();
    }

    [Fact]
    public async Task CancelJob_Succeeds_WhenPending()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "CancelCity",
            Status = BatchJobStatus.Pending,
            Progress = 0
        };

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            context.BatchJobs.Add(job);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await Client.PostAsync($"/api/admin/jobs/{job.Id}/cancel", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<BatchJobDto>();
        dto.ShouldNotBeNull();
        dto!.Status.ShouldBe(BatchJobStatus.Failed.ToString());
        dto.Error.ShouldBe("Cancelled by user");
    }
}
