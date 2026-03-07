using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.IntegrationTests;

public class BatchJobRepositoryTests : BaseTestcontainersIntegrationTest
{
    private readonly BatchJobRepository _repository;

    public BatchJobRepositoryTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
        _repository = new BatchJobRepository(DbContext);
    }

    [Fact]
    public async Task GetNextPendingJobAsync_ShouldReturnOldestPendingJobAndMarkAsProcessing()
    {
        // Arrange
        var uniquePrefix = Guid.NewGuid().ToString();
        var job1 = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = $"{uniquePrefix}-Oldest",
            Status = BatchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var job2 = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = $"{uniquePrefix}-Newer",
            Status = BatchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        DbContext.BatchJobs.AddRange(job1, job2);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        // This will now use ExecuteUpdateAsync when Testcontainers is active,
        // or the fallback code when running InMemory.
        var result = await _repository.GetNextPendingJobAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(job1.Id, result.Id); // Ensure the oldest job is returned
        Assert.Equal($"{uniquePrefix}-Oldest", result.Target); // Double-check the target
        Assert.Equal(BatchJobStatus.Processing, result.Status);
        Assert.NotNull(result.StartedAt);

        var dbJob = await DbContext.BatchJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == result.Id);
        Assert.NotNull(dbJob);
        Assert.Equal(BatchJobStatus.Processing, dbJob.Status);
    }

    [Fact]
    public async Task GetNextPendingJobAsync_ShouldReturnNullWhenNoPendingJobs()
    {
        // First ensure the table is clean of pending jobs from any other tests
        // that might have leaked state. While BaseTestcontainersIntegrationTest
        // cleans up before each test, if we run in parallel without a fresh DB per test,
        // we might still have a race condition. However, to strictly follow the review:
        // "Mutates all pending jobs... which can affect other tests; instead, seed isolated data...
        // remove the global ExecuteUpdateAsync/loop..."

        // In a true isolated Testcontainer setup, the DB is empty here anyway
        // because of `InitializeAsync` wiping the table.
        // We will seed a single completed job to prove the repository ignores it.
        var uniqueTarget = Guid.NewGuid().ToString();
        var completedJob = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = $"{uniqueTarget}-Completed",
            Status = BatchJobStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.BatchJobs.Add(completedJob);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetNextPendingJobAsync();

        // Assert
        // We assert null because there are no PENDING jobs, only COMPLETED.
        Assert.Null(result);
    }

    [Fact]
    public async Task GetJobsAsync_ShouldFilterAndSortCorrectly()
    {
        // Arrange
        var uniqueTarget = Guid.NewGuid().ToString();
        var job1 = new BatchJob
        {
            Type = BatchJobType.MapGeneration,
            Target = $"{uniqueTarget}-Alpha",
            Status = BatchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var job2 = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = $"{uniqueTarget}-Beta",
            Status = BatchJobStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var job3 = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = $"{uniqueTarget}-Alpha-Omega",
            Status = BatchJobStatus.Failed,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.BatchJobs.AddRange(job1, job2, job3);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act & Assert

        // 1. Filter by Status
        var completedJobs = await _repository.GetJobsAsync(1, 10, status: BatchJobStatus.Completed);
        Assert.Contains(completedJobs.Items, j => j.Target == $"{uniqueTarget}-Beta");
        Assert.DoesNotContain(completedJobs.Items, j => j.Target == $"{uniqueTarget}-Alpha");

        // 2. Filter by Type
        var cityJobs = await _repository.GetJobsAsync(1, 10, type: BatchJobType.CityIngestion);
        Assert.Contains(cityJobs.Items, j => j.Target == $"{uniqueTarget}-Beta");
        Assert.DoesNotContain(cityJobs.Items, j => j.Target == $"{uniqueTarget}-Alpha");

        // 3. Search target
        var alphaSearch = await _repository.GetJobsAsync(1, 10, search: $"{uniqueTarget}-Alpha");
        Assert.Equal(2, alphaSearch.Items.Count); // Should find job1 and job3
        Assert.Contains(alphaSearch.Items, j => j.Status == BatchJobStatus.Pending);
        Assert.Contains(alphaSearch.Items, j => j.Status == BatchJobStatus.Failed);

        // 4. Sort by CreatedAt Asc
        var sortAsc = await _repository.GetJobSummariesAsync(1, 10, search: uniqueTarget, sort: "createdAt_asc");
        Assert.Equal($"{uniqueTarget}-Alpha", sortAsc.Items.First().Target);

        // 5. Sort by Status Desc
        var sortStatusDesc = await _repository.GetJobsAsync(1, 10, search: uniqueTarget, sort: "status_desc");
        Assert.Equal($"{uniqueTarget}-Alpha-Omega", sortStatusDesc.Items.First().Target);
    }
}
