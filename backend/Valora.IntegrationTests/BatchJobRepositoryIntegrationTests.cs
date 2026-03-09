using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

public class BatchJobRepositoryIntegrationTests : BaseIntegrationTest
{
    public BatchJobRepositoryIntegrationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetNextPendingJobAsync_ShouldAtomicallyClaimJob()
    {
        // Arrange
        // Create a single pending job
        var pendingJob = new BatchJob
        {
            Id = Guid.NewGuid(),
            Type = BatchJobType.CityIngestion,
            Status = BatchJobStatus.Pending,
            Target = "ConcurrentTestCity",
            CreatedAt = DateTime.UtcNow
        };

        DbContext.BatchJobs.Add(pendingJob);
        await DbContext.SaveChangesAsync();

        // Ensure ChangeTracker is cleared so we query real DB state
        DbContext.ChangeTracker.Clear();

        int concurrentWorkers = 5;
        var tasks = new List<Task<BatchJob?>>();

        // Act
        // Attempt to claim the job simultaneously from multiple scopes
        for (int i = 0; i < concurrentWorkers; i++)
        {
            var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBatchJobRepository>();
            tasks.Add(repository.GetNextPendingJobAsync());
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulClaims = results.Where(r => r != null).ToList();

        // Exactly one worker should have successfully claimed the job
        Assert.Single(successfulClaims);

        var claimedJob = successfulClaims.First();
        Assert.NotNull(claimedJob);
        Assert.Equal(pendingJob.Id, claimedJob.Id);
        Assert.Equal(BatchJobStatus.Processing, claimedJob.Status);
        Assert.NotNull(claimedJob.StartedAt);

        // Verify database state using a fresh context
        using var verifyScope = Factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var dbJob = await verifyDbContext.BatchJobs.FirstOrDefaultAsync(j => j.Id == pendingJob.Id);

        Assert.NotNull(dbJob);
        Assert.Equal(BatchJobStatus.Processing, dbJob.Status);
        Assert.NotNull(dbJob.StartedAt);

        // Ensure only one job exists in the table to be safe
        var totalJobsCount = await verifyDbContext.BatchJobs.CountAsync();
        Assert.Equal(1, totalJobsCount);
    }
}
