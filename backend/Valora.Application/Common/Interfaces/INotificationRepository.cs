using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface INotificationRepository
{
    Task<List<Notification>> GetByUserIdAsync(string userId, bool unreadOnly, int limit, int offset, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<List<Notification>> GetUnreadByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
    Task DeleteAsync(Notification notification, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
