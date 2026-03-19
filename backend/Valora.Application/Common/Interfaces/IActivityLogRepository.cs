using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IActivityLogRepository
{
    Task LogActivityAsync(ActivityLog log, CancellationToken ct = default);
    Task<List<ActivityLog>> GetActivityLogsAsync(Guid workspaceId, CancellationToken ct = default);
    Task<List<ActivityLogDto>> GetActivityLogDtosAsync(Guid workspaceId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
