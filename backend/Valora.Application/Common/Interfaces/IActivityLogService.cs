using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IActivityLogService
{
    Task LogActivityAsync(Guid? workspaceId, string actorId, ActivityLogType type, string summary, CancellationToken ct = default);
    Task LogActivityAsync(Workspace workspace, string actorId, ActivityLogType type, string summary, CancellationToken ct = default);
    Task<List<ActivityLogDto>> GetActivityLogsAsync(string userId, Guid workspaceId, CancellationToken ct = default);
}
