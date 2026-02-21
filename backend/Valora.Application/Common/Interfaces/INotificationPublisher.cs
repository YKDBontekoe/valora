using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface INotificationPublisher
{
    Task PublishNotificationCreatedAsync(string userId, NotificationDto notification);
    Task PublishNotificationReadAsync(string userId, Guid notificationId);
    Task PublishNotificationDeletedAsync(string userId, Guid notificationId);
}
