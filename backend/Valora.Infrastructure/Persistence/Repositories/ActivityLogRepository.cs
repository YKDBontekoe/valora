using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Persistence.Repositories;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly ValoraDbContext _context;

    public ActivityLogRepository(ValoraDbContext context)
    {
        _context = context;
    }

    public Task LogActivityAsync(ActivityLog log, CancellationToken ct = default)
    {
        _context.ActivityLogs.Add(log);
        return Task.CompletedTask;
    }

    public async Task<List<ActivityLog>> GetActivityLogsAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .ToListAsync(ct);
    }

    public async Task<List<ActivityLogDto>> GetActivityLogDtosAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .Select(a => new ActivityLogDto(
                a.Id,
                a.ActorId,
                a.Type.ToString(),
                a.Summary,
                a.CreatedAt,
                a.Metadata
            ))
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
