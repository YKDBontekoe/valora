using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IWorkspaceService
{
    Task<WorkspaceDto> CreateWorkspaceAsync(string userId, CreateWorkspaceDto dto, CancellationToken ct = default);
    Task<List<WorkspaceDto>> GetUserWorkspacesAsync(string userId, CancellationToken ct = default);
    Task<WorkspaceDto> GetWorkspaceAsync(string userId, Guid workspaceId, CancellationToken ct = default);
    Task DeleteWorkspaceAsync(string userId, Guid workspaceId, CancellationToken ct = default);
}
