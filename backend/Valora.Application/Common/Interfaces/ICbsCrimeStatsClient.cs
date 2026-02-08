using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface ICbsCrimeStatsClient
{
    Task<CrimeStatsDto?> GetStatsAsync(ResolvedLocationDto location, CancellationToken cancellationToken = default);
}
