using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Services;
using Valora.Domain.Entities;

namespace Valora.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .RequireAuthorization();

        group.MapGet("/", async (
            [FromQuery] bool unreadOnly,
            [FromQuery] int limit,
            INotificationService service,
            ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var result = await service.GetUserNotificationsAsync(userId, unreadOnly, limit == 0 ? 50 : limit);
            return Results.Ok(result);
        });

        group.MapGet("/unread-count", async (
            INotificationService service,
            ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var count = await service.GetUnreadCountAsync(userId);
            return Results.Ok(new { count });
        });

        group.MapPost("/{id}/read", async (
            Guid id,
            INotificationService service,
            ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            await service.MarkAsReadAsync(id, userId);
            return Results.Ok();
        });

        group.MapPost("/read-all", async (
            INotificationService service,
            ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            await service.MarkAllAsReadAsync(userId);
            return Results.Ok();
        });
    }
}
