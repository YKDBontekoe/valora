using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IWorkspaceMemberRepository
{
    Task<List<WorkspaceMember>> GetMembersAsync(Guid workspaceId, CancellationToken ct = default);
    Task<WorkspaceMember?> GetMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<WorkspaceMember?> GetMemberByEmailAsync(Guid workspaceId, string email, CancellationToken ct = default);
    Task AddMemberAsync(WorkspaceMember member, CancellationToken ct = default);
    Task RemoveMemberAsync(WorkspaceMember member, CancellationToken ct = default);
    Task<bool> IsMemberAsync(Guid workspaceId, string userId, CancellationToken ct = default);
    Task<WorkspaceRole> GetUserRoleAsync(Guid workspaceId, string userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
