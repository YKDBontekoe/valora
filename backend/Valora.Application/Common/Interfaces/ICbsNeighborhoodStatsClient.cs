using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface ICbsNeighborhoodStatsClient
{
    Task<NeighborhoodStatsDto?> GetStatsAsync(ResolvedLocationDto location, CancellationToken cancellationToken = default);
}
