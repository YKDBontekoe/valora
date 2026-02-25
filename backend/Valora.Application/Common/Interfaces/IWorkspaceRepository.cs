using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IWorkspaceRepository
{
    // Workspace Management
    Task<Workspace> AddAsync(Workspace workspace, CancellationToken ct = default);
    Task<List<Workspace>> GetUserWorkspacesAsync(string userId, CancellationToken ct = default);
    /// <summary>
    /// Returns the count of workspaces where the user is an owner.
    /// </summary>
    Task<int> GetUserOwnedWorkspacesCountAsync(string userId, CancellationToken ct = default);
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

    // Saved Listings Management
    Task<SavedListing?> GetSavedListingAsync(Guid workspaceId, Guid listingId, CancellationToken ct = default);
    Task<SavedListing?> GetSavedListingByIdAsync(Guid savedListingId, CancellationToken ct = default);
    Task<List<SavedListing>> GetSavedListingsAsync(Guid workspaceId, CancellationToken ct = default);
    Task<SavedListing> AddSavedListingAsync(SavedListing savedListing, CancellationToken ct = default);
    Task RemoveSavedListingAsync(SavedListing savedListing, CancellationToken ct = default);

    // Listing Queries (for checks)
    Task<Listing?> GetListingAsync(Guid listingId, CancellationToken ct = default);

    // Comment Management
    Task<ListingComment> AddCommentAsync(ListingComment comment, CancellationToken ct = default);
    Task<List<ListingComment>> GetCommentsAsync(Guid savedListingId, CancellationToken ct = default);
    Task<ListingComment?> GetCommentAsync(Guid commentId, CancellationToken ct = default);

    // Activity Logs
    Task LogActivityAsync(ActivityLog log, CancellationToken ct = default);
    Task<List<ActivityLog>> GetActivityLogsAsync(Guid workspaceId, CancellationToken ct = default);

    // Persistence
    Task SaveChangesAsync(CancellationToken ct = default);
}
