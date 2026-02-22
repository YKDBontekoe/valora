using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface INeighborhoodRepository
{
    Task<Neighborhood?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<List<Neighborhood>> GetByCityAsync(string city, CancellationToken cancellationToken = default);
    Task<Neighborhood> AddAsync(Neighborhood neighborhood, CancellationToken cancellationToken = default);
    Task UpdateAsync(Neighborhood neighborhood, CancellationToken cancellationToken = default);

    // Batch operations
    void AddRange(IEnumerable<Neighborhood> neighborhoods);
    void UpdateRange(IEnumerable<Neighborhood> neighborhoods);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
