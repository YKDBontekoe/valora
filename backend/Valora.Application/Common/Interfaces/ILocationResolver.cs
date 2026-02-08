using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface ILocationResolver
{
    Task<ResolvedLocationDto?> ResolveAsync(string input, CancellationToken cancellationToken = default);
}
