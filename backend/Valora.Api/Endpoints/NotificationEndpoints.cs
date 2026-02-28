using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Services;
using Valora.Domain.Enums;

namespace Valora.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .RequireAuthorization();

        group.MapGet("/", async (
            INotificationService service,
            ClaimsPrincipal user,
            [FromQuery] bool unreadOnly = false,
            [FromQuery] int limit = 50,
            [FromQuery] int offset = 0) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            if (limit <= 0 || limit > 100) return Results.BadRequest("Limit must be between 1 and 100.");
            if (offset < 0) return Results.BadRequest("Offset must be non-negative.");

            var result = await service.GetUserNotificationsAsync(userId, unreadOnly, limit, offset);
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

        group.MapDelete("/{id}", async (
            Guid id,
            INotificationService service,
            ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var deleted = await service.DeleteNotificationAsync(id, userId);
            return deleted ? Results.NoContent() : Results.NotFound();
        });
    }
}
