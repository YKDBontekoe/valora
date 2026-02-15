using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Repositories;

public class ContextCacheRepositoryTests
{
    private readonly DbContextOptions<ValoraDbContext> _options;

    public ContextCacheRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task NeighborhoodStats_UpsertAndGet_ShouldWork()
    {
        using var context = new ValoraDbContext(_options);
        var repository = new ContextCacheRepository(context);
        var stats = new CbsNeighborhoodStats
        {
            RegionCode = "BU03630101",
            Residents = 100,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1)
        };

        await repository.UpsertNeighborhoodStatsAsync(stats, CancellationToken.None);
        var retrieved = await repository.GetNeighborhoodStatsAsync("BU03630101", CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(100, retrieved.Residents);

        // Update
        stats.Residents = 200;
        await repository.UpsertNeighborhoodStatsAsync(stats, CancellationToken.None);
        var updated = await repository.GetNeighborhoodStatsAsync("BU03630101", CancellationToken.None);
        Assert.Equal(200, updated!.Residents);
    }

    [Fact]
    public async Task GetNeighborhoodStats_ShouldNotReturnExpired()
    {
        using var context = new ValoraDbContext(_options);
        var repository = new ContextCacheRepository(context);
        var stats = new CbsNeighborhoodStats
        {
            RegionCode = "EXPIRED",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(-1)
        };

        await repository.UpsertNeighborhoodStatsAsync(stats, CancellationToken.None);
        var retrieved = await repository.GetNeighborhoodStatsAsync("EXPIRED", CancellationToken.None);

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task CrimeStats_UpsertAndGet_ShouldWork()
    {
        using var context = new ValoraDbContext(_options);
        var repository = new ContextCacheRepository(context);
        var stats = new CbsCrimeStats
        {
            RegionCode = "BU03630101",
            TotalCrimesPer1000 = 50,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1)
        };

        await repository.UpsertCrimeStatsAsync(stats, CancellationToken.None);
        var retrieved = await repository.GetCrimeStatsAsync("BU03630101", CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(50, retrieved.TotalCrimesPer1000);
    }

    [Fact]
    public async Task AirQuality_UpsertAndGet_ShouldWork()
    {
        using var context = new ValoraDbContext(_options);
        var repository = new ContextCacheRepository(context);
        var snapshot = new AirQualitySnapshot
        {
            StationId = "S1",
            Pm25 = 12.5,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1)
        };

        await repository.UpsertAirQualitySnapshotAsync(snapshot, CancellationToken.None);
        var retrieved = await repository.GetAirQualitySnapshotAsync("S1", CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(12.5, retrieved.Pm25);
    }

    [Fact]
    public async Task AmenityCache_UpsertAndGet_ShouldWork()
    {
        using var context = new ValoraDbContext(_options);
        var repository = new ContextCacheRepository(context);
        var cache = new AmenityCache
        {
            LocationKey = "test-loc",
            SchoolCount = 5,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1)
        };

        await repository.UpsertAmenityCacheAsync(cache, CancellationToken.None);
        var retrieved = await repository.GetAmenityCacheAsync("test-loc", CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(5, retrieved.SchoolCount);
    }

    [Fact]
    public async Task SourceMetadata_UpdateAndGet_ShouldWork()
    {
        using var context = new ValoraDbContext(_options);
        var repository = new ContextCacheRepository(context);
        var meta = new SourceMetadata
        {
            Source = "CBS",
            DatasetId = "85618NED",
            LastCheckedAtUtc = DateTimeOffset.UtcNow
        };

        await repository.UpdateSourceMetadataAsync(meta, CancellationToken.None);
        var retrieved = await repository.GetSourceMetadataAsync("CBS", CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal("85618NED", retrieved.DatasetId);
    }
}
