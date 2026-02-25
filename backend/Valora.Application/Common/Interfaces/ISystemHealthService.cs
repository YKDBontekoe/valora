using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface ISystemHealthService
{
    Task<SystemHealthDto> GetHealthAsync(CancellationToken ct);
}
