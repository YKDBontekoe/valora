using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Repositories;

public class PriceHistoryRepository : IPriceHistoryRepository
{
    private readonly ValoraDbContext _context;

    public PriceHistoryRepository(ValoraDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PriceHistory>> GetByListingIdAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        return await _context.PriceHistories
            .Where(ph => ph.ListingId == listingId)
            .OrderByDescending(ph => ph.RecordedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<PriceHistory?> GetLatestByListingIdAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        return await _context.PriceHistories
            .Where(ph => ph.ListingId == listingId)
            .OrderByDescending(ph => ph.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PriceHistory> AddAsync(PriceHistory priceHistory, CancellationToken cancellationToken = default)
    {
        _context.PriceHistories.Add(priceHistory);
        await _context.SaveChangesAsync(cancellationToken);
        return priceHistory;
    }

    public async Task AddRangeAsync(IEnumerable<PriceHistory> priceHistories, CancellationToken cancellationToken = default)
    {
        await _context.PriceHistories.AddRangeAsync(priceHistories, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
