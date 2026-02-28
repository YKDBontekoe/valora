using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Persistence.Repositories;

public class WorkspaceRepository : IWorkspaceRepository
{
    private readonly ValoraDbContext _context;

    public WorkspaceRepository(ValoraDbContext context)
    {
        _context = context;
    }

    // Workspace Management
    public Task<Workspace> AddAsync(Workspace workspace, CancellationToken ct = default)
    {
        _context.Workspaces.Add(workspace);
        return Task.FromResult(workspace);
    }

    public async Task<List<Workspace>> GetUserWorkspacesAsync(string userId, CancellationToken ct = default)
    {
        return await _context.Workspaces
            .AsNoTracking()
            .Include(w => w.Members)
            .Include(w => w.SavedProperties)
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<WorkspaceDto>> GetUserWorkspaceDtosAsync(string userId, CancellationToken ct = default)
    {
        return await _context.Workspaces
            .AsNoTracking()
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WorkspaceDto(
                w.Id,
                w.Name,
                w.Description,
                w.OwnerId,
                w.CreatedAt,
                w.Members.Count,
                w.SavedProperties.Count
            ))
            .ToListAsync(ct);
    }

    public async Task<int> GetUserOwnedWorkspacesCountAsync(string userId, CancellationToken ct = default)
    {
        return await _context.Workspaces
            .AsNoTracking()
            .Where(w => w.Members.Any(m => m.UserId == userId && m.Role == WorkspaceRole.Owner))
            .CountAsync(ct);
    }

    public async Task<(WorkspaceDto? Dto, bool IsMember)> GetWorkspaceDtoAndMemberStatusAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var result = await _context.Workspaces
            .AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new ValueTuple<WorkspaceDto, bool>(
                new WorkspaceDto(
                    w.Id,
                    w.Name,
                    w.Description,
                    w.OwnerId,
                    w.CreatedAt,
                    w.Members.Count,
                    w.SavedProperties.Count
                ),
                w.Members.Any(m => m.UserId == userId)
            ))
            .FirstOrDefaultAsync(ct);

        return result.Item1 == null ? (null, false) : (result.Item1, result.Item2);
    }

    public async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Workspaces
            .Include(w => w.Members)
            .Include(w => w.SavedProperties)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task<Workspace?> GetByIdWithMembersAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public Task UpdateAsync(Workspace workspace, CancellationToken ct = default)
    {
        _context.Entry(workspace).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Workspace workspace, CancellationToken ct = default)
    {
        _context.Workspaces.Remove(workspace);
        return Task.CompletedTask;
    }

    // Member Management
    public async Task<List<WorkspaceMember>> GetMembersAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _context.WorkspaceMembers
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.WorkspaceId == workspaceId)
            .ToListAsync(ct);
    }

    public async Task<WorkspaceMember?> GetMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _context.WorkspaceMembers.FindAsync(new object[] { memberId }, ct);
    }

    public async Task<WorkspaceMember?> GetMemberByEmailAsync(Guid workspaceId, string email, CancellationToken ct = default)
    {
        return await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId &&
                ((m.UserId != null && m.User!.Email == email) || m.InvitedEmail == email), ct);
    }

    public Task AddMemberAsync(WorkspaceMember member, CancellationToken ct = default)
    {
        _context.WorkspaceMembers.Add(member);
        return Task.CompletedTask;
    }

    public Task RemoveMemberAsync(WorkspaceMember member, CancellationToken ct = default)
    {
        _context.WorkspaceMembers.Remove(member);
        return Task.CompletedTask;
    }

    public async Task<bool> IsMemberAsync(Guid workspaceId, string userId, CancellationToken ct = default)
    {
        return await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);
    }

    public async Task<WorkspaceRole> GetUserRoleAsync(Guid workspaceId, string userId, CancellationToken ct = default)
    {
        var member = await _context.WorkspaceMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

        if (member == null) throw new ForbiddenAccessException();
        return member.Role;
    }

    // Saved Properties Management
    public async Task<SavedProperty?> GetSavedPropertyAsync(Guid workspaceId, Guid propertyId, CancellationToken ct = default)
    {
        return await _context.SavedProperties
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

    // Property Queries
    public async Task<Property?> GetPropertyAsync(Guid propertyId, CancellationToken ct = default)
    {
        return await _context.Properties.FindAsync(new object[] { propertyId }, ct);
    }

    // Comment Management
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

    // Activity Logs
    public Task LogActivityAsync(ActivityLog log, CancellationToken ct = default)
    {
        _context.ActivityLogs.Add(log);
        return Task.CompletedTask;
    }

    public async Task<List<ActivityLog>> GetActivityLogsAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .ToListAsync(ct);
    }

    public async Task<List<ActivityLogDto>> GetActivityLogDtosAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .Select(a => new ActivityLogDto(
                a.Id,
                a.ActorId,
                a.Type.ToString(),
                a.Summary,
                a.CreatedAt,
                a.Metadata
            ))
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
