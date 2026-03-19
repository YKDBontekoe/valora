using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Persistence.Repositories;

public class WorkspaceMemberRepository : IWorkspaceMemberRepository
{
    private readonly ValoraDbContext _context;

    public WorkspaceMemberRepository(ValoraDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkspaceMember>> GetMembersAsync(Guid workspaceId, CancellationToken ct = default)
    {
        return await _context.WorkspaceMembers
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.WorkspaceId == workspaceId)
            .ToListAsync(ct);
    }

    public async Task<WorkspaceMember?> GetMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _context.WorkspaceMembers.FindAsync(new object[] { memberId }, ct);
    }

    public async Task<WorkspaceMember?> GetMemberByEmailAsync(Guid workspaceId, string email, CancellationToken ct = default)
    {
        return await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId &&
                ((m.UserId != null && m.User!.Email == email) || m.InvitedEmail == email), ct);
    }

    public Task AddMemberAsync(WorkspaceMember member, CancellationToken ct = default)
    {
        _context.WorkspaceMembers.Add(member);
        return Task.CompletedTask;
    }

    public Task RemoveMemberAsync(WorkspaceMember member, CancellationToken ct = default)
    {
        _context.WorkspaceMembers.Remove(member);
        return Task.CompletedTask;
    }

    public async Task<bool> IsMemberAsync(Guid workspaceId, string userId, CancellationToken ct = default)
    {
        return await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);
    }

    public async Task<WorkspaceRole> GetUserRoleAsync(Guid workspaceId, string userId, CancellationToken ct = default)
    {
        var member = await _context.WorkspaceMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

        if (member == null) throw new ForbiddenAccessException();
        return member.Role;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
