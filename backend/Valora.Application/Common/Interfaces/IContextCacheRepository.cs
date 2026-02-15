using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IContextCacheRepository
{
    Task<CbsNeighborhoodStats?> GetNeighborhoodStatsAsync(string regionCode, CancellationToken ct);
    Task UpsertNeighborhoodStatsAsync(CbsNeighborhoodStats stats, CancellationToken ct);

    Task<CbsCrimeStats?> GetCrimeStatsAsync(string regionCode, CancellationToken ct);
    Task UpsertCrimeStatsAsync(CbsCrimeStats stats, CancellationToken ct);

    Task<AirQualitySnapshot?> GetAirQualitySnapshotAsync(string stationId, CancellationToken ct);
    Task UpsertAirQualitySnapshotAsync(AirQualitySnapshot snapshot, CancellationToken ct);

    Task<AmenityCache?> GetAmenityCacheAsync(string locationKey, CancellationToken ct);
    Task UpsertAmenityCacheAsync(AmenityCache cache, CancellationToken ct);

    Task<SourceMetadata?> GetSourceMetadataAsync(string source, CancellationToken ct);
    Task UpdateSourceMetadataAsync(SourceMetadata metadata, CancellationToken ct);
}
