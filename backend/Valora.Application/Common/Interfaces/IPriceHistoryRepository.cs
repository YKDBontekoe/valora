using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IPriceHistoryRepository
{
    Task<IEnumerable<PriceHistory>> GetByListingIdAsync(Guid listingId, CancellationToken cancellationToken = default);
    Task<PriceHistory?> GetLatestByListingIdAsync(Guid listingId, CancellationToken cancellationToken = default);
    Task<PriceHistory> AddAsync(PriceHistory priceHistory, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<PriceHistory> priceHistories, CancellationToken cancellationToken = default);
}
