using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class WorkspaceListingService : IWorkspaceListingService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IActivityLogService _activityLogService;

    public WorkspaceListingService(IWorkspaceRepository repository, IActivityLogService activityLogService)
    {
        _repository = repository;
        _activityLogService = activityLogService;
    }

    public async Task<SavedListingDto> SaveListingAsync(string userId, Guid workspaceId, Guid listingId, string? notes, CancellationToken ct = default)
    {
        var role = await _repository.GetUserRoleAsync(workspaceId, userId, ct);
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
        await _activityLogService.LogActivityAsync(workspaceId, userId, ActivityLogType.ListingSaved, "Saved listing", ct); // Redacted PII

        await _repository.SaveChangesAsync(ct);

        return MapToSavedListingDto(savedListing, listing);
    }

    public async Task<List<SavedListingDto>> GetSavedListingsAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        var isMember = await _repository.IsMemberAsync(workspaceId, userId, ct);
        if (!isMember) throw new ForbiddenAccessException();

        var savedListings = await _repository.GetSavedListingsAsync(workspaceId, ct);

        return savedListings.Select(sl => MapToSavedListingDto(sl)).ToList();
    }

    public async Task RemoveSavedListingAsync(string userId, Guid workspaceId, Guid savedListingId, CancellationToken ct = default)
    {
        var role = await _repository.GetUserRoleAsync(workspaceId, userId, ct);
        if (role == WorkspaceRole.Viewer) throw new ForbiddenAccessException();

        var savedListing = await _repository.GetSavedListingByIdAsync(savedListingId, ct);

        if (savedListing == null || savedListing.WorkspaceId != workspaceId)
            throw new NotFoundException(nameof(SavedListing), savedListingId);

        await _repository.RemoveSavedListingAsync(savedListing, ct);
        await _activityLogService.LogActivityAsync(workspaceId, userId, ActivityLogType.ListingRemoved, "Removed listing", ct); // Redacted PII
        await _repository.SaveChangesAsync(ct);
    }

    public async Task<CommentDto> AddCommentAsync(string userId, Guid workspaceId, Guid savedListingId, AddCommentDto dto, CancellationToken ct = default)
    {
        var isMember = await _repository.IsMemberAsync(workspaceId, userId, ct);
        if (!isMember) throw new ForbiddenAccessException();

        var savedListing = await _repository.GetSavedListingByIdAsync(savedListingId, ct);
        if (savedListing == null || savedListing.WorkspaceId != workspaceId)
            throw new NotFoundException(nameof(SavedListing), savedListingId);

        // Validate ParentId
        if (dto.ParentId.HasValue)
        {
            var parent = await _repository.GetCommentAsync(dto.ParentId.Value, ct);
            if (parent == null || parent.SavedListingId != savedListingId)
            {
                throw new NotFoundException(nameof(ListingComment), dto.ParentId.Value);
            }
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
        await _activityLogService.LogActivityAsync(workspaceId, userId, ActivityLogType.CommentAdded, "Added a comment", ct);

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
        var isMember = await _repository.IsMemberAsync(workspaceId, userId, ct);
        if (!isMember) throw new ForbiddenAccessException();

        // Verify saved listing belongs to workspace
        var savedListing = await _repository.GetSavedListingByIdAsync(savedListingId, ct);
        if (savedListing == null || savedListing.WorkspaceId != workspaceId)
             throw new NotFoundException(nameof(SavedListing), savedListingId);

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
