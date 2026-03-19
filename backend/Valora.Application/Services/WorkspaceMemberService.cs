using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class WorkspaceMemberService : IWorkspaceMemberService
{
    private readonly IWorkspaceManagementRepository _managementRepository;
    private readonly IWorkspaceMemberRepository _memberRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IIdentityService _identityService;
    private readonly IEventDispatcher _eventDispatcher;

    public WorkspaceMemberService(
        IWorkspaceManagementRepository managementRepository,
        IWorkspaceMemberRepository memberRepository,
        IActivityLogRepository activityLogRepository,
        IIdentityService identityService,
        IEventDispatcher eventDispatcher)
    {
        _managementRepository = managementRepository;
        _memberRepository = memberRepository;
        _activityLogRepository = activityLogRepository;
        _identityService = identityService;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<List<WorkspaceMemberDto>> GetMembersAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        var members = await _memberRepository.GetMembersAsync(workspaceId, ct);

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
        var role = await GetUserRole(userId, workspaceId, ct);
        if (role != WorkspaceRole.Owner) throw new ForbiddenAccessException();

        var existingMember = await _memberRepository.GetMemberByEmailAsync(workspaceId, dto.Email, ct);
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

        await _memberRepository.AddMemberAsync(member, ct);
        await LogActivityAsync(workspaceId, userId, ActivityLogType.MemberInvited, $"Invited a new member as {dto.Role}", ct);
        await _memberRepository.SaveChangesAsync(ct);

        var workspace = await _managementRepository.GetByIdAsync(workspaceId, ct);
        if (workspace != null)
        {
            await _eventDispatcher.DispatchAsync(new Valora.Application.Common.Events.WorkspaceInviteAcceptedEvent(workspaceId, workspace.Name, userId, member.UserId ?? "", dto.Email), ct);
        }
    }

    public async Task RemoveMemberAsync(string userId, Guid workspaceId, Guid memberId, CancellationToken ct = default)
    {
        var currentUserRole = await GetUserRole(userId, workspaceId, ct);
        if (currentUserRole != WorkspaceRole.Owner) throw new ForbiddenAccessException();

        var member = await _memberRepository.GetMemberAsync(memberId, ct);
        if (member == null || member.WorkspaceId != workspaceId) throw new NotFoundException(nameof(WorkspaceMember), memberId);

        if (member.UserId == userId) throw new InvalidOperationException("Cannot remove yourself.");

        await _memberRepository.RemoveMemberAsync(member, ct);
        await LogActivityAsync(workspaceId, userId, ActivityLogType.MemberRemoved, "Removed a member", ct);
        await _memberRepository.SaveChangesAsync(ct);
    }

    // Helpers
    private async Task ValidateMemberAccess(string userId, Guid workspaceId, CancellationToken ct)
    {
        var isMember = await _memberRepository.IsMemberAsync(workspaceId, userId, ct);
        if (!isMember) throw new ForbiddenAccessException();
    }

    private async Task<WorkspaceRole> GetUserRole(string userId, Guid workspaceId, CancellationToken ct)
    {
        return await _memberRepository.GetUserRoleAsync(workspaceId, userId, ct);
    }

    private async Task LogActivityAsync(Guid? workspaceId, string actorId, ActivityLogType type, string summary, CancellationToken ct)
    {
        var log = new ActivityLog
        {
            WorkspaceId = workspaceId,
            ActorId = actorId,
            Type = type,
            Summary = summary,
            CreatedAt = DateTime.UtcNow
        };
        await _activityLogRepository.LogActivityAsync(log, ct);
    }
}
