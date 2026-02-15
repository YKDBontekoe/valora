using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IContextDataProvider
{
    Task<ContextSourceData> GetSourceDataAsync(ResolvedLocationDto location, int radiusMeters, CancellationToken cancellationToken);

    Task<NeighborhoodStatsDto?> GetNeighborhoodStatsAsync(ResolvedLocationDto location, CancellationToken ct);
    Task<CrimeStatsDto?> GetCrimeStatsAsync(ResolvedLocationDto location, CancellationToken ct);
    Task<AmenityStatsDto?> GetAmenityStatsAsync(ResolvedLocationDto location, int radiusMeters, CancellationToken ct);
    Task<AirQualitySnapshotDto?> GetAirQualitySnapshotAsync(ResolvedLocationDto location, CancellationToken ct);
}
