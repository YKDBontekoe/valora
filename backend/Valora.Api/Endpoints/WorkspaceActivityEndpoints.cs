using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class WorkspaceActivityEndpoints
{
    public static RouteGroupBuilder MapWorkspaceActivityEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/workspaces")
            .RequireAuthorization()
            .RequireRateLimiting(Valora.Api.Constants.RateLimitPolicies.Fixed)
            .WithTags("Workspaces");

        group.MapGet("/{id}/activity", GetActivityLogs);

        return group;
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
