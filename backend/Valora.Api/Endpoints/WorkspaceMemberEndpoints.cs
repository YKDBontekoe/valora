using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class WorkspaceMemberEndpoints
{
    public static RouteGroupBuilder MapWorkspaceMemberEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{id}/members", GetMembers);
        group.MapPost("/{id}/members", InviteMember)
            .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<InviteMemberDto>>();

        group.MapDelete("/{id}/members/{memberId}", RemoveMember);

        return group;
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
}