using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IActivityLogService _activityLogService;

    public WorkspaceService(IWorkspaceRepository repository, IActivityLogService activityLogService)
    {
        _repository = repository;
        _activityLogService = activityLogService;
    }

    public async Task<WorkspaceDto> CreateWorkspaceAsync(string userId, CreateWorkspaceDto dto, CancellationToken ct = default)
    {
        var existingCount = await _repository.GetUserOwnedWorkspacesCountAsync(userId, ct);
        if (existingCount >= 10)
        {
            await _activityLogService.LogActivityAsync((Guid?)null, userId, ActivityLogType.WorkspaceCreated, "Workspace creation failed: limit reached", ct);
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

        await _activityLogService.LogActivityAsync(workspace, userId, ActivityLogType.WorkspaceCreated, $"Workspace '{dto.Name}' created", ct);

        await _repository.SaveChangesAsync(ct);

        return MapToDto(workspace);
    }

    public async Task<List<WorkspaceDto>> GetUserWorkspacesAsync(string userId, CancellationToken ct = default)
    {
        var workspaces = await _repository.GetUserWorkspacesAsync(userId, ct);
        return workspaces.Select(MapToDto).ToList();
    }

    public async Task<WorkspaceDto> GetWorkspaceAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        var workspace = await _repository.GetByIdAsync(workspaceId, ct);

        if (workspace == null) throw new NotFoundException(nameof(Workspace), workspaceId);

        if (!workspace.Members.Any(m => m.UserId == userId))
            throw new ForbiddenAccessException();

        return MapToDto(workspace);
    }

    public async Task DeleteWorkspaceAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        var workspace = await _repository.GetByIdAsync(workspaceId, ct);
        if (workspace == null) throw new NotFoundException(nameof(Workspace), workspaceId);

        if (workspace.OwnerId != userId)
            throw new ForbiddenAccessException();

        await _activityLogService.LogActivityAsync(workspace, userId, ActivityLogType.WorkspaceDeleted, $"Workspace '{workspace.Name}' deleted", ct);

        await _repository.DeleteAsync(workspace, ct);
        await _repository.SaveChangesAsync(ct);
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
            w.SavedListings.Count
        );
    }
}
