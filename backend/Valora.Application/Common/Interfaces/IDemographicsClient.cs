using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IDemographicsClient
{
    Task<DemographicsDto?> GetDemographicsAsync(ResolvedLocationDto location, CancellationToken cancellationToken = default);
}
