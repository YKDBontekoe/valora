using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
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
            .Where(x => x.City == city)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DatasetStatusDto>> GetDatasetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Neighborhoods
            .AsNoTracking()
            .GroupBy(n => n.City)
            .Select(g => new DatasetStatusDto(
                g.Key,
                g.Count(),
                g.Max(n => n.LastUpdated)
            ))
            .OrderBy(d => d.City)
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

    public void AddRange(IEnumerable<Neighborhood> neighborhoods)
    {
        _context.Neighborhoods.AddRange(neighborhoods);
    }

    public void UpdateRange(IEnumerable<Neighborhood> neighborhoods)
    {
        var now = DateTime.UtcNow;
        foreach (var neighborhood in neighborhoods)
        {
            neighborhood.UpdatedAt = now;
            if (_context.Entry(neighborhood).State == EntityState.Detached)
            {
                _context.Neighborhoods.Attach(neighborhood);
                _context.Entry(neighborhood).State = EntityState.Modified;
            }
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
