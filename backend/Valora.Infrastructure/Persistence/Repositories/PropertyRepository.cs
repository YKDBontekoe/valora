using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Persistence.Repositories;

public class PropertyRepository : IPropertyRepository
{
    private readonly ValoraDbContext _context;

    public PropertyRepository(ValoraDbContext context)
    {
        _context = context;
    }

    public async Task<Property?> GetPropertyAsync(Guid propertyId, CancellationToken ct = default)
    {
        return await _context.Properties.FindAsync(new object[] { propertyId }, ct);
    }

    public async Task<Property?> GetPropertyByBagIdAsync(string bagId, CancellationToken ct = default)
    {
        return await _context.Properties.FirstOrDefaultAsync(p => p.BagId == bagId, ct);
    }

    public async Task<Property> AddPropertyAsync(Property property, CancellationToken ct = default)
    {
        _context.Properties.Add(property);
        await _context.SaveChangesAsync(ct);
        return property;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
