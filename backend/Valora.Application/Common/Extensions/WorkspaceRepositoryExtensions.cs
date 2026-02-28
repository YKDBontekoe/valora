using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Valora.Application.Common.Extensions;

public static class WorkspaceRepositoryExtensions
{
    public static async Task LogActivityEventAsync(
        this IWorkspaceRepository repository,
        Guid? workspaceId,
        string actorId,
        ActivityLogType type,
        string summary,
        CancellationToken ct)
    {
        var log = new ActivityLog
        {
            WorkspaceId = workspaceId,
            ActorId = actorId,
            Type = type,
            Summary = summary,
            CreatedAt = DateTime.UtcNow
        };
        await repository.LogActivityAsync(log, ct);
    }

    public static async Task LogActivityEventAsync(
        this IWorkspaceRepository repository,
        Workspace workspace,
        string actorId,
        ActivityLogType type,
        string summary,
        CancellationToken ct)
    {
        var log = new ActivityLog
        {
            Workspace = workspace,
            ActorId = actorId,
            Type = type,
            Summary = summary,
            CreatedAt = DateTime.UtcNow
        };
        await repository.LogActivityAsync(log, ct);
    }
}
