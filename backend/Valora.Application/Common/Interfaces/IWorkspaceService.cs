using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IWorkspaceService
{
    Task<WorkspaceDto> CreateWorkspaceAsync(string userId, CreateWorkspaceDto dto, CancellationToken ct = default);
    Task<List<WorkspaceDto>> GetUserWorkspacesAsync(string userId, CancellationToken ct = default);
    Task<WorkspaceDto> GetWorkspaceAsync(string userId, Guid workspaceId, CancellationToken ct = default);

    Task<List<WorkspaceMemberDto>> GetMembersAsync(string userId, Guid workspaceId, CancellationToken ct = default);
    Task AddMemberAsync(string userId, Guid workspaceId, InviteMemberDto dto, CancellationToken ct = default);
    Task RemoveMemberAsync(string userId, Guid workspaceId, Guid memberId, CancellationToken ct = default);

    Task<SavedListingDto> SaveListingAsync(string userId, Guid workspaceId, Guid listingId, string? notes, CancellationToken ct = default);
    Task<List<SavedListingDto>> GetSavedListingsAsync(string userId, Guid workspaceId, CancellationToken ct = default);
    Task RemoveSavedListingAsync(string userId, Guid workspaceId, Guid savedListingId, CancellationToken ct = default);

    Task<CommentDto> AddCommentAsync(string userId, Guid workspaceId, Guid savedListingId, AddCommentDto dto, CancellationToken ct = default);
    Task<List<CommentDto>> GetCommentsAsync(string userId, Guid workspaceId, Guid savedListingId, CancellationToken ct = default);

    Task<List<ActivityLogDto>> GetActivityLogsAsync(string userId, Guid workspaceId, CancellationToken ct = default);
}
