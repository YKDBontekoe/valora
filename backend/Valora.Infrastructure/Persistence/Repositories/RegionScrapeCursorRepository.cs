using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Repositories;

public class RegionScrapeCursorRepository : IRegionScrapeCursorRepository
{
    private readonly ValoraDbContext _context;

    public RegionScrapeCursorRepository(ValoraDbContext context)
    {
        _context = context;
    }

    public async Task<RegionScrapeCursor> GetOrCreateAsync(string region, CancellationToken cancellationToken = default)
    {
        var normalized = region.ToLowerInvariant();
        var existing = await _context.RegionScrapeCursors
            .FirstOrDefaultAsync(c => c.Region == normalized, cancellationToken);

        if (existing != null)
        {
            return existing;
        }

        var cursor = new RegionScrapeCursor
        {
            Region = normalized,
            NextBackfillPage = 1
        };

        _context.RegionScrapeCursors.Add(cursor);
        await _context.SaveChangesAsync(cancellationToken);

        return cursor;
    }

    public async Task UpdateAsync(RegionScrapeCursor cursor, CancellationToken cancellationToken = default)
    {
        cursor.UpdatedAt = DateTime.UtcNow;
        _context.RegionScrapeCursors.Update(cursor);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
