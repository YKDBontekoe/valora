using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Valora.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .RequireAuthorization("Admin")
            .RequireRateLimiting("strict");

        group.MapGet("/users", async (
            IIdentityService identityService,
            ILoggerFactory loggerFactory,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null) =>
        {
            var logger = loggerFactory.CreateLogger("AdminEndpoints");
            logger.LogInformation("Admin user listing requested. Page: {Page}, PageSize: {PageSize}, Search: {Search}, Sort: {Sort}", page, pageSize, searchTerm, sortBy);

            var paginatedUsers = await identityService.GetUsersAsync(page, pageSize, searchTerm, sortBy, sortOrder);
            var rolesMap = await identityService.GetRolesForUsersAsync(paginatedUsers.Items);

            var userDtos = paginatedUsers.Items.Select(user => new AdminUserDto(
                user.Id,
                user.Email ?? "No Email",
                rolesMap.TryGetValue(user.Id, out var roles) ? roles : new List<string>()
            )).ToList();

            return Results.Ok(new {
                Items = userDtos,
                paginatedUsers.PageIndex,
                paginatedUsers.TotalPages,
                paginatedUsers.TotalCount,
                paginatedUsers.HasNextPage,
                paginatedUsers.HasPreviousPage
            });
        });

        group.MapDelete("/users/{id}", async (
            string id,
            ClaimsPrincipal user,
            IIdentityService identityService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AdminEndpoints");
            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId == id)
            {
                logger.LogWarning("Self-deletion attempted by user {UserId}", currentUserId);
                return Results.BadRequest(new { error = "You cannot delete your own account." });
            }

            logger.LogInformation("Admin user deletion requested for user {TargetUserId} by admin {AdminId}", id, currentUserId);

            var result = await identityService.DeleteUserAsync(id);
            if (result.Succeeded)
            {
                logger.LogInformation("Successfully deleted user {TargetUserId}", id);
                return Results.NoContent();
            }

            logger.LogError("Failed to delete user {TargetUserId}: {Errors}", id, string.Join(", ", result.Errors));
            return Results.BadRequest(new { error = "Operation failed. Please try again or contact support." });
        });

        group.MapGet("/stats", async (
            IIdentityService identityService,
            IListingRepository listingRepository,
            INotificationRepository notificationRepository,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AdminEndpoints");
            logger.LogInformation("Admin stats requested.");

            var usersCount = await identityService.CountAsync();
            var listingsCount = await listingRepository.CountAsync();
            var notificationsCount = await notificationRepository.CountAsync();

            return Results.Ok(new AdminStatsDto(
                usersCount,
                listingsCount,
                notificationsCount
            ));
        });
    }
}
