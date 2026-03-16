using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Application.Common.Extensions;

namespace Valora.Application.Services;

public class WorkspacePropertyService : IWorkspacePropertyService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IEventDispatcher _eventDispatcher;

    public WorkspacePropertyService(IWorkspaceRepository repository, IEventDispatcher eventDispatcher)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
    }

    /// <summary>
    /// Saves a property to a user's workspace, acting as a "bookmark" or "shortlist" action.
    /// </summary>
    /// <param name="userId">The ID of the user attempting to save the property.</param>
    /// <param name="workspaceId">The unique identifier of the target workspace.</param>
    /// <param name="propertyId">The unique identifier of the property to be saved.</param>
    /// <param name="notes">Optional user notes attached to the saved property.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// <para>
    /// <strong>Why verify roles?</strong> Valora workspaces can be shared. A user with a 'Viewer'
    /// role should only be able to view properties, not modify the workspace content.
    /// This prevents unauthorized additions to shared team or family workspaces.
    /// </para>
    /// <para>
    /// <strong>Idempotency:</strong> If the property is already saved in the workspace,
    /// the method returns the existing record rather than throwing an error or duplicating the entry.
    /// </para>
    /// </remarks>
    /// <returns>A DTO containing the details of the newly saved or existing property.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown if the user does not have sufficient permissions (e.g., Viewer role).</exception>
    /// <exception cref="NotFoundException">Thrown if the property does not exist in the system.</exception>
    public async Task<SavedPropertyDto> SavePropertyAsync(string userId, Guid workspaceId, Guid propertyId, string? notes, CancellationToken ct = default)
    {
        var role = await GetUserRole(userId, workspaceId, ct);
        if (role == WorkspaceRole.Viewer) throw new ForbiddenAccessException();

        var existing = await _repository.GetSavedPropertyAsync(workspaceId, propertyId, ct);
        if (existing != null) return MapToSavedPropertyDto(existing);

        var property = await _repository.GetPropertyAsync(propertyId, ct);
        if (property == null) throw new NotFoundException(nameof(Property), propertyId);

        var savedProperty = new SavedProperty
        {
            WorkspaceId = workspaceId,
            PropertyId = propertyId,
            AddedByUserId = userId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddSavedPropertyAsync(savedProperty, ct);
        await _repository.LogActivityEventAsync(workspaceId, userId, ActivityLogType.PropertySaved, $"Saved property {property.Address}", ct);

        await _repository.SaveChangesAsync(ct);
        await _eventDispatcher.DispatchAsync(new Valora.Application.Common.Events.ReportSavedToWorkspaceEvent(workspaceId, propertyId, userId), ct);

        return MapToSavedPropertyDto(savedProperty, property);
    }

    public async Task<SavedPropertyDto> SaveContextReportAsync(string userId, Guid workspaceId, ContextReportDto report, string? notes, CancellationToken ct = default)
    {
        var role = await GetUserRole(userId, workspaceId, ct);
        if (role == WorkspaceRole.Viewer) throw new ForbiddenAccessException();

        // Check if property exists by Address/BAG ID
        // The ContextReportDto doesn't currently expose BagId, so we use Address as a fallback or if we can extract it.
        // For now, let's try Address.
        var property = await _repository.GetPropertyByBagIdAsync(report.Location.PostalCode + report.Location.DisplayAddress, ct); // Hypothetical unique key if no BagId
        
        if (property == null) {
            property = new Property {
                Address = report.Location.DisplayAddress,
                City = report.Location.MunicipalityName,
                PostalCode = report.Location.PostalCode,
                Latitude = report.Location.Latitude,
                Longitude = report.Location.Longitude,
                ContextCompositeScore = report.CompositeScore,
                ContextSafetyScore = report.CategoryScores.TryGetValue("Safety", out var safety) ? safety : null,
                ContextSocialScore = report.CategoryScores.TryGetValue("Social", out var social) ? social : null,
                ContextAmenitiesScore = report.CategoryScores.TryGetValue("Amenities", out var amenities) ? amenities : null,
                ContextEnvironmentScore = report.CategoryScores.TryGetValue("Environment", out var environment) ? environment : null,
            };
            await _repository.AddPropertyAsync(property, ct);
        }

        return await SavePropertyAsync(userId, workspaceId, property.Id, notes, ct);
    }

    public async Task<List<SavedPropertyDto>> GetSavedPropertiesAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        return await _repository.GetSavedPropertyDtosAsync(workspaceId, ct);
    }

    public async Task RemoveSavedPropertyAsync(string userId, Guid workspaceId, Guid savedPropertyId, CancellationToken ct = default)
    {
        var role = await GetUserRole(userId, workspaceId, ct);
        if (role == WorkspaceRole.Viewer) throw new ForbiddenAccessException();

        var savedProperty = await _repository.GetSavedPropertyByIdAsync(savedPropertyId, ct);

        if (savedProperty == null || savedProperty.WorkspaceId != workspaceId)
            throw new NotFoundException(nameof(SavedProperty), savedPropertyId);

        await _repository.RemoveSavedPropertyAsync(savedProperty, ct);
        await _repository.LogActivityEventAsync(workspaceId, userId, ActivityLogType.PropertyRemoved, $"Removed property {savedProperty.Property?.Address ?? "Unknown"}", ct);
        await _repository.SaveChangesAsync(ct);
    }

    public async Task<CommentDto> AddCommentAsync(string userId, Guid workspaceId, Guid savedPropertyId, AddCommentDto dto, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        var savedProperty = await _repository.GetSavedPropertyByIdAsync(savedPropertyId, ct);
        if (savedProperty == null || savedProperty.WorkspaceId != workspaceId)
            throw new NotFoundException(nameof(SavedProperty), savedPropertyId);

        if (dto.ParentId.HasValue)
        {
            var parent = await _repository.GetCommentAsync(dto.ParentId.Value, ct);
            if (parent == null || parent.SavedPropertyId != savedPropertyId)
                throw new InvalidOperationException("Parent comment must belong to the same property.");
        }

        var comment = new PropertyComment
        {
            SavedPropertyId = savedPropertyId,
            UserId = userId,
            Content = dto.Content,
            ParentCommentId = dto.ParentId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddCommentAsync(comment, ct);
        await _repository.LogActivityEventAsync(workspaceId, userId, ActivityLogType.CommentAdded, "Added a comment", ct);

        await _repository.SaveChangesAsync(ct);
        await _eventDispatcher.DispatchAsync(new Valora.Application.Common.Events.CommentAddedEvent(workspaceId, savedPropertyId, comment.Id, userId, comment.Content, comment.ParentCommentId), ct);

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

    public async Task<List<CommentDto>> GetCommentsAsync(string userId, Guid workspaceId, Guid savedPropertyId, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        var comments = await _repository.GetCommentsAsync(savedPropertyId, ct);

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

    private SavedPropertyDto MapToSavedPropertyDto(SavedProperty sp, Property? p = null)
    {
        var property = p ?? sp.Property;
        return new SavedPropertyDto(
            sp.Id,
            sp.PropertyId,
            property != null ? new PropertySummaryDto(
                property.Id,
                property.Address,
                property.City,
                property.LivingAreaM2,
                property.ContextSafetyScore,
                property.ContextCompositeScore
            ) : null,
            sp.AddedByUserId,
            sp.Notes,
            sp.CreatedAt,
            sp.Comments?.Count ?? 0
        );
    }
}
