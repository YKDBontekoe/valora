using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ValoraDbContext _context;
    private readonly TimeProvider _timeProvider;

    public NotificationRepository(ValoraDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<List<Notification>> GetByUserIdAsync(string userId, bool unreadOnly, int limit, int offset, CancellationToken cancellationToken = default)
    {
        // AsNoTracking() is used here because this is a read-only query for the UI.
        // It avoids the overhead of change tracking in EF Core, resulting in faster performance
        // and lower memory usage, especially when pagination is involved.
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);
    }

    public async Task<List<Notification>> GetUnreadByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        notification.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Notifications.CountAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (_context.Database.IsRelational())
        {
            // ExecuteUpdateAsync performs a bulk update directly in the database (SQL UPDATE).
            // This is significantly more efficient than fetching all entities into memory,
            // modifying them, and calling SaveChanges(), especially for users with many notifications.
            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.UpdatedAt, now),
                    cancellationToken);
        }
        else
        {
            // Fallback for InMemory provider (for tests)
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(cancellationToken);

            foreach (var n in notifications)
            {
                n.IsRead = true;
                n.UpdatedAt = now;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
