using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(INotificationRepository repository, ILogger<NotificationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int limit = 50, int offset = 0)
    {
        var notifications = await _repository.GetByUserIdAsync(userId, unreadOnly, limit, offset);
        return notifications.Select(NotificationDto.FromEntity).ToList();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _repository.GetUnreadCountAsync(userId);
    }

    public async Task MarkAsReadAsync(Guid notificationId, string userId)
    {
        var notification = await _repository.GetByIdAsync(notificationId, userId);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            await _repository.UpdateAsync(notification);
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var unreadNotifications = await _repository.GetUnreadByUserIdAsync(userId);
        if (unreadNotifications.Any())
        {
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                await _repository.UpdateAsync(notification);
            }
        }
    }

    public async Task<bool> DeleteNotificationAsync(Guid notificationId, string userId)
    {
        var notification = await _repository.GetByIdAsync(notificationId, userId);
        if (notification == null) return false;

        _logger.LogInformation("[AUDIT] User {UserId} deleting notification {NotificationId}", userId, notificationId);

        await _repository.DeleteAsync(notification);
        return true;
    }

    public async Task CreateNotificationAsync(string userId, string title, string body, NotificationType type, string? actionUrl = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(notification);
    }
}
