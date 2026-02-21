using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        group.MapGet("/amenities", async (
            [FromQuery] double minLat,
            [FromQuery] double minLon,
            [FromQuery] double maxLat,
            [FromQuery] double maxLon,
            [FromQuery] string? types,
            IMapService mapService,
            CancellationToken ct) =>
        {
            var typeList = types?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();

            var amenities = await mapService.GetMapAmenitiesAsync(minLat, minLon, maxLat, maxLon, typeList, ct);
            return Results.Ok(amenities);
        })
        .RequireAuthorization()
        .WithName("GetMapAmenities");

        group.MapGet("/overlays", async (
            [FromQuery] double minLat,
            [FromQuery] double minLon,
            [FromQuery] double maxLat,
            [FromQuery] double maxLon,
            [FromQuery] MapOverlayMetric metric,
            IMapService mapService,
            CancellationToken ct) =>
        {
            var overlays = await mapService.GetMapOverlaysAsync(minLat, minLon, maxLat, maxLon, metric, ct);
            return Results.Ok(overlays);
        })
        .RequireAuthorization()
        .WithName("GetMapOverlays");

        return group;
    }
}
