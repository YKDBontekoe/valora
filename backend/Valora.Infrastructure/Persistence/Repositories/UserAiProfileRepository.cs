using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Repositories;

public class UserAiProfileRepository : IUserAiProfileRepository
{
    private readonly ValoraDbContext _context;

    public UserAiProfileRepository(ValoraDbContext context)
    {
        _context = context;
    }

    public async Task<UserAiProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await _context.UserAiProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(UserAiProfile profile, CancellationToken cancellationToken)
    {
        _context.UserAiProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserAiProfile profile, CancellationToken cancellationToken)
    {
        _context.UserAiProfiles.Update(profile);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserAiProfile profile, CancellationToken cancellationToken)
    {
        _context.UserAiProfiles.Remove(profile);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
