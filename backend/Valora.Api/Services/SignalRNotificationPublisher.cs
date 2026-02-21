using Microsoft.AspNetCore.SignalR;
using Valora.Api.Hubs;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Services;

public class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationPublisher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PublishNotificationCreatedAsync(string userId, NotificationDto notification)
    {
        await _hubContext.Clients.Group($"User-{userId}").SendAsync("NotificationCreated", notification);
    }

    public async Task PublishNotificationReadAsync(string userId, Guid notificationId)
    {
        await _hubContext.Clients.Group($"User-{userId}").SendAsync("NotificationRead", notificationId);
    }

    public async Task PublishNotificationDeletedAsync(string userId, Guid notificationId)
    {
        await _hubContext.Clients.Group($"User-{userId}").SendAsync("NotificationDeleted", notificationId);
    }
}
