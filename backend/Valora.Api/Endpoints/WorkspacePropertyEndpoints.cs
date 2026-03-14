using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class WorkspacePropertyEndpoints
{
    public static RouteGroupBuilder MapWorkspacePropertyEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/workspaces")
            .RequireAuthorization()
            .RequireRateLimiting(Valora.Api.Constants.RateLimitPolicies.Fixed)
            .WithTags("Workspace Properties");

        group.MapGet("/{id}/properties", GetSavedProperties);
        group.MapPost("/{id}/properties", SaveProperty)
            .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<SavePropertyDto>>();

        group.MapPost("/{id}/properties/from-report", SavePropertyFromReport)
            .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<SavePropertyFromReportDto>>();

        group.MapDelete("/{id}/properties/{savedPropertyId}", RemoveSavedProperty);

        group.MapGet("/{id}/properties/{savedPropertyId}/comments", GetComments);
        group.MapPost("/{id}/properties/{savedPropertyId}/comments", AddComment)
            .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<AddCommentDto>>();

        return group;
    }

    private static async Task<IResult> GetSavedProperties(
        ClaimsPrincipal user,
        Guid id,
        IWorkspacePropertyService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.GetSavedPropertiesAsync(userId, id, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> SaveProperty(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] SavePropertyDto request,
        IWorkspacePropertyService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.SavePropertyAsync(userId, id, request.PropertyId, request.Notes, ct);
        return Results.Ok(result);
    }

    /// <summary>
    /// Saves a newly generated Context Report directly to a Workspace.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Why save reports?</strong> Context reports are dynamically generated in real-time ("Fan-Out").
    /// Saving a report to a workspace takes a "snapshot" of the data and persists it to the database,
    /// allowing users to revisit the exact stats, share them with team members, and add comments.
    /// </para>
    /// </remarks>
    private static async Task<IResult> SavePropertyFromReport(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] SavePropertyFromReportDto request,
        IWorkspacePropertyService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.SaveContextReportAsync(userId, id, request.Report, request.Notes, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> RemoveSavedProperty(
        ClaimsPrincipal user,
        Guid id,
        Guid savedPropertyId,
        IWorkspacePropertyService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        await service.RemoveSavedPropertyAsync(userId, id, savedPropertyId, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetComments(
        ClaimsPrincipal user,
        Guid id,
        Guid savedPropertyId,
        IWorkspacePropertyService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.GetCommentsAsync(userId, id, savedPropertyId, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> AddComment(
        ClaimsPrincipal user,
        Guid id,
        Guid savedPropertyId,
        [FromBody] AddCommentDto dto,
        IWorkspacePropertyService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.AddCommentAsync(userId, id, savedPropertyId, dto, ct);
        return Results.Ok(result);
    }
}
