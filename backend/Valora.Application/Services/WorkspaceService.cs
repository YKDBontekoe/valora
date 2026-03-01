using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Application.Common.Extensions;

namespace Valora.Application.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _repository;

    public WorkspaceService(IWorkspaceRepository repository)
    {
        _repository = repository;
    }

    public async Task<WorkspaceDto> CreateWorkspaceAsync(string userId, CreateWorkspaceDto dto, CancellationToken ct = default)
    {
        var existingCount = await _repository.GetUserOwnedWorkspacesCountAsync(userId, ct);
        if (existingCount >= 10)
        {
            await _repository.LogActivityEventAsync((Guid?)null, userId, ActivityLogType.WorkspaceCreated, "Workspace creation failed: limit reached", ct);
            await _repository.SaveChangesAsync(ct);
            throw new InvalidOperationException("You have reached the maximum number of workspaces (10).");
        }

        var workspace = new Workspace
        {
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = userId,
            Members = new List<WorkspaceMember>
            {
                new WorkspaceMember
                {
                    UserId = userId,
                    Role = WorkspaceRole.Owner,
                    JoinedAt = DateTime.UtcNow
                }
            }
        };

        await _repository.AddAsync(workspace, ct);

        await _repository.LogActivityEventAsync(workspace, userId, ActivityLogType.WorkspaceCreated, $"Workspace '{dto.Name}' created", ct);

        await _repository.SaveChangesAsync(ct);

        return MapToDto(workspace);
    }

    public async Task<List<WorkspaceDto>> GetUserWorkspacesAsync(string userId, CancellationToken ct = default)
    {
        return await _repository.GetUserWorkspaceDtosAsync(userId, ct);
    }

    public async Task<WorkspaceDto> GetWorkspaceAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        var result = await _repository.GetWorkspaceDtoAndMemberStatusAsync(workspaceId, userId, ct);

        if (result.Dto == null) throw new NotFoundException(nameof(Workspace), workspaceId);
        if (!result.IsMember) throw new ForbiddenAccessException();

        return result.Dto;
    }

    public async Task DeleteWorkspaceAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        var workspace = await _repository.GetByIdAsync(workspaceId, ct);
        if (workspace == null) throw new NotFoundException(nameof(Workspace), workspaceId);

        if (workspace.OwnerId != userId)
            throw new ForbiddenAccessException();

        // Log audit trail for deletion - even if it cascades, having it in the log stream before commit is better than nothing.
        // In a real system we'd log this to a non-cascade table or external audit service.
        await _repository.LogActivityEventAsync(workspace, userId, ActivityLogType.WorkspaceDeleted, $"Workspace '{workspace.Name}' deleted", ct);

        await _repository.DeleteAsync(workspace, ct);
        await _repository.SaveChangesAsync(ct);
    }

    public async Task<List<ActivityLogDto>> GetActivityLogsAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        await ValidateMemberAccess(userId, workspaceId, ct);

        return await _repository.GetActivityLogDtosAsync(workspaceId, ct);
    }

    // Helpers
    private async Task ValidateMemberAccess(string userId, Guid workspaceId, CancellationToken ct)
    {
        var isMember = await _repository.IsMemberAsync(workspaceId, userId, ct);
        if (!isMember) throw new ForbiddenAccessException();
    }

    private static WorkspaceDto MapToDto(Workspace w)
    {
        return new WorkspaceDto(
            w.Id,
            w.Name,
            w.Description,
            w.OwnerId,
            w.CreatedAt,
            w.Members.Count,
            w.SavedProperties.Count
        );
    }
}
