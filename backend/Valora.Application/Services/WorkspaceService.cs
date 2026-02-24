using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IIdentityService _identityService;

    public WorkspaceService(IWorkspaceRepository repository, IIdentityService identityService)
    {
        _repository = repository;
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

        await _repository.AddAsync(workspace, ct);

        await LogActivityAsync(workspace, userId, ActivityLogType.WorkspaceCreated, $"Workspace '{dto.Name}' created", ct);

        await _repository.SaveChangesAsync(ct);

        return MapToDto(workspace);
    }

    public async Task<List<WorkspaceDto>> GetUserWorkspacesAsync(string userId, CancellationToken ct = default)
    {
        var workspaces = await _repository.GetUserWorkspacesAsync(userId, ct);
        return workspaces.Select(MapToDto).ToList();
    }

    public async Task<WorkspaceDto> GetWorkspaceAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        var workspace = await _repository.GetByIdAsync(workspaceId, ct);

        if (workspace == null) throw new NotFoundException(nameof(Workspace), workspaceId);

        if (!workspace.Members.Any(m => m.UserId == userId))
            throw new ForbiddenAccessException();

        return MapToDto(workspace);
    }

    public async Task<List<WorkspaceMemberDto>> GetMembersAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        var members = await _repository.GetMembersAsync(workspaceId, ct);

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

        var existingMember = await _repository.GetMemberByEmailAsync(workspaceId, dto.Email, ct);
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

        await _repository.AddMemberAsync(member, ct);
        await LogActivityAsync(workspaceId, userId, ActivityLogType.MemberInvited, $"Invited {dto.Email} as {dto.Role}", ct);
        await _repository.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(string userId, Guid workspaceId, Guid memberId, CancellationToken ct = default)
    {
        var currentUserRole = await GetUserRole(userId, workspaceId, ct);
        if (currentUserRole != WorkspaceRole.Owner) throw new ForbiddenAccessException();

        var member = await _repository.GetMemberAsync(memberId, ct);
        if (member == null || member.WorkspaceId != workspaceId) throw new NotFoundException(nameof(WorkspaceMember), memberId);

        if (member.UserId == userId) throw new InvalidOperationException("Cannot remove yourself.");

        await _repository.RemoveMemberAsync(member, ct);
        await LogActivityAsync(workspaceId, userId, ActivityLogType.MemberRemoved, $"Removed member {(member.InvitedEmail ?? member.UserId)}", ct);
        await _repository.SaveChangesAsync(ct);
    }

    public async Task<SavedListingDto> SaveListingAsync(string userId, Guid workspaceId, Guid listingId, string? notes, CancellationToken ct = default)
    {
        var role = await GetUserRole(userId, workspaceId, ct);
        if (role == WorkspaceRole.Viewer) throw new ForbiddenAccessException();

        var existing = await _repository.GetSavedListingAsync(workspaceId, listingId, ct);
        if (existing != null) return MapToSavedListingDto(existing);

        var listing = await _repository.GetListingAsync(listingId, ct);
        if (listing == null) throw new NotFoundException(nameof(Listing), listingId);

        var savedListing = new SavedListing
        {
            WorkspaceId = workspaceId,
            ListingId = listingId,
            AddedByUserId = userId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddSavedListingAsync(savedListing, ct);
        await LogActivityAsync(workspaceId, userId, ActivityLogType.ListingSaved, $"Saved listing {listing.Address}", ct);

        await _repository.SaveChangesAsync(ct);

        return MapToSavedListingDto(savedListing, listing);
    }

    public async Task<List<SavedListingDto>> GetSavedListingsAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        var savedListings = await _repository.GetSavedListingsAsync(workspaceId, ct);

        return savedListings.Select(sl => MapToSavedListingDto(sl)).ToList();
    }

    public async Task RemoveSavedListingAsync(string userId, Guid workspaceId, Guid savedListingId, CancellationToken ct = default)
    {
        var role = await GetUserRole(userId, workspaceId, ct);
        if (role == WorkspaceRole.Viewer) throw new ForbiddenAccessException();

        var savedListing = await _repository.GetSavedListingByIdAsync(savedListingId, ct);

        if (savedListing == null || savedListing.WorkspaceId != workspaceId)
            throw new NotFoundException(nameof(SavedListing), savedListingId);

        await _repository.RemoveSavedListingAsync(savedListing, ct);
        await LogActivityAsync(workspaceId, userId, ActivityLogType.ListingRemoved, $"Removed listing {savedListing.Listing?.Address ?? "Unknown"}", ct);
        await _repository.SaveChangesAsync(ct);
    }

    public async Task<CommentDto> AddCommentAsync(string userId, Guid workspaceId, Guid savedListingId, AddCommentDto dto, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        var savedListing = await _repository.GetSavedListingByIdAsync(savedListingId, ct);
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

        await _repository.AddCommentAsync(comment, ct);
        await LogActivityAsync(workspaceId, userId, ActivityLogType.CommentAdded, "Added a comment", ct);

        await _repository.SaveChangesAsync(ct);

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

        var comments = await _repository.GetCommentsAsync(savedListingId, ct);

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

        var logs = await _repository.GetActivityLogsAsync(workspaceId, ct);

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
        var isMember = await _repository.IsMemberAsync(workspaceId, userId, ct);
        if (!isMember) throw new ForbiddenAccessException();
    }

    private async Task<WorkspaceRole> GetUserRole(string userId, Guid workspaceId, CancellationToken ct)
    {
        return await _repository.GetUserRoleAsync(workspaceId, userId, ct);
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
        await _repository.LogActivityAsync(log, ct);
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
        await _repository.LogActivityAsync(log, ct);
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
