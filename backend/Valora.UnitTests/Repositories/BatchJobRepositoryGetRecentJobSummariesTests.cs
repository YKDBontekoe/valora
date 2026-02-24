using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Repositories;

public class BatchJobRepositoryGetRecentJobSummariesTests
{
    private readonly DbContextOptions<ValoraDbContext> _options;

    public BatchJobRepositoryGetRecentJobSummariesTests()
    {
        _options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetRecentJobSummariesAsync_ShouldReturnSummaries_OrderedByCreatedAtDesc()
    {
        using var context = new ValoraDbContext(_options);
        context.BatchJobs.AddRange(
            new BatchJob
            {
                Id = Guid.NewGuid(),
                Type = BatchJobType.CityIngestion,
                Target = "Old",
                Status = BatchJobStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new BatchJob
            {
                Id = Guid.NewGuid(),
                Type = BatchJobType.CityIngestion,
                Target = "New",
                Status = BatchJobStatus.Pending,
                CreatedAt = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        var repository = new BatchJobRepository(context);

        var result = await repository.GetRecentJobSummariesAsync(10);

        Assert.Equal(2, result.Count);
        Assert.Equal("New", result[0].Target);
        Assert.Equal("Old", result[1].Target);
    }

    [Fact]
    public async Task GetRecentJobSummariesAsync_ShouldLimitResults()
    {
        using var context = new ValoraDbContext(_options);
        for (int i = 0; i < 15; i++)
        {
            context.BatchJobs.Add(new BatchJob
            {
                Id = Guid.NewGuid(),
                Type = BatchJobType.CityIngestion,
                Target = $"Job {i}",
                Status = BatchJobStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        var repository = new BatchJobRepository(context);

        var result = await repository.GetRecentJobSummariesAsync(5);

        Assert.Equal(5, result.Count);
        // The newest jobs (highest i) should be returned
        Assert.Equal("Job 14", result[0].Target);
    }
}
