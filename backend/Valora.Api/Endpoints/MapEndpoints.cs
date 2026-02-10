using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;

namespace Valora.Api.Endpoints;

public static class MapEndpoints
{
    public static RouteGroupBuilder MapMapEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/map")
            .WithTags("Map");

        group.MapGet("/cities", async (IMapService mapService, CancellationToken ct) =>
        {
            var insights = await mapService.GetCityInsightsAsync(ct);
            return Results.Ok(insights);
        })
        .RequireAuthorization()
        .WithName("GetCityInsights");

        return group;
    }
}
