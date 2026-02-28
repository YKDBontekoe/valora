using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Common;
using Valora.Domain.Entities;
using Valora.Domain.Enums;

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
        var safeUserId = userId.Truncate(ValidationConstants.Notification.UserIdMaxLength, out var userIdTruncated)
                         ?? throw new Valora.Application.Common.Exceptions.ValidationException(new Dictionary<string, string[]> { { "UserId", new[] { "UserId cannot be null." } } });

        var safeTitle = title.Truncate(ValidationConstants.Notification.TitleMaxLength, out var titleTruncated)
                        ?? "Notification";

        var safeBody = body.Truncate(ValidationConstants.Notification.BodyMaxLength, out var bodyTruncated)
                       ?? string.Empty;

        var safeActionUrl = actionUrl.Truncate(ValidationConstants.Notification.ActionUrlMaxLength, out var urlTruncated);

        if (userIdTruncated || titleTruncated || bodyTruncated || urlTruncated)
        {
            _logger.LogWarning("Notification data truncated. UserId: {UserIdTruncated}, Title: {TitleTruncated}, Body: {BodyTruncated}, Url: {UrlTruncated}",
                userIdTruncated, titleTruncated, bodyTruncated, urlTruncated);
        }

        var notification = new Notification
        {
            UserId = safeUserId,
            Title = safeTitle,
            Body = safeBody,
            Type = type,
            ActionUrl = safeActionUrl,
            IsRead = false,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        await _repository.AddAsync(notification);
        _logger.LogInformation("[AUDIT] Created notification {NotificationId} for user {UserId} (Type: {Type})", notification.Id, safeUserId, type);
    }
}
