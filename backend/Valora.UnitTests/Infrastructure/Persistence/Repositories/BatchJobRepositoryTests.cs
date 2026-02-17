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
        // Arrange
        using var context = new ValoraDbContext(CreateOptions());
        var repository = new BatchJobRepository(context);
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam" };

        // Act
        var result = await repository.AddAsync(job);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(1, await context.BatchJobs.CountAsync());
    }

    [Fact]
    public async Task GetNextPendingJobAsync_ShouldReturnOldestPendingJob()
    {
        // Arrange
        using var context = new ValoraDbContext(CreateOptions());
        var repository = new BatchJobRepository(context);

        var job1 = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Job1", Status = BatchJobStatus.Pending };
        var job2 = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Job2", Status = BatchJobStatus.Pending };
        var job3 = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Job3", Status = BatchJobStatus.Processing };

        context.BatchJobs.AddRange(job1, job2, job3);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetNextPendingJobAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Job1", result.Target);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateJob()
    {
        // Arrange
        using var context = new ValoraDbContext(CreateOptions());
        var repository = new BatchJobRepository(context);
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam" };
        context.BatchJobs.Add(job);
        await context.SaveChangesAsync();

        job.Status = BatchJobStatus.Completed;
        job.Progress = 100;

        // Act
        await repository.UpdateAsync(job);

        // Assert
        var updated = await context.BatchJobs.FindAsync(job.Id);
        Assert.NotNull(updated);
        Assert.Equal(BatchJobStatus.Completed, updated.Status);
        Assert.Equal(100, updated.Progress);
    }
}
