using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Repositories;

public class ContextCacheRepository : IContextCacheRepository
{
    private readonly ValoraDbContext _db;

    public ContextCacheRepository(ValoraDbContext db)
    {
        _db = db;
    }

    public async Task<CbsNeighborhoodStats?> GetNeighborhoodStatsAsync(string regionCode, CancellationToken ct)
    {
        return await _db.CbsNeighborhoodStats
            .FirstOrDefaultAsync(s => s.RegionCode == regionCode && s.ExpiresAtUtc > DateTimeOffset.UtcNow, ct);
    }

    public async Task UpsertNeighborhoodStatsAsync(CbsNeighborhoodStats stats, CancellationToken ct)
    {
        var existing = await _db.CbsNeighborhoodStats
            .FirstOrDefaultAsync(s => s.RegionCode == stats.RegionCode, ct);

        if (existing != null)
        {
            _db.Entry(existing).CurrentValues.SetValues(stats);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.CbsNeighborhoodStats.Add(stats);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<CbsCrimeStats?> GetCrimeStatsAsync(string regionCode, CancellationToken ct)
    {
        return await _db.CbsCrimeStats
            .FirstOrDefaultAsync(s => s.RegionCode == regionCode && s.ExpiresAtUtc > DateTimeOffset.UtcNow, ct);
    }

    public async Task UpsertCrimeStatsAsync(CbsCrimeStats stats, CancellationToken ct)
    {
        var existing = await _db.CbsCrimeStats
            .FirstOrDefaultAsync(s => s.RegionCode == stats.RegionCode, ct);

        if (existing != null)
        {
            _db.Entry(existing).CurrentValues.SetValues(stats);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.CbsCrimeStats.Add(stats);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<AirQualitySnapshot?> GetAirQualitySnapshotAsync(string stationId, CancellationToken ct)
    {
        return await _db.AirQualitySnapshots
            .FirstOrDefaultAsync(s => s.StationId == stationId && s.ExpiresAtUtc > DateTimeOffset.UtcNow, ct);
    }

    public async Task UpsertAirQualitySnapshotAsync(AirQualitySnapshot snapshot, CancellationToken ct)
    {
        var existing = await _db.AirQualitySnapshots
            .FirstOrDefaultAsync(s => s.StationId == snapshot.StationId, ct);

        if (existing != null)
        {
            _db.Entry(existing).CurrentValues.SetValues(snapshot);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.AirQualitySnapshots.Add(snapshot);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<AmenityCache?> GetAmenityCacheAsync(string locationKey, CancellationToken ct)
    {
        return await _db.AmenityCaches
            .FirstOrDefaultAsync(s => s.LocationKey == locationKey && s.ExpiresAtUtc > DateTimeOffset.UtcNow, ct);
    }

    public async Task UpsertAmenityCacheAsync(AmenityCache cache, CancellationToken ct)
    {
        var existing = await _db.AmenityCaches
            .FirstOrDefaultAsync(s => s.LocationKey == cache.LocationKey, ct);

        if (existing != null)
        {
            _db.Entry(existing).CurrentValues.SetValues(cache);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.AmenityCaches.Add(cache);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<SourceMetadata?> GetSourceMetadataAsync(string source, CancellationToken ct)
    {
        return await _db.SourceMetadata
            .FirstOrDefaultAsync(s => s.Source == source && s.IsActive, ct);
    }

    public async Task UpdateSourceMetadataAsync(SourceMetadata metadata, CancellationToken ct)
    {
        var existing = await _db.SourceMetadata
            .FirstOrDefaultAsync(s => s.Source == metadata.Source, ct);

        if (existing != null)
        {
            _db.Entry(existing).CurrentValues.SetValues(metadata);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.SourceMetadata.Add(metadata);
        }

        await _db.SaveChangesAsync(ct);
    }
}
