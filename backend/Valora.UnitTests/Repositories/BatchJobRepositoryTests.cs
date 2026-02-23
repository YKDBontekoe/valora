using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Repositories;

public class BatchJobRepositoryTests
{
    private readonly DbContextOptions<ValoraDbContext> _options;

    public BatchJobRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private async Task SeedDatabase(ValoraDbContext context)
    {
        context.BatchJobs.RemoveRange(context.BatchJobs);
        await context.SaveChangesAsync();

        context.BatchJobs.AddRange(
            new BatchJob
            {
                Id = Guid.NewGuid(),
                Type = BatchJobType.CityIngestion,
                Target = "Amsterdam",
                Status = BatchJobStatus.Completed,
                Progress = 100,
                CreatedAt = new DateTime(2023, 1, 1),
                UpdatedAt = new DateTime(2023, 1, 1)
            },
            new BatchJob
            {
                Id = Guid.NewGuid(),
                Type = BatchJobType.MapGeneration,
                Target = "Rotterdam",
                Status = BatchJobStatus.Processing,
                Progress = 50,
                CreatedAt = new DateTime(2023, 1, 2),
                UpdatedAt = new DateTime(2023, 1, 2)
            },
            new BatchJob
            {
                Id = Guid.NewGuid(),
                Type = BatchJobType.CityIngestion,
                Target = "Utrecht",
                Status = BatchJobStatus.Pending,
                Progress = 0,
                CreatedAt = new DateTime(2023, 1, 3),
                UpdatedAt = new DateTime(2023, 1, 3)
            },
            new BatchJob
            {
                Id = Guid.NewGuid(),
                Type = BatchJobType.AllCitiesIngestion,
                Target = "Netherlands",
                Status = BatchJobStatus.Failed,
                Progress = 0,
                CreatedAt = new DateTime(2023, 1, 4),
                UpdatedAt = new DateTime(2023, 1, 4)
            }
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetJobsAsync_ShouldFilterByStatus()
    {
        using var context = new ValoraDbContext(_options);
        await SeedDatabase(context);
        var repository = new BatchJobRepository(context);

        var result = await repository.GetJobsAsync(1, 10, status: BatchJobStatus.Completed);

        Assert.Single(result.Items);
        Assert.Equal("Amsterdam", result.Items[0].Target);
    }

    [Fact]
    public async Task GetJobsAsync_ShouldFilterByType()
    {
        using var context = new ValoraDbContext(_options);
        await SeedDatabase(context);
        var repository = new BatchJobRepository(context);

        var result = await repository.GetJobsAsync(1, 10, type: BatchJobType.CityIngestion);

        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, j => j.Target == "Amsterdam");
        Assert.Contains(result.Items, j => j.Target == "Utrecht");
    }

    [Fact]
    public async Task GetJobsAsync_ShouldSearchByPartialTarget()
    {
        using var context = new ValoraDbContext(_options);
        await SeedDatabase(context);
        var repository = new BatchJobRepository(context);

        var result = await repository.GetJobsAsync(1, 10, search: "dam");

        Assert.Equal(2, result.Items.Count); // Amsterdam, Rotterdam
        Assert.Contains(result.Items, j => j.Target == "Amsterdam");
        Assert.Contains(result.Items, j => j.Target == "Rotterdam");
        // Note: True case-insensitive behavior depends on the database collation and cannot be fully
        // validated with the InMemory provider. This test verifies that partial matching works as expected.
    }

    [Fact]
    public async Task GetJobsAsync_ShouldSortByCreatedAt()
    {
        using var context = new ValoraDbContext(_options);
        await SeedDatabase(context);
        var repository = new BatchJobRepository(context);

        // Default desc
        var resultDesc = await repository.GetJobsAsync(1, 10, sort: "createdAt_desc");
        Assert.Equal("Netherlands", resultDesc.Items[0].Target); // Newest
        Assert.Equal("Amsterdam", resultDesc.Items[3].Target); // Oldest

        // Asc
        var resultAsc = await repository.GetJobsAsync(1, 10, sort: "createdAt_asc");
        Assert.Equal("Amsterdam", resultAsc.Items[0].Target); // Oldest
        Assert.Equal("Netherlands", resultAsc.Items[3].Target); // Newest
    }

    [Fact]
    public async Task GetJobsAsync_ShouldSortByStatus()
    {
        using var context = new ValoraDbContext(_options);
        await SeedDatabase(context);
        var repository = new BatchJobRepository(context);

        // Status enum order: Pending(0), Processing(1), Completed(2), Failed(3)
        // Note: The Seed method uses Completed for Amsterdam, Processing for Rotterdam, Pending for Utrecht, Failed for Netherlands

        var resultAsc = await repository.GetJobsAsync(1, 10, sort: "status_asc");
        Assert.Equal(BatchJobStatus.Pending, resultAsc.Items[0].Status);

        var resultDesc = await repository.GetJobsAsync(1, 10, sort: "status_desc");
        Assert.Equal(BatchJobStatus.Failed, resultDesc.Items[0].Status);
    }

    [Fact]
    public async Task GetJobsAsync_ShouldSortByTarget()
    {
        using var context = new ValoraDbContext(_options);
        await SeedDatabase(context);
        var repository = new BatchJobRepository(context);

        var resultAsc = await repository.GetJobsAsync(1, 10, sort: "target_asc");
        Assert.Equal("Amsterdam", resultAsc.Items[0].Target);
        Assert.Equal("Utrecht", resultAsc.Items[3].Target); // "Netherlands", "Rotterdam", "Utrecht" ? Alphabetical order

        // "Amsterdam" -> "Netherlands" -> "Rotterdam" -> "Utrecht"
        // Wait, "N", "R", "U". Yes.

        var resultDesc = await repository.GetJobsAsync(1, 10, sort: "target_desc");
        Assert.Equal("Utrecht", resultDesc.Items[0].Target);
    }

    [Fact]
    public async Task GetJobsAsync_ShouldSortByType()
    {
        using var context = new ValoraDbContext(_options);
        await SeedDatabase(context);
        var repository = new BatchJobRepository(context);

        // Enum: CityIngestion(0), MapGeneration(1), AllCitiesIngestion(2)
        // Seed:
        // Amsterdam: CityIngestion (0)
        // Rotterdam: MapGeneration (1)
        // Utrecht: CityIngestion (0)
        // Netherlands: AllCitiesIngestion (2)

        var resultAsc = await repository.GetJobsAsync(1, 10, sort: "type_asc");
        Assert.Contains(resultAsc.Items[0].Type, new[] { BatchJobType.CityIngestion, BatchJobType.CityIngestion }); // Should be 0
        Assert.Equal(BatchJobType.AllCitiesIngestion, resultAsc.Items[3].Type); // Should be 2

        var resultDesc = await repository.GetJobsAsync(1, 10, sort: "type_desc");
        Assert.Equal(BatchJobType.AllCitiesIngestion, resultDesc.Items[0].Type); // Should be 2
        Assert.Contains(resultDesc.Items[3].Type, new[] { BatchJobType.CityIngestion, BatchJobType.CityIngestion }); // Should be 0
    }

    [Fact]
    public async Task GetJobsAsync_ShouldReturnEmpty_WhenSearchDoesNotMatch()
    {
        using var context = new ValoraDbContext(_options);
        await SeedDatabase(context);
        var repository = new BatchJobRepository(context);

        var result = await repository.GetJobsAsync(1, 10, search: "NonExistentCity");

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetJobsAsync_ShouldCombineFilters()
    {
        using var context = new ValoraDbContext(_options);
        await SeedDatabase(context);
        var repository = new BatchJobRepository(context);

        // Search "dam" (Amsterdam, Rotterdam)
        // Status Completed (Amsterdam)
        // Type CityIngestion (Amsterdam)

        var result = await repository.GetJobsAsync(1, 10,
            status: BatchJobStatus.Completed,
            type: BatchJobType.CityIngestion,
            search: "dam");

        Assert.Single(result.Items);
        Assert.Equal("Amsterdam", result.Items[0].Target);
    }
}
