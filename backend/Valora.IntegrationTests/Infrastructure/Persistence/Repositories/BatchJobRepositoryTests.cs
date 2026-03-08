using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Domain.Enums;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests.Infrastructure.Persistence.Repositories;

[Collection("TestDatabase")]
public class BatchJobRepositoryTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;

    public BatchJobRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        using var scope = _fixture.Factory!.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        dbContext.BatchJobs.RemoveRange(dbContext.BatchJobs);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetNextPendingJobAsync_ShouldReturnOldestPendingJob()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBatchJobRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var oldJobId = Guid.NewGuid();
        var newJobId = Guid.NewGuid();

        var oldJob = new BatchJob
        {
            Id = oldJobId,
            Type = BatchJobType.CityIngestion,
            Target = "OldJobTarget",
            Status = BatchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var newJob = new BatchJob
        {
            Id = newJobId,
            Type = BatchJobType.CityIngestion,
            Target = "NewJobTarget",
            Status = BatchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        await repository.AddAsync(oldJob);
        await repository.AddAsync(newJob);

        // Clear ChangeTracker so we test database state, not in-memory state
        dbContext.ChangeTracker.Clear();

        // Act
        var result = await repository.GetNextPendingJobAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(oldJobId, result.Id);
        Assert.Equal(BatchJobStatus.Processing, result.Status);
        Assert.NotNull(result.StartedAt);

        // Verify Database Side Effects
        dbContext.ChangeTracker.Clear();
        var oldJobFromDb = await repository.GetByIdAsync(oldJobId);
        var newJobFromDb = await repository.GetByIdAsync(newJobId);

        Assert.NotNull(oldJobFromDb);
        Assert.Equal(BatchJobStatus.Processing, oldJobFromDb.Status);

        Assert.NotNull(newJobFromDb);
        Assert.Equal(BatchJobStatus.Pending, newJobFromDb.Status);
    }

    [Fact]
    public async Task GetNextPendingJobAsync_WhenNoPendingJobs_ShouldReturnNull()
    {
        // Arrange
        using var scope = _fixture.Factory!.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBatchJobRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var processingJob = new BatchJob
        {
            Id = Guid.NewGuid(),
            Type = BatchJobType.CityIngestion,
            Target = "ProcessingJob",
            Status = BatchJobStatus.Processing,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var completedJob = new BatchJob
        {
            Id = Guid.NewGuid(),
            Type = BatchJobType.MapGeneration,
            Target = "CompletedJob",
            Status = BatchJobStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        await repository.AddAsync(processingJob);
        await repository.AddAsync(completedJob);

        dbContext.ChangeTracker.Clear();

        // Act
        var result = await repository.GetNextPendingJobAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetNextPendingJobAsync_ConcurrentClaims_ShouldOnlyClaimOnce()
    {
        // Arrange
        var scopes = new List<IServiceScope>();
        var repositories = new List<IBatchJobRepository>();

        // Setup a pending job to be claimed concurrently
        using var setupScope = _fixture.Factory!.Services.CreateScope();
        var setupRepository = setupScope.ServiceProvider.GetRequiredService<IBatchJobRepository>();
        var dbContext = setupScope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var jobId = Guid.NewGuid();
        var pendingJob = new BatchJob
        {
            Id = jobId,
            Type = BatchJobType.CityIngestion,
            Target = "ConcurrentClaimTarget",
            Status = BatchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20) // ensure it's oldest
        };
        await setupRepository.AddAsync(pendingJob);
        dbContext.ChangeTracker.Clear();

        // Prepare concurrent requests
        int concurrentRequests = 10;
        for (int i = 0; i < concurrentRequests; i++)
        {
            var scope = _fixture.Factory!.Services.CreateScope();
            scopes.Add(scope);
            repositories.Add(scope.ServiceProvider.GetRequiredService<IBatchJobRepository>());
        }

        // Act
        // Create an array of tasks that will call GetNextPendingJobAsync concurrently
        var tasks = repositories.Select(r => r.GetNextPendingJobAsync()).ToArray();

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks);

        // Assert
        // We expect exactly one task to successfully claim the job (return the job)
        // Others should return null (because we only have one oldest job)

        var successfulClaims = results.Where(r => r != null && r.Id == jobId).ToList();

        Assert.Single(successfulClaims);
        Assert.Equal(BatchJobStatus.Processing, successfulClaims.First()!.Status);

        // Verify database state directly
        var verifyScope = _fixture.Factory!.Services.CreateScope();
        var verifyRepo = verifyScope.ServiceProvider.GetRequiredService<IBatchJobRepository>();
        var jobInDb = await verifyRepo.GetByIdAsync(jobId);

        Assert.NotNull(jobInDb);
        Assert.Equal(BatchJobStatus.Processing, jobInDb.Status);

        // Cleanup scopes
        foreach (var scope in scopes)
        {
            scope.Dispose();
        }
    }
}
