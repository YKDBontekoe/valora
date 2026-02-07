using System;
using Valora.Domain.Entities;

namespace Valora.Application.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ActionUrl { get; set; }
    public NotificationType Type { get; set; }

    public static NotificationDto FromEntity(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Body = notification.Body,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ActionUrl = notification.ActionUrl,
            Type = notification.Type
        };
    }
}
