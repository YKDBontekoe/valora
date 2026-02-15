using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .RequireAuthorization("Admin")
            .RequireRateLimiting("strict");

        group.MapGet("/users", async (IIdentityService identityService) =>
        {
            var users = await identityService.GetUsersAsync();
            var userDtos = new List<AdminUserDto>();

            foreach (var user in users)
            {
                var roles = await identityService.GetUserRolesAsync(user);
                userDtos.Add(new AdminUserDto(user.Id, user.Email!, roles));
            }

            return Results.Ok(userDtos);
        });

        group.MapDelete("/users/{id}", async (string id, IIdentityService identityService) =>
        {
            var result = await identityService.DeleteUserAsync(id);
            if (result.Succeeded)
            {
                return Results.NoContent();
            }

            return Results.BadRequest(new { error = string.Join(", ", result.Errors) });
        });

        group.MapGet("/stats", async (
            IIdentityService identityService,
            IListingRepository listingRepository,
            INotificationRepository notificationRepository) =>
        {
            var users = await identityService.GetUsersAsync();
            var listingsCount = await listingRepository.CountAsync();
            var notificationsCount = await notificationRepository.CountAsync();

            return Results.Ok(new AdminStatsDto(
                users.Count,
                listingsCount,
                notificationsCount
            ));
        });
    }
}
