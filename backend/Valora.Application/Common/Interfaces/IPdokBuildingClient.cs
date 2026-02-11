using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IPdokBuildingClient
{
    Task<SolarPotentialDto?> GetSolarPotentialAsync(ResolvedLocationDto location, CancellationToken cancellationToken = default);
}
