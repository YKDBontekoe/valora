using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<NotificationService> _logger;
    private readonly TimeProvider _timeProvider;

    public NotificationService(
        INotificationRepository repository,
        ILogger<NotificationService> logger,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int limit = 50, int offset = 0)
    {
        limit = limit <= 0 ? 50 : limit;
        offset = offset < 0 ? 0 : offset;

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
            _logger.LogInformation("[AUDIT] User {UserId} marked notification {NotificationId} as read", userId, notificationId);
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        await _repository.MarkAllAsReadAsync(userId);
        _logger.LogInformation("[AUDIT] User {UserId} marked all notifications as read", userId);
    }

    public async Task<bool> DeleteNotificationAsync(Guid notificationId, string userId)
    {
        var notification = await _repository.GetByIdAsync(notificationId, userId);
        if (notification == null) return false;

        await _repository.DeleteAsync(notification);
        _logger.LogInformation("[AUDIT] User {UserId} deleted notification {NotificationId}", userId, notificationId);

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
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        await _repository.AddAsync(notification);
        _logger.LogInformation("[AUDIT] Created notification {NotificationId} for user {UserId} (Type: {Type})", notification.Id, userId, type);
    }
}
