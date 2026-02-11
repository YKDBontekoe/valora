using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IPdokSoilClient
{
    Task<FoundationRiskDto?> GetFoundationRiskAsync(ResolvedLocationDto location, CancellationToken cancellationToken = default);
}
