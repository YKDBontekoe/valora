using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;

namespace Valora.UnitTests.Infrastructure.Persistence.Repositories;

public class BatchJobRepositoryTests
{
    private DbContextOptions<ValoraDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task AddAsync_ShouldAddJob()
    {
        using var context = new ValoraDbContext(CreateOptions());
        var repository = new BatchJobRepository(context);
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam" };

        var result = await repository.AddAsync(job);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(1, await context.BatchJobs.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnJob()
    {
        using var context = new ValoraDbContext(CreateOptions());
        var repository = new BatchJobRepository(context);
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam" };
        context.BatchJobs.Add(job);
        await context.SaveChangesAsync();

        var result = await repository.GetByIdAsync(job.Id);

        Assert.NotNull(result);
        Assert.Equal("Amsterdam", result.Target);
    }

    [Fact]
    public async Task GetNextPendingJobAsync_ShouldReturnOldestPendingJob()
    {
        using var context = new ValoraDbContext(CreateOptions());
        var repository = new BatchJobRepository(context);

        var job1 = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Job1", Status = BatchJobStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-10) };
        var job2 = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Job2", Status = BatchJobStatus.Pending, CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var job3 = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Job3", Status = BatchJobStatus.Processing };

        context.BatchJobs.AddRange(job1, job2, job3);
        await context.SaveChangesAsync();

        var result = await repository.GetNextPendingJobAsync();

        Assert.NotNull(result);
        Assert.Equal("Job1", result.Target);
    }

    [Fact]
    public async Task GetRecentJobsAsync_ShouldReturnLatestJobs()
    {
        using var context = new ValoraDbContext(CreateOptions());
        var repository = new BatchJobRepository(context);

        for (int i = 1; i <= 15; i++)
        {
            context.BatchJobs.Add(new BatchJob { Type = BatchJobType.CityIngestion, Target = $"Job{i}", CreatedAt = DateTime.UtcNow.AddMinutes(i) });
        }
        await context.SaveChangesAsync();

        var result = await repository.GetRecentJobsAsync(10);

        Assert.Equal(10, result.Count);
        Assert.Equal("Job15", result[0].Target);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateJob()
    {
        using var context = new ValoraDbContext(CreateOptions());
        var repository = new BatchJobRepository(context);
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam" };
        context.BatchJobs.Add(job);
        await context.SaveChangesAsync();

        job.Status = BatchJobStatus.Completed;
        job.Progress = 100;

        await repository.UpdateAsync(job);

        var updated = await context.BatchJobs.FindAsync(job.Id);
        Assert.NotNull(updated);
        Assert.Equal(BatchJobStatus.Completed, updated.Status);
        Assert.Equal(100, updated.Progress);
    }
}
