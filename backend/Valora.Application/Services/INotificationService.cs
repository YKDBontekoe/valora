using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int limit = 50, int offset = 0);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(Guid notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
    Task<bool> DeleteNotificationAsync(Guid notificationId, string userId);
    Task CreateNotificationAsync(string userId, string title, string body, NotificationType type, string? actionUrl = null);
}
