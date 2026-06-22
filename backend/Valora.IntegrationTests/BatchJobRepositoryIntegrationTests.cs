using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
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
        var job1 = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Job1", Status = BatchJobStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var job2 = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Job2", Status = BatchJobStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var job3 = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Job3", Status = BatchJobStatus.Processing };

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            context.BatchJobs.AddRange(job1, job2, job3);
            await context.SaveChangesAsync();
        }

        BatchJob? result;

        // Act
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var repository = new BatchJobRepository(context);
            result = await repository.GetNextPendingJobAsync();
        }

        // Assert
        result.ShouldNotBeNull();
        result!.Target.ShouldBe("Job1");
        result.Status.ShouldBe(BatchJobStatus.Processing);
        result.StartedAt.ShouldNotBeNull();

        // Verify database state
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var dbJob = await context.BatchJobs.FindAsync(result.Id);
            dbJob.ShouldNotBeNull();
            dbJob!.Status.ShouldBe(BatchJobStatus.Processing);
        }
    }

    [Fact]
    public async Task GetNextPendingJobAsync_WithConcurrentExecution_OnlyOneProcessClaimsJob()
    {
        // Arrange
        var job1 = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Job1", Status = BatchJobStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-10) };

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            context.BatchJobs.Add(job1);
            await context.SaveChangesAsync();
        }

        // Act
        // Create 3 distinct scopes for 3 concurrent workers
        var scope1 = Factory.Services.CreateScope();
        var scope2 = Factory.Services.CreateScope();
        var scope3 = Factory.Services.CreateScope();

        var repo1 = new BatchJobRepository(scope1.ServiceProvider.GetRequiredService<ValoraDbContext>());
        var repo2 = new BatchJobRepository(scope2.ServiceProvider.GetRequiredService<ValoraDbContext>());
        var repo3 = new BatchJobRepository(scope3.ServiceProvider.GetRequiredService<ValoraDbContext>());

        // Start all 3 simultaneously
        var t1 = repo1.GetNextPendingJobAsync();
        var t2 = repo2.GetNextPendingJobAsync();
        var t3 = repo3.GetNextPendingJobAsync();

        var results = await Task.WhenAll(t1, t2, t3);

        // Cleanup scopes
        scope1.Dispose();
        scope2.Dispose();
        scope3.Dispose();

        // Assert
        // Only one of the tasks should successfully return the claimed job (not null)
        var successfulClaims = results.Where(r => r != null).ToList();

        successfulClaims.Count.ShouldBe(1);
        successfulClaims.Single()!.Target.ShouldBe("Job1");
        successfulClaims.Single()!.Status.ShouldBe(BatchJobStatus.Processing);

        // Verify database state
        using (var checkScope = Factory.Services.CreateScope())
        {
            var context = checkScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var dbJob = await context.BatchJobs.FindAsync(job1.Id);
            dbJob.ShouldNotBeNull();
            dbJob!.Status.ShouldBe(BatchJobStatus.Processing);
        }
    }
}
