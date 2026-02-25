using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IWorkspaceRepository _repository;

    public ActivityLogService(IWorkspaceRepository repository)
    {
        _repository = repository;
    }

    public async Task LogActivityAsync(Guid? workspaceId, string actorId, ActivityLogType type, string summary, CancellationToken ct = default)
    {
        var log = new ActivityLog
        {
            WorkspaceId = workspaceId,
            ActorId = actorId,
            Type = type,
            Summary = summary,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.LogActivityAsync(log, ct);
    }

    public async Task LogActivityAsync(Workspace workspace, string actorId, ActivityLogType type, string summary, CancellationToken ct = default)
    {
        var log = new ActivityLog
        {
            Workspace = workspace,
            ActorId = actorId,
            Type = type,
            Summary = summary,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.LogActivityAsync(log, ct);
    }

    public async Task<List<ActivityLogDto>> GetActivityLogsAsync(string userId, Guid workspaceId, CancellationToken ct = default)
    {
        var isMember = await _repository.IsMemberAsync(workspaceId, userId, ct);
        if (!isMember) throw new ForbiddenAccessException();

        var logs = await _repository.GetActivityLogsAsync(workspaceId, ct);

        return logs.Select(a => new ActivityLogDto(
            a.Id,
            a.ActorId,
            a.Type.ToString(),
            a.Summary,
            a.CreatedAt,
            a.Metadata
        )).ToList();
    }
}
