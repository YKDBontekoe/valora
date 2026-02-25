using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IWorkspaceMemberService
{
    Task<List<WorkspaceMemberDto>> GetMembersAsync(string userId, Guid workspaceId, CancellationToken ct = default);
    Task AddMemberAsync(string userId, Guid workspaceId, InviteMemberDto dto, CancellationToken ct = default);
    Task RemoveMemberAsync(string userId, Guid workspaceId, Guid memberId, CancellationToken ct = default);
}
