using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

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
            ClaimsPrincipal user,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? q = null,
            [FromQuery] string? sort = null) =>
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return Results.BadRequest(new { error = "Invalid pagination parameters." });
            }

            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var paginatedUsers = await adminService.GetUsersAsync(page, pageSize, q, sort, currentUserId);

            return Results.Ok(new {
                paginatedUsers.Items,
                paginatedUsers.PageIndex,
                paginatedUsers.TotalPages,
                paginatedUsers.TotalCount,
                paginatedUsers.HasNextPage,
                paginatedUsers.HasPreviousPage
            });
        });

        group.MapPost("/users", async (
            AdminCreateUserDto request,
            IAdminService adminService,
            ClaimsPrincipal user) =>
        {
            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Results.Unauthorized();
            }

            var result = await adminService.CreateUserAsync(request, currentUserId);
            if (result.Succeeded)
            {
                return Results.Created($"/api/admin/users", new { message = "User created successfully." });
            }

            if (result.ErrorCode == "Conflict")
            {
                return Results.Conflict(new { error = result.Errors.FirstOrDefault() ?? "User already exists." });
            }

            return Results.BadRequest(new { error = result.Errors.FirstOrDefault() ?? "Operation failed." });
        })
        .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<AdminCreateUserDto>>();

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

        group.MapPost("/jobs", async (
            BatchJobRequest request,
            IBatchJobService jobService,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<BatchJobType>(request.Type, out var jobType))
            {
                return Results.BadRequest(new { error = "Invalid job type." });
            }

            var job = await jobService.EnqueueJobAsync(jobType, request.Target, ct);
            return Results.Accepted($"/api/admin/jobs/{job.Id}", job);
        })
        .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<BatchJobRequest>>();

        group.MapGet("/jobs", async (
            IBatchJobService jobService,
            CancellationToken ct,
            [FromQuery] int limit = 10) =>
        {
            if (limit < 1 || limit > 100)
            {
                return Results.BadRequest(new { error = "Limit must be between 1 and 100." });
            }

            var jobs = await jobService.GetRecentJobsAsync(limit, ct);
            return Results.Ok(jobs);
        });
    }
}
