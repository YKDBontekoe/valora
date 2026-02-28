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

        group.MapGet("/{id}/members", GetMembers);
        group.MapPost("/{id}/members", InviteMember)
            .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<InviteMemberDto>>();

        group.MapDelete("/{id}/members/{memberId}", RemoveMember);

        group.MapGet("/{id}/listings", GetSavedListings);
        group.MapPost("/{id}/listings", SaveListing)
            .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<SaveListingDto>>();

        group.MapDelete("/{id}/listings/{savedListingId}", RemoveSavedListing);

        group.MapGet("/{id}/listings/{savedListingId}/comments", GetComments);
        group.MapPost("/{id}/listings/{savedListingId}/comments", AddComment)
            .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<AddCommentDto>>();

        group.MapGet("/{id}/activity", GetActivityLogs);

        return group;
    }

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

    private static async Task<IResult> GetMembers(
        ClaimsPrincipal user,
        Guid id,
        IWorkspaceMemberService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.GetMembersAsync(userId, id, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> InviteMember(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] InviteMemberDto dto,
        IWorkspaceMemberService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        await service.AddMemberAsync(userId, id, dto, ct);
        return Results.Ok();
    }

    private static async Task<IResult> RemoveMember(
        ClaimsPrincipal user,
        Guid id,
        Guid memberId,
        IWorkspaceMemberService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        await service.RemoveMemberAsync(userId, id, memberId, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetSavedListings(
        ClaimsPrincipal user,
        Guid id,
        IWorkspaceListingService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.GetSavedListingsAsync(userId, id, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> SaveListing(
        ClaimsPrincipal user,
        Guid id,
        [FromBody] SaveListingDto request,
        IWorkspaceListingService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.SaveListingAsync(userId, id, request.ListingId, request.Notes, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> RemoveSavedListing(
        ClaimsPrincipal user,
        Guid id,
        Guid savedListingId,
        IWorkspaceListingService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        await service.RemoveSavedListingAsync(userId, id, savedListingId, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetComments(
        ClaimsPrincipal user,
        Guid id,
        Guid savedListingId,
        IWorkspaceListingService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.GetCommentsAsync(userId, id, savedListingId, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> AddComment(
        ClaimsPrincipal user,
        Guid id,
        Guid savedListingId,
        [FromBody] AddCommentDto dto,
        IWorkspaceListingService service,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Results.Unauthorized();

        var result = await service.AddCommentAsync(userId, id, savedListingId, dto, ct);
        return Results.Ok(result);
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
