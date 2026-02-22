using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Filters;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.DTOs.Shared;
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
            [AsParameters] PaginationRequest pagination,
            [FromQuery] string? q = null,
            [FromQuery] string? sort = null) =>
        {
            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var paginatedUsers = await adminService.GetUsersAsync(pagination.Page, pagination.PageSize, q, sort, currentUserId);

            return Results.Ok(new {
                paginatedUsers.Items,
                paginatedUsers.PageIndex,
                paginatedUsers.TotalPages,
                paginatedUsers.TotalCount,
                paginatedUsers.HasNextPage,
                paginatedUsers.HasPreviousPage
            });
        })
        .AddEndpointFilter<ValidationFilter<PaginationRequest>>();

        group.MapDelete("/users/{id}", async (
            string id,
            ClaimsPrincipal user,
            IAdminService adminService) =>
        {
            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                 return Results.Problem(detail: "Unauthorized.", statusCode: 401);
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
                "NotFound" => Results.Problem(detail: result.Errors.FirstOrDefault() ?? "Resource not found.", statusCode: 404),
                _ => Results.Problem(detail: result.Errors.FirstOrDefault() ?? "Operation failed.", statusCode: 400)
            };
        });

        group.MapGet("/stats", async (
            IAdminService adminService) =>
        {
            var stats = await adminService.GetSystemStatsAsync();
            return Results.Ok(stats);
        });

        group.MapGet("/dataset/status", async (
            IAdminService adminService,
            CancellationToken ct) =>
        {
            var status = await adminService.GetDatasetStatusAsync(ct);
            return Results.Ok(status);
        });

        group.MapPost("/jobs", async (
            BatchJobRequest request,
            IBatchJobService jobService,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<BatchJobType>(request.Type, out var jobType))
            {
                return Results.Problem(detail: "Invalid job type.", statusCode: 400);
            }

            var job = await jobService.EnqueueJobAsync(jobType, request.Target, ct);
            return Results.Accepted($"/api/admin/jobs/{job.Id}", job);
        })
        .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<BatchJobRequest>>();

        group.MapGet("/jobs", async (
            IBatchJobService jobService,
            CancellationToken ct,
            [AsParameters] PaginationRequest pagination,
            [FromQuery] string? status = null,
            [FromQuery] string? type = null) =>
        {
            var jobs = await jobService.GetJobsAsync(pagination.Page, pagination.PageSize, status, type, ct);
            return Results.Ok(new {
                jobs.Items,
                jobs.PageIndex,
                jobs.TotalPages,
                jobs.TotalCount,
                jobs.HasNextPage,
                jobs.HasPreviousPage
            });
        })
        .AddEndpointFilter<ValidationFilter<PaginationRequest>>();

        group.MapGet("/jobs/{id}", async (
            Guid id,
            IBatchJobService jobService,
            CancellationToken ct) =>
        {
            try
            {
                var job = await jobService.GetJobDetailsAsync(id, ct);
                return Results.Ok(job);
            }
            catch (KeyNotFoundException)
            {
                return Results.Problem(detail: "Job not found.", statusCode: 404);
            }
        });

        group.MapPost("/jobs/{id}/retry", async (
            Guid id,
            IBatchJobService jobService,
            CancellationToken ct) =>
        {
            try
            {
                var job = await jobService.RetryJobAsync(id, ct);
                return Results.Ok(job);
            }
            catch (KeyNotFoundException)
            {
                return Results.Problem(detail: "Job not found.", statusCode: 404);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 400);
            }
        });

        group.MapPost("/jobs/{id}/cancel", async (
            Guid id,
            IBatchJobService jobService,
            CancellationToken ct) =>
        {
            try
            {
                var job = await jobService.CancelJobAsync(id, ct);
                return Results.Ok(job);
            }
            catch (KeyNotFoundException)
            {
                return Results.Problem(detail: "Job not found.", statusCode: 404);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 400);
            }

        });
    }
}
