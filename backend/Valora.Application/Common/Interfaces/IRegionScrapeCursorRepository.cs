using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IRegionScrapeCursorRepository
{
    Task<RegionScrapeCursor> GetOrCreateAsync(string region, CancellationToken cancellationToken = default);
    Task UpdateAsync(RegionScrapeCursor cursor, CancellationToken cancellationToken = default);
}
