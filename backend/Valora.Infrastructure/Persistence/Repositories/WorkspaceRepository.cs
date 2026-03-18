using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Persistence.Repositories;

public class WorkspaceRepository : IWorkspaceRepository
{
    private readonly ValoraDbContext _context;

    public WorkspaceRepository(ValoraDbContext context)
    {
        _context = context;
    }

    public Task<Workspace> AddAsync(Workspace workspace, CancellationToken ct = default)
    {
        _context.Workspaces.Add(workspace);
        return Task.FromResult(workspace);
    }

    public async Task<List<Workspace>> GetUserWorkspacesAsync(string userId, CancellationToken ct = default)
    {
        return await _context.Workspaces
            .AsNoTracking()
            .Include(w => w.Members)
            .Include(w => w.SavedProperties)
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<WorkspaceDto>> GetUserWorkspaceDtosAsync(string userId, CancellationToken ct = default)
    {
        return await _context.Workspaces
            .AsNoTracking()
            .Where(w => w.Members.Any(m => m.UserId == userId))
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WorkspaceDto(
                w.Id,
                w.Name,
                w.Description,
                w.OwnerId,
                w.CreatedAt,
                w.Members.Count,
                w.SavedProperties.Count
            ))
            .ToListAsync(ct);
    }

    public async Task<int> GetUserOwnedWorkspacesCountAsync(string userId, CancellationToken ct = default)
    {
        return await _context.Workspaces
            .AsNoTracking()
            .Where(w => w.Members.Any(m => m.UserId == userId && m.Role == WorkspaceRole.Owner))
            .CountAsync(ct);
    }

    public async Task<(WorkspaceDto? Dto, bool IsMember)> GetWorkspaceDtoAndMemberStatusAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var result = await _context.Workspaces
            .AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new ValueTuple<WorkspaceDto, bool>(
                new WorkspaceDto(
                    w.Id,
                    w.Name,
                    w.Description,
                    w.OwnerId,
                    w.CreatedAt,
                    w.Members.Count,
                    w.SavedProperties.Count
                ),
                w.Members.Any(m => m.UserId == userId)
            ))
            .FirstOrDefaultAsync(ct);

        return result.Item1 == null ? (null, false) : (result.Item1, result.Item2);
    }

    public async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Workspaces
            .Include(w => w.Members)
            .Include(w => w.SavedProperties)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task<Workspace?> GetByIdWithMembersAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public Task UpdateAsync(Workspace workspace, CancellationToken ct = default)
    {
        _context.Entry(workspace).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Workspace workspace, CancellationToken ct = default)
    {
        _context.Workspaces.Remove(workspace);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
