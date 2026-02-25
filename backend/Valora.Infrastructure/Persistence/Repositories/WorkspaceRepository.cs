using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
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
            .Include(w => w.SavedListings)
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> GetUserOwnedWorkspacesCountAsync(string userId, CancellationToken ct = default)
    {
        return await _context.Workspaces
            .AsNoTracking()
            .Where(w => w.Members.Any(m => m.UserId == userId && m.Role == WorkspaceRole.Owner))
            .CountAsync(ct);
    }

    public async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Workspaces
            .Include(w => w.Members)
            .Include(w => w.SavedListings)
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

    // Saved Listings Management
    public async Task<SavedListing?> GetSavedListingAsync(Guid workspaceId, Guid listingId, CancellationToken ct = default)
    {
        return await _context.SavedListings
            .FirstOrDefaultAsync(sl => sl.WorkspaceId == workspaceId && sl.ListingId == listingId, ct);
    }

    public async Task<SavedListing?> GetSavedListingByIdAsync(Guid savedListingId, CancellationToken ct = default)
    {
        return await _context.SavedListings
            .Include(sl => sl.Listing)
            .FirstOrDefaultAsync(sl => sl.Id == savedListingId, ct);
    }

    public async Task<List<SavedListing>> GetSavedListingsAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _context.SavedListings
            .AsNoTracking()
            .Include(sl => sl.Listing)
            .Include(sl => sl.Comments)
            .Where(sl => sl.WorkspaceId == workspaceId)
            .OrderByDescending(sl => sl.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<SavedListing> AddSavedListingAsync(SavedListing savedListing, CancellationToken ct = default)
    {
        _context.SavedListings.Add(savedListing);
        return Task.FromResult(savedListing);
    }

    public Task RemoveSavedListingAsync(SavedListing savedListing, CancellationToken ct = default)
    {
        _context.SavedListings.Remove(savedListing);
        return Task.CompletedTask;
    }

    // Listing Queries
    public async Task<Listing?> GetListingAsync(Guid listingId, CancellationToken ct = default)
    {
        return await _context.Listings.FindAsync(new object[] { listingId }, ct);
    }

    // Comment Management
    public Task<ListingComment> AddCommentAsync(ListingComment comment, CancellationToken ct = default)
    {
        _context.ListingComments.Add(comment);
        return Task.FromResult(comment);
    }

    public async Task<List<ListingComment>> GetCommentsAsync(Guid savedListingId, CancellationToken ct = default)
    {
        return await _context.ListingComments
            .AsNoTracking()
            .Where(c => c.SavedListingId == savedListingId)
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

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
