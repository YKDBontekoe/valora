using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Persistence.Repositories;

public class PropertyCommentRepository : IPropertyCommentRepository
{
    private readonly ValoraDbContext _context;

    public PropertyCommentRepository(ValoraDbContext context)
    {
        _context = context;
    }

    public Task<PropertyComment> AddCommentAsync(PropertyComment comment, CancellationToken ct = default)
    {
        _context.PropertyComments.Add(comment);
        return Task.FromResult(comment);
    }

    public async Task<PropertyComment?> GetCommentAsync(Guid commentId, CancellationToken ct = default)
    {
        return await _context.PropertyComments.FindAsync(new object[] { commentId }, ct);
    }

    public async Task<List<PropertyComment>> GetCommentsAsync(Guid savedPropertyId, CancellationToken ct = default)
    {
        return await _context.PropertyComments
            .AsNoTracking()
            .Where(c => c.SavedPropertyId == savedPropertyId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
