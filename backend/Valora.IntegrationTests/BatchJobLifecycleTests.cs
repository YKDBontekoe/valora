using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class BatchJobLifecycleTests : BaseIntegrationTest
{
    public BatchJobLifecycleTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task EnqueueJob_CreatesPendingJob()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var request = new BatchJobRequest("CityIngestion", "TestCity");

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/jobs", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var dto = await response.Content.ReadFromJsonAsync<BatchJobDto>();
        dto.ShouldNotBeNull();
        dto!.Target.ShouldBe("TestCity");

        // Verify database state
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var job = await context.BatchJobs.FindAsync(dto.Id);
            job.ShouldNotBeNull();
            job!.Status.ShouldBe(BatchJobStatus.Pending);
            job.Target.ShouldBe("TestCity");

            // CreatedAt should be very recent (within last 10 seconds)
            (DateTime.UtcNow - job.CreatedAt).TotalSeconds.ShouldBeLessThan(10);
        }
    }

    [Fact]
    public async Task RetryJob_ResetsStatusAndUpdatesQueuePosition()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Create a job with an old timestamp
        var originalCreatedAt = DateTime.UtcNow.AddHours(-1);
        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "RetryTest",
            Status = BatchJobStatus.Failed,
            Progress = 50,
            Error = "Original Error",
            CreatedAt = originalCreatedAt
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

        // Verify database state
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var updatedJob = await context.BatchJobs.FindAsync(job.Id);

            updatedJob.ShouldNotBeNull();

            // Status should be reset to Pending
            updatedJob!.Status.ShouldBe(BatchJobStatus.Pending);

            // Error and Progress should be reset
            updatedJob.Error.ShouldBeNull();
            updatedJob.Progress.ShouldBe(0);

            // Queue position logic: CreatedAt must be updated to now (moved to end of queue)
            // It should be strictly greater than the original timestamp
            updatedJob.CreatedAt.ShouldBeGreaterThan(originalCreatedAt);

            // And it should be recent
            (DateTime.UtcNow - updatedJob.CreatedAt).TotalSeconds.ShouldBeLessThan(10);
        }
    }

    [Fact]
    public async Task CancelJob_MarksJobAsFailed()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "CancelTest",
            Status = BatchJobStatus.Pending,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
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

        // Verify database state
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var updatedJob = await context.BatchJobs.FindAsync(job.Id);

            updatedJob.ShouldNotBeNull();

            updatedJob!.Status.ShouldBe(BatchJobStatus.Failed);
            updatedJob.Error.ShouldBe("Job cancelled by user.");
            updatedJob.CompletedAt.ShouldNotBeNull();
        }
    }
}
