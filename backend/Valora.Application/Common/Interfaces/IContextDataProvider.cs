using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IContextDataProvider
{
    Task<ContextSourceData> GetSourceDataAsync(ResolvedLocationDto location, int radiusMeters, CancellationToken cancellationToken);
}
