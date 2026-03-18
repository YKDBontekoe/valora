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

    // Persistence
    Task SaveChangesAsync(CancellationToken ct = default);
}
