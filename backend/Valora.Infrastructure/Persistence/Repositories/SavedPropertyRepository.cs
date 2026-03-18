using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Persistence.Repositories;

public class SavedPropertyRepository : ISavedPropertyRepository
{
    private readonly ValoraDbContext _context;

    public SavedPropertyRepository(ValoraDbContext context)
    {
        _context = context;
    }

    public async Task<SavedProperty?> GetSavedPropertyAsync(Guid workspaceId, Guid propertyId, CancellationToken ct = default)
    {
        return await _context.SavedProperties
            .Include(sp => sp.Property)
            .FirstOrDefaultAsync(sp => sp.WorkspaceId == workspaceId && sp.PropertyId == propertyId, ct);
    }

    public async Task<SavedProperty?> GetSavedPropertyByIdAsync(Guid savedPropertyId, CancellationToken ct = default)
    {
        return await _context.SavedProperties
            .Include(sp => sp.Property)
            .FirstOrDefaultAsync(sp => sp.Id == savedPropertyId, ct);
    }

    public async Task<List<SavedProperty>> GetSavedPropertiesAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _context.SavedProperties
            .AsNoTracking()
            .Include(sp => sp.Property)
            .Include(sp => sp.Comments)
            .Where(sp => sp.WorkspaceId == workspaceId)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<SavedPropertyDto>> GetSavedPropertyDtosAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _context.SavedProperties
            .AsNoTracking()
            .Where(sp => sp.WorkspaceId == workspaceId)
            .OrderByDescending(sp => sp.CreatedAt)
            .Select(sp => new SavedPropertyDto(
                sp.Id,
                sp.PropertyId,
                sp.Property != null ? new PropertySummaryDto(
                    sp.Property.Id,
                    sp.Property.Address,
                    sp.Property.City,
                    sp.Property.LivingAreaM2,
                    sp.Property.ContextSafetyScore,
                    sp.Property.ContextCompositeScore
                ) : null,
                sp.AddedByUserId,
                sp.Notes,
                sp.CreatedAt,
                sp.Comments.Count
            ))
            .ToListAsync(ct);
    }

    public Task<SavedProperty> AddSavedPropertyAsync(SavedProperty savedProperty, CancellationToken ct = default)
    {
        _context.SavedProperties.Add(savedProperty);
        return Task.FromResult(savedProperty);
    }

    public Task RemoveSavedPropertyAsync(SavedProperty savedProperty, CancellationToken ct = default)
    {
        _context.SavedProperties.Remove(savedProperty);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
