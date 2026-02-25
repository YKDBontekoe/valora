using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class WorkspaceMemberService : IWorkspaceMemberService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IIdentityService _identityService;
    private readonly IActivityLogService _activityLogService;

    public WorkspaceMemberService(
        IWorkspaceRepository repository,
        IIdentityService identityService,
        IActivityLogService activityLogService)
    {
        _repository = repository;
        _identityService = identityService;
        _activityLogService = activityLogService;
    }

    public async Task<List<WorkspaceMemberDto>> GetMembersAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        var isMember = await _repository.IsMemberAsync(workspaceId, userId, ct);
        if (!isMember) throw new ForbiddenAccessException();

        var members = await _repository.GetMembersAsync(workspaceId, ct);

        return members.Select(m => new WorkspaceMemberDto(
            m.Id,
            m.UserId,
            m.User?.Email ?? m.InvitedEmail,
            m.Role,
            m.IsPending,
            m.JoinedAt
        )).ToList();
    }

    public async Task AddMemberAsync(string userId, Guid workspaceId, InviteMemberDto dto, CancellationToken ct = default)
    {
        var role = await _repository.GetUserRoleAsync(workspaceId, userId, ct);
        if (role != WorkspaceRole.Owner) throw new ForbiddenAccessException();

        var existingMember = await _repository.GetMemberByEmailAsync(workspaceId, dto.Email, ct);
        if (existingMember != null) return;

        var invitedUser = await _identityService.GetUserByEmailAsync(dto.Email);

        var member = new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            Role = dto.Role,
            InvitedEmail = dto.Email,
            UserId = invitedUser?.Id,
            JoinedAt = invitedUser != null ? DateTime.UtcNow : (DateTime?)null
        };

        await _repository.AddMemberAsync(member, ct);
        await _activityLogService.LogActivityAsync(workspaceId, userId, ActivityLogType.MemberInvited, $"Invited {dto.Email} as {dto.Role}", ct);
        await _repository.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(string userId, Guid workspaceId, Guid memberId, CancellationToken ct = default)
    {
        var currentUserRole = await _repository.GetUserRoleAsync(workspaceId, userId, ct);
        if (currentUserRole != WorkspaceRole.Owner) throw new ForbiddenAccessException();

        var member = await _repository.GetMemberAsync(memberId, ct);
        if (member == null || member.WorkspaceId != workspaceId) throw new NotFoundException(nameof(WorkspaceMember), memberId);

        if (member.UserId == userId) throw new InvalidOperationException("Cannot remove yourself.");

        await _repository.RemoveMemberAsync(member, ct);
        await _activityLogService.LogActivityAsync(workspaceId, userId, ActivityLogType.MemberRemoved, $"Removed member {(member.InvitedEmail ?? member.UserId)}", ct);
        await _repository.SaveChangesAsync(ct);
    }
}
