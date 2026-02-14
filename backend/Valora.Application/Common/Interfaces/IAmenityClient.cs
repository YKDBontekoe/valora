using Valora.Application.DTOs;
using Valora.Application.DTOs.Map;

namespace Valora.Application.Common.Interfaces;

public interface IAmenityClient
{
    Task<AmenityStatsDto?> GetAmenitiesAsync(ResolvedLocationDto location, int radiusMeters, CancellationToken cancellationToken = default);

    Task<List<MapAmenityDto>> GetAmenitiesInBboxAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        List<string>? types = null,
        CancellationToken cancellationToken = default);
}
