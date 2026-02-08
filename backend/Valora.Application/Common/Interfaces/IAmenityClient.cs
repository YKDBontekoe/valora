using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IAmenityClient
{
    Task<AmenityStatsDto?> GetAmenitiesAsync(ResolvedLocationDto location, int radiusMeters, CancellationToken cancellationToken = default);
}
