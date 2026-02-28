using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class WorkspaceListingService : IWorkspaceListingService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IEventDispatcher _eventDispatcher;

    public WorkspaceListingService(IWorkspaceRepository repository, IEventDispatcher eventDispatcher)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
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
        await _eventDispatcher.DispatchAsync(new Valora.Application.Common.Events.ReportSavedToWorkspaceEvent(workspaceId, listingId, userId), ct);

        return MapToSavedListingDto(savedListing, listing);
    }

    public async Task<List<SavedListingDto>> GetSavedListingsAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        return await _repository.GetSavedListingDtosAsync(workspaceId, ct);
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

        if (dto.ParentId.HasValue)
        {
            var parent = await _repository.GetCommentAsync(dto.ParentId.Value, ct);
            if (parent == null || parent.SavedListingId != savedListingId)
                throw new InvalidOperationException("Parent comment must belong to the same listing.");
        }

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
        await _eventDispatcher.DispatchAsync(new Valora.Application.Common.Events.CommentAddedEvent(workspaceId, savedListingId, comment.Id, userId, comment.Content, comment.ParentCommentId), ct);

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

    private async Task LogActivityAsync(Guid? workspaceId, string actorId, ActivityLogType type, string summary, CancellationToken ct)
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
