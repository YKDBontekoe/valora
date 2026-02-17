using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;

namespace Valora.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .RequireAuthorization("Admin")
            .RequireRateLimiting("strict");

        group.MapGet("/users", async (
            IAdminService adminService,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10) =>
        {
            var paginatedUsers = await adminService.GetUsersAsync(page, pageSize);

            return Results.Ok(new {
                paginatedUsers.Items,
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
            IAdminService adminService) =>
        {
            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                 return Results.Unauthorized();
            }

            var result = await adminService.DeleteUserAsync(id, currentUserId);
            if (result.Succeeded)
            {
                return Results.NoContent();
            }

            // Map ErrorCode to HTTP Status
            return result.ErrorCode switch
            {
                "Forbidden" => Results.Forbid(),
                "NotFound" => Results.NotFound(new { error = result.Errors.FirstOrDefault() ?? "Resource not found." }),
                _ => Results.BadRequest(new { error = result.Errors.FirstOrDefault() ?? "Operation failed." })
            };
        });

        group.MapGet("/stats", async (
            IAdminService adminService) =>
        {
            var stats = await adminService.GetSystemStatsAsync();
            return Results.Ok(stats);
        });
    }
}
