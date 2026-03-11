using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;
using Shouldly;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class BatchJobConcurrencyIntegrationTests : BaseTestcontainersIntegrationTest
{
    public BatchJobConcurrencyIntegrationTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetNextPendingJobAsync_ConcurrentCalls_OnlyOneWorkerClaimsJob()
    {
        // Arrange
        // We need a job that is pending and can be claimed
        Guid jobId;
        using (var setupScope = Factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<ValoraDbContext>();

            // Clear existing jobs to ensure clean slate
            dbContext.BatchJobs.RemoveRange(dbContext.BatchJobs);
            await dbContext.SaveChangesAsync();

            var job = new BatchJob
            {
                Type = BatchJobType.CityIngestion,
                Status = BatchJobStatus.Pending,
                Target = "ConcurrencyTestCity",
                CreatedAt = DateTime.UtcNow
            };

            dbContext.BatchJobs.Add(job);
            await dbContext.SaveChangesAsync();
            jobId = job.Id;
        }

        // We will simulate multiple concurrent workers trying to claim a job.
        // We use Task.WhenAll to run them as closely together as possible.
        int numWorkers = 5;
        var tasks = new Task<BatchJob?>[numWorkers];

        // Act
        for (int i = 0; i < numWorkers; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                // Each worker needs its own scope to avoid DbContext concurrency issues
                using var scope = Factory.Services.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IBatchJobRepository>();
                return await repository.GetNextPendingJobAsync();
            });
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        // Out of all concurrent attempts, exactly ONE should return the job
        var successfulClaims = results.Where(j => j != null).ToList();

        successfulClaims.Count.ShouldBe(1, "Exactly one worker should have successfully claimed the job.");
        var claimedJob = successfulClaims.First();
        claimedJob!.Id.ShouldBe(jobId);
        claimedJob.Status.ShouldBe(BatchJobStatus.Processing);
        claimedJob.StartedAt.ShouldNotBeNull();

        // The rest should return null
        results.Count(j => j == null).ShouldBe(numWorkers - 1);

        // Verify final state in DB
        using (var assertScope = Factory.Services.CreateScope())
        {
            var dbContext = assertScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var jobInDb = await dbContext.BatchJobs.FindAsync(jobId);

            jobInDb.ShouldNotBeNull();
            jobInDb!.Status.ShouldBe(BatchJobStatus.Processing);
            jobInDb.StartedAt.ShouldNotBeNull();
        }
    }
}
