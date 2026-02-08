using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IAirQualityClient
{
    Task<AirQualitySnapshotDto?> GetSnapshotAsync(ResolvedLocationDto location, CancellationToken cancellationToken = default);
}
