using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Repositories;

public class NeighborhoodRepository : INeighborhoodRepository
{
    private readonly ValoraDbContext _context;

    public NeighborhoodRepository(ValoraDbContext context)
    {
        _context = context;
    }

    public async Task<Neighborhood?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Neighborhoods.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<List<Neighborhood>> GetByCityAsync(string city, CancellationToken cancellationToken = default)
    {
        return await _context.Neighborhoods
            .Where(x => x.City.ToLower() == city.ToLower())
            .ToListAsync(cancellationToken);
    }

    public async Task<Neighborhood> AddAsync(Neighborhood neighborhood, CancellationToken cancellationToken = default)
    {
        _context.Neighborhoods.Add(neighborhood);
        await _context.SaveChangesAsync(cancellationToken);
        return neighborhood;
    }

    public async Task UpdateAsync(Neighborhood neighborhood, CancellationToken cancellationToken = default)
    {
        neighborhood.UpdatedAt = DateTime.UtcNow;
        _context.Entry(neighborhood).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
