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

            return Results.BadRequest(new { error = result.Errors.FirstOrDefault() ?? "Operation failed." });
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
        });

        group.MapGet("/jobs", async (
            IBatchJobService jobService,
            CancellationToken ct,
            [FromQuery] int limit = 10) =>
        {
            var jobs = await jobService.GetRecentJobsAsync(limit, ct);
            return Results.Ok(jobs);
        });
    }
}
