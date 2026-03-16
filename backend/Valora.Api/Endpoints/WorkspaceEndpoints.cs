using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class WorkspaceEndpoints
{
    public static RouteGroupBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/workspaces")
            .RequireAuthorization()
            .RequireRateLimiting(Valora.Api.Constants.RateLimitPolicies.Fixed)
            .WithTags("Workspaces");

        group.MapPost("/", CreateWorkspace)
            .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<CreateWorkspaceDto>>();

        group.MapGet("/", GetUserWorkspaces);
        group.MapGet("/{id}", GetWorkspace);
        group.MapDelete("/{id}", DeleteWorkspace);

        group.MapWorkspaceMemberEndpoints();
        group.MapWorkspacePropertyEndpoints();

        group.MapGet("/{id}/activity", GetActivityLogs);

        return group;
    }

    /// <summary>
    /// Creates a new Workspace for the authenticated user.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Why Workspaces?</strong> Workspaces provide a collaborative environment where users
    /// can group, save, and discuss context reports. By allowing users to create multiple workspaces,
    /// they can organize properties by project, city, or client.
    /// </para>
    /// </remarks>
    private static async Task<IResult> CreateWorkspace(
        ClaimsPrincipal user,
        [FromBody] CreateWorkspaceDto dto,
        IWorkspaceService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.CreateWorkspaceAsync(userId, dto, ct);
        return Results.Created($"/api/workspaces/{result.Id}", result);
    }

    private static async Task<IResult> GetUserWorkspaces(
        ClaimsPrincipal user,
        IWorkspaceService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.GetUserWorkspacesAsync(userId, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetWorkspace(
        ClaimsPrincipal user,
        Guid id,
        IWorkspaceService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.GetWorkspaceAsync(userId, id, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteWorkspace(
        ClaimsPrincipal user,
        Guid id,
        IWorkspaceService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        await service.DeleteWorkspaceAsync(userId, id, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetActivityLogs(
        ClaimsPrincipal user,
        Guid id,
        IWorkspaceService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.GetActivityLogsAsync(userId, id, ct);
        return Results.Ok(result);
    }

}
