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
    public async Task GetJobs_ReturnsSummaryList() // Does not include ExecutionLog
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "ListCity",
            Status = BatchJobStatus.Pending,
            Progress = 0,
            ExecutionLog = "Big log"
        };

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            context.BatchJobs.Add(job);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await Client.GetAsync("/api/admin/jobs");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();
        result.ShouldNotBeNull();
        result!.Items.ShouldNotBeEmpty();
        var dto = result.Items.First(j => j.Target == "ListCity");
        // Cannot check ExecutionLog as it is not on the DTO, which is the point.
        dto.Target.ShouldBe("ListCity");
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
        dto.Error.ShouldBe("Job cancelled by user.");
    }

    [Fact]
    public async Task RetryJob_ShouldFail_WhenJobIsPending()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "PendingRetry",
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
        var response = await Client.PostAsync($"/api/admin/jobs/{job.Id}/retry", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelJob_ShouldFail_WhenJobIsCompleted()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "CompletedCancel",
            Status = BatchJobStatus.Completed,
            Progress = 100,
            CompletedAt = DateTime.UtcNow
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
