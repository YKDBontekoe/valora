using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class BatchJobRepositoryIntegrationTests : BaseTestcontainersIntegrationTest
{
    public BatchJobRepositoryIntegrationTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetNextPendingJobAsync_ShouldReturnOldestPendingJob_AndSetStatusToProcessing()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBatchJobRepository>();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var job1 = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Job1", Status = BatchJobStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var job2 = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Job2", Status = BatchJobStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var job3 = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Job3", Status = BatchJobStatus.Processing, CreatedAt = DateTime.UtcNow.AddMinutes(-15) };

        context.BatchJobs.AddRange(job1, job2, job3);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var result = await repository.GetNextPendingJobAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Job1", result.Target);
        Assert.Equal(BatchJobStatus.Processing, result.Status);
        Assert.NotNull(result.StartedAt);

        // Verify side-effect in DB
        context.ChangeTracker.Clear();
        var updatedJobInDb = await context.BatchJobs.FindAsync(job1.Id);
        Assert.NotNull(updatedJobInDb);
        Assert.Equal(BatchJobStatus.Processing, updatedJobInDb.Status);
        Assert.NotNull(updatedJobInDb.StartedAt);
    }

    [Fact]
    public async Task GetNextPendingJobAsync_Concurrency_ShouldOnlyClaimOnce()
    {
        // Arrange
        using var setupScope = Factory.Services.CreateScope();
        var context = setupScope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "ConcurrentJob", Status = BatchJobStatus.Pending, CreatedAt = DateTime.UtcNow };
        context.BatchJobs.Add(job);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        int concurrentWorkers = 5;
        var tasks = new Task<BatchJob?>[concurrentWorkers];
        var scopes = new IServiceScope[concurrentWorkers];

        // Act
        for (int i = 0; i < concurrentWorkers; i++)
        {
            scopes[i] = Factory.Services.CreateScope();
            var repo = scopes[i].ServiceProvider.GetRequiredService<IBatchJobRepository>();
            tasks[i] = repo.GetNextPendingJobAsync();
        }

        var results = await Task.WhenAll(tasks);

        // Cleanup scopes
        foreach (var scope in scopes)
        {
            scope.Dispose();
        }

        // Assert
        // Only one worker should have successfully claimed the job
        var successfulClaims = results.Where(r => r != null).ToList();
        Assert.Single(successfulClaims);
        Assert.Equal("ConcurrentJob", successfulClaims[0]!.Target);

        // Verify side-effect in DB
        using var assertScope = Factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var updatedJobInDb = await assertContext.BatchJobs.FindAsync(job.Id);

        Assert.NotNull(updatedJobInDb);
        Assert.Equal(BatchJobStatus.Processing, updatedJobInDb.Status);
        Assert.NotNull(updatedJobInDb.StartedAt);
    }
}
