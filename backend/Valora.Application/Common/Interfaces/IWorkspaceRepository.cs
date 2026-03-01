using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IWorkspaceRepository
{
    // Workspace Management
    Task<Workspace> AddAsync(Workspace workspace, CancellationToken ct = default);
    Task<List<Workspace>> GetUserWorkspacesAsync(string userId, CancellationToken ct = default);
    /// <summary>
    /// Gets projected DTOs for workspaces where the user is a member, ordered by creation date.
    /// </summary>
    Task<List<WorkspaceDto>> GetUserWorkspaceDtosAsync(string userId, CancellationToken ct = default);
    /// <summary>
    /// Returns the count of workspaces where the user is an owner.
    /// </summary>
    Task<int> GetUserOwnedWorkspacesCountAsync(string userId, CancellationToken ct = default);
    Task<(WorkspaceDto? Dto, bool IsMember)> GetWorkspaceDtoAndMemberStatusAsync(Guid id, string userId, CancellationToken ct = default);
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Workspace?> GetByIdWithMembersAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(Workspace workspace, CancellationToken ct = default);
    Task DeleteAsync(Workspace workspace, CancellationToken ct = default);

    // Member Management
    Task<List<WorkspaceMember>> GetMembersAsync(Guid workspaceId, CancellationToken ct = default);
    Task<WorkspaceMember?> GetMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<WorkspaceMember?> GetMemberByEmailAsync(Guid workspaceId, string email, CancellationToken ct = default);
    Task AddMemberAsync(WorkspaceMember member, CancellationToken ct = default);
    Task RemoveMemberAsync(WorkspaceMember member, CancellationToken ct = default);
    Task<bool> IsMemberAsync(Guid workspaceId, string userId, CancellationToken ct = default);
    Task<WorkspaceRole> GetUserRoleAsync(Guid workspaceId, string userId, CancellationToken ct = default);

    // Saved Properties Management
    Task<SavedProperty?> GetSavedPropertyAsync(Guid workspaceId, Guid propertyId, CancellationToken ct = default);
    Task<SavedProperty?> GetSavedPropertyByIdAsync(Guid savedPropertyId, CancellationToken ct = default);
    Task<List<SavedProperty>> GetSavedPropertiesAsync(Guid workspaceId, CancellationToken ct = default);
    /// <summary>
    /// Gets projected DTOs for saved properties in a workspace.
    /// </summary>
    Task<List<SavedPropertyDto>> GetSavedPropertyDtosAsync(Guid workspaceId, CancellationToken ct = default);
    Task<SavedProperty> AddSavedPropertyAsync(SavedProperty savedProperty, CancellationToken ct = default);
    Task RemoveSavedPropertyAsync(SavedProperty savedProperty, CancellationToken ct = default);

    // Property Queries (for checks)
    Task<Property?> GetPropertyAsync(Guid propertyId, CancellationToken ct = default);
    Task<Property?> GetPropertyByBagIdAsync(string bagId, CancellationToken ct = default);
    Task<Property> AddPropertyAsync(Property property, CancellationToken ct = default);

    // Comment Management
    Task<PropertyComment> AddCommentAsync(PropertyComment comment, CancellationToken ct = default);
    Task<PropertyComment?> GetCommentAsync(Guid commentId, CancellationToken ct = default);
    Task<List<PropertyComment>> GetCommentsAsync(Guid savedPropertyId, CancellationToken ct = default);

    // Activity Logs
    Task LogActivityAsync(ActivityLog log, CancellationToken ct = default);
    Task<List<ActivityLog>> GetActivityLogsAsync(Guid workspaceId, CancellationToken ct = default);
    /// <summary>
    /// Gets projected DTOs for activity logs in a workspace.
    /// </summary>
    Task<List<ActivityLogDto>> GetActivityLogDtosAsync(Guid workspaceId, CancellationToken ct = default);

    // Persistence
    Task SaveChangesAsync(CancellationToken ct = default);
}
