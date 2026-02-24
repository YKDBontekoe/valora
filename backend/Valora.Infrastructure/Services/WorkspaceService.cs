using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly ValoraDbContext _context;
    private readonly IIdentityService _identityService;

    public WorkspaceService(ValoraDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<WorkspaceDto> CreateWorkspaceAsync(string userId, CreateWorkspaceDto dto, CancellationToken ct = default)
    {
        var workspace = new Workspace
        {
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = userId,
            Members = new List<WorkspaceMember>
            {
                new WorkspaceMember
                {
                    UserId = userId,
                    Role = WorkspaceRole.Owner,
                    JoinedAt = DateTime.UtcNow
                }
            }
        };

        _context.Workspaces.Add(workspace);

        await LogActivityAsync(workspace, userId, ActivityLogType.WorkspaceCreated, $"Workspace '{dto.Name}' created", ct);

        await _context.SaveChangesAsync(ct);

        return MapToDto(workspace);
    }

    public async Task<WorkspaceDto> UpdateWorkspaceAsync(string userId, Guid workspaceId, UpdateWorkspaceDto dto, CancellationToken ct = default)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Members)
            .Include(w => w.SavedListings)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, ct);

        if (workspace == null) throw new NotFoundException(nameof(Workspace), workspaceId);

        if (workspace.OwnerId != userId) throw new ForbiddenAccessException();

        workspace.Name = dto.Name;
        workspace.Description = dto.Description;

        await LogActivityAsync(workspace, userId, ActivityLogType.WorkspaceUpdated, $"Updated workspace details", ct);
        await _context.SaveChangesAsync(ct);

        return MapToDto(workspace);
    }

    public async Task DeleteWorkspaceAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        var workspace = await _context.Workspaces.FindAsync(new object[] { workspaceId }, ct);

        if (workspace == null) throw new NotFoundException(nameof(Workspace), workspaceId);

        if (workspace.OwnerId != userId) throw new ForbiddenAccessException();

        await LogActivityAsync(workspace, userId, ActivityLogType.WorkspaceDeleted, $"Workspace deleted: {workspace.Name} ({workspace.Id})", ct);
        await _context.SaveChangesAsync(ct); // Save log first (if SetNull behavior is used, log will persist as orphaned)

        _context.Workspaces.Remove(workspace);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<List<WorkspaceDto>> GetUserWorkspacesAsync(string userId, CancellationToken ct = default)
    {
        var workspaces = await _context.Workspaces
            .AsNoTracking()
            .Include(w => w.Members)
            .Include(w => w.SavedListings)
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);

        return workspaces.Select(MapToDto).ToList();
    }

    public async Task<WorkspaceDto> GetWorkspaceAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        var workspace = await _context.Workspaces
            .AsNoTracking()
            .Include(w => w.Members)
            .Include(w => w.SavedListings)
            .FirstOrDefaultAsync(w => w.Id == workspaceId, ct);

        if (workspace == null) throw new NotFoundException(nameof(Workspace), workspaceId);

        if (!workspace.Members.Any(m => m.UserId == userId))
            throw new ForbiddenAccessException();

        return MapToDto(workspace);
    }

    public async Task<List<WorkspaceMemberDto>> GetMembersAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        var members = await _context.WorkspaceMembers
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.WorkspaceId == workspaceId)
            .ToListAsync(ct);

        return members.Select(m => new WorkspaceMemberDto(
            m.Id,
            m.UserId,
            m.User?.Email ?? m.InvitedEmail,
            m.Role,
            m.IsPending,
            m.JoinedAt
        )).ToList();
    }

    public async Task AddMemberAsync(string userId, Guid workspaceId, InviteMemberDto dto, CancellationToken ct = default)
    {
        var role = await GetUserRole(userId, workspaceId, ct);
        if (role != WorkspaceRole.Owner) throw new ForbiddenAccessException();

        var existingMember = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId &&
                ((m.UserId != null && m.User!.Email == dto.Email) || m.InvitedEmail == dto.Email), ct);

        if (existingMember != null) return;

        var invitedUser = await _identityService.GetUserByEmailAsync(dto.Email);

        var member = new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            Role = dto.Role,
            InvitedEmail = dto.Email,
            UserId = invitedUser?.Id,
            JoinedAt = invitedUser != null ? DateTime.UtcNow : (DateTime?)null
        };

        _context.WorkspaceMembers.Add(member);
        await LogActivityAsync(workspaceId, userId, ActivityLogType.MemberInvited, $"Invited {dto.Email} as {dto.Role}", ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(string userId, Guid workspaceId, Guid memberId, CancellationToken ct = default)
    {
        var currentUserRole = await GetUserRole(userId, workspaceId, ct);
        if (currentUserRole != WorkspaceRole.Owner) throw new ForbiddenAccessException();

        var member = await _context.WorkspaceMembers.FindAsync(new object[] { memberId }, ct);
        if (member == null || member.WorkspaceId != workspaceId) throw new NotFoundException(nameof(WorkspaceMember), memberId);

        if (member.UserId == userId) throw new InvalidOperationException("Cannot remove yourself.");

        _context.WorkspaceMembers.Remove(member);
        await LogActivityAsync(workspaceId, userId, ActivityLogType.MemberRemoved, $"Removed member {(member.InvitedEmail ?? member.UserId)}", ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<SavedListingDto> SaveListingAsync(string userId, Guid workspaceId, Guid listingId, string? notes, CancellationToken ct = default)
    {
        var role = await GetUserRole(userId, workspaceId, ct);
        if (role == WorkspaceRole.Viewer) throw new ForbiddenAccessException();

        var existing = await _context.SavedListings
            .FirstOrDefaultAsync(sl => sl.WorkspaceId == workspaceId && sl.ListingId == listingId, ct);

        if (existing != null) return MapToSavedListingDto(existing);

        var listing = await _context.Listings.FindAsync(new object[] { listingId }, ct);
        if (listing == null) throw new NotFoundException(nameof(Listing), listingId);

        var savedListing = new SavedListing
        {
            WorkspaceId = workspaceId,
            ListingId = listingId,
            AddedByUserId = userId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.SavedListings.Add(savedListing);
        await LogActivityAsync(workspaceId, userId, ActivityLogType.ListingSaved, $"Saved listing {listing.Address}", ct);
        await _context.SaveChangesAsync(ct);

        return MapToSavedListingDto(savedListing, listing);
    }

    public async Task<List<SavedListingDto>> GetSavedListingsAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        var savedListings = await _context.SavedListings
            .AsNoTracking()
            .Include(sl => sl.Listing)
            .Include(sl => sl.Comments)
            .Where(sl => sl.WorkspaceId == workspaceId)
            .OrderByDescending(sl => sl.CreatedAt)
            .ToListAsync(ct);

        return savedListings.Select(sl => MapToSavedListingDto(sl)).ToList();
    }

    public async Task RemoveSavedListingAsync(string userId, Guid workspaceId, Guid savedListingId, CancellationToken ct = default)
    {
        var role = await GetUserRole(userId, workspaceId, ct);
        if (role == WorkspaceRole.Viewer) throw new ForbiddenAccessException();

        var savedListing = await _context.SavedListings
            .Include(sl => sl.Listing)
            .FirstOrDefaultAsync(sl => sl.Id == savedListingId, ct);

        if (savedListing == null || savedListing.WorkspaceId != workspaceId)
            throw new NotFoundException(nameof(SavedListing), savedListingId);

        _context.SavedListings.Remove(savedListing);
        await LogActivityAsync(workspaceId, userId, ActivityLogType.ListingRemoved, $"Removed listing {savedListing.Listing?.Address ?? "Unknown"}", ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<CommentDto> AddCommentAsync(string userId, Guid workspaceId, Guid savedListingId, AddCommentDto dto, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        var savedListing = await _context.SavedListings.FindAsync(new object[] { savedListingId }, ct);
        if (savedListing == null || savedListing.WorkspaceId != workspaceId)
            throw new NotFoundException(nameof(SavedListing), savedListingId);

        var comment = new ListingComment
        {
            SavedListingId = savedListingId,
            UserId = userId,
            Content = dto.Content,
            ParentCommentId = dto.ParentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ListingComments.Add(comment);
        await LogActivityAsync(workspaceId, userId, ActivityLogType.CommentAdded, "Added a comment", ct);
        await _context.SaveChangesAsync(ct);

        return new CommentDto(
            comment.Id,
            comment.UserId,
            comment.Content,
            comment.CreatedAt,
            comment.ParentCommentId,
            new List<CommentDto>(),
            new Dictionary<string, List<string>>()
        );
    }

    public async Task<List<CommentDto>> GetCommentsAsync(string userId, Guid workspaceId, Guid savedListingId, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        var comments = await _context.ListingComments
            .AsNoTracking()
            .Where(c => c.SavedListingId == savedListingId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        var dtos = comments.Select(c => new CommentDto(
            c.Id,
            c.UserId,
            c.Content,
            c.CreatedAt,
            c.ParentCommentId,
            new List<CommentDto>(),
            c.Reactions ?? new Dictionary<string, List<string>>()
        )).ToList();

        var lookup = dtos.ToDictionary(c => c.Id);
        var rootComments = new List<CommentDto>();

        foreach (var c in dtos)
        {
            if (c.ParentId.HasValue && lookup.TryGetValue(c.ParentId.Value, out var parent))
            {
                parent.Replies.Add(c);
            }
            else
            {
                rootComments.Add(c);
            }
        }

        return rootComments;
    }

    public async Task<List<ActivityLogDto>> GetActivityLogsAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        var logs = await _context.ActivityLogs
            .AsNoTracking()
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        return logs.Select(a => new ActivityLogDto(
            a.Id,
            a.ActorId,
            a.Type.ToString(),
            a.Summary,
            a.CreatedAt,
            a.Metadata
        )).ToList();
    }

    // Helpers
    private async Task ValidateMemberAccess(string userId, Guid workspaceId, CancellationToken ct)
    {
        var isMember = await _context.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);
        if (!isMember) throw new ForbiddenAccessException();
    }

    private async Task<WorkspaceRole> GetUserRole(string userId, Guid workspaceId, CancellationToken ct)
    {
        var member = await _context.WorkspaceMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

        if (member == null) throw new ForbiddenAccessException();
        return member.Role;
    }

    private async Task LogActivityAsync(Guid workspaceId, string actorId, ActivityLogType type, string summary, CancellationToken ct)
    {
        var log = new ActivityLog
        {
            WorkspaceId = workspaceId,
            ActorId = actorId,
            Type = type,
            Summary = summary,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActivityLogs.Add(log);
    }

    private async Task LogActivityAsync(Workspace workspace, string actorId, ActivityLogType type, string summary, CancellationToken ct)
    {
        var log = new ActivityLog
        {
            Workspace = workspace,
            ActorId = actorId,
            Type = type,
            Summary = summary,
            CreatedAt = DateTime.UtcNow
        };
        _context.ActivityLogs.Add(log);
    }

    private static WorkspaceDto MapToDto(Workspace w)
    {
        return new WorkspaceDto(
            w.Id,
            w.Name,
            w.Description,
            w.OwnerId,
            w.CreatedAt,
            w.Members.Count,
            w.SavedListings.Count
        );
    }

    private SavedListingDto MapToSavedListingDto(SavedListing sl, Listing? l = null)
    {
        var listing = l ?? sl.Listing;
        return new SavedListingDto(
            sl.Id,
            sl.ListingId,
            listing != null ? new ListingSummaryDto(
                listing.Id,
                listing.Address,
                listing.City,
                listing.Price,
                listing.ImageUrl,
                listing.Bedrooms,
                listing.LivingAreaM2
            ) : null,
            sl.AddedByUserId,
            sl.Notes,
            sl.CreatedAt,
            sl.Comments?.Count ?? 0
        );
    }
}
