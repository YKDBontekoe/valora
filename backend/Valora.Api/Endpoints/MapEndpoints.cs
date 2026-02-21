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

        group.MapGet("/cities", GetCityInsightsHandler)
            .RequireAuthorization()
            .WithName("GetCityInsights");

        group.MapGet("/amenities", GetMapAmenitiesHandler)
            .RequireAuthorization()
            .WithName("GetMapAmenities");

        group.MapGet("/amenities/clusters", GetMapAmenityClustersHandler)
            .RequireAuthorization()
            .WithName("GetMapAmenityClusters");

        group.MapGet("/overlays", GetMapOverlaysHandler)
            .RequireAuthorization()
            .WithName("GetMapOverlays");

        group.MapGet("/overlays/tiles", GetMapOverlayTilesHandler)
            .RequireAuthorization()
            .WithName("GetMapOverlayTiles");

        group.MapPost("/query", ProcessMapQueryHandler)
            .RequireAuthorization()
            .WithName("ProcessMapQuery");

        return group;
    }

    public static async Task<IResult> GetCityInsightsHandler(IMapService mapService, CancellationToken ct)
    {
        var insights = await mapService.GetCityInsightsAsync(ct);
        return Results.Ok(insights);
    }

    public static async Task<IResult> GetMapAmenitiesHandler(
        [FromQuery] double minLat,
        [FromQuery] double minLon,
        [FromQuery] double maxLat,
        [FromQuery] double maxLon,
        [FromQuery] string? types,
        IMapService mapService,
        CancellationToken ct)
    {
        var typeList = ParseTypes(types);
        var amenities = await mapService.GetMapAmenitiesAsync(minLat, minLon, maxLat, maxLon, typeList, ct);
        return Results.Ok(amenities);
    }

    public static async Task<IResult> GetMapAmenityClustersHandler(
        [FromQuery] double minLat,
        [FromQuery] double minLon,
        [FromQuery] double maxLat,
        [FromQuery] double maxLon,
        [FromQuery] double zoom,
        [FromQuery] string? types,
        IMapService mapService,
        CancellationToken ct)
    {
        var typeList = ParseTypes(types);
        var clusters = await mapService.GetMapAmenityClustersAsync(minLat, minLon, maxLat, maxLon, zoom, typeList, ct);
        return Results.Ok(clusters);
    }

    public static async Task<IResult> GetMapOverlaysHandler(
        [FromQuery] double minLat,
        [FromQuery] double minLon,
        [FromQuery] double maxLat,
        [FromQuery] double maxLon,
        [FromQuery] MapOverlayMetric metric,
        IMapService mapService,
        CancellationToken ct)
    {
        var overlays = await mapService.GetMapOverlaysAsync(minLat, minLon, maxLat, maxLon, metric, ct);
        return Results.Ok(overlays);
    }

    public static async Task<IResult> GetMapOverlayTilesHandler(
        [FromQuery] double minLat,
        [FromQuery] double minLon,
        [FromQuery] double maxLat,
        [FromQuery] double maxLon,
        [FromQuery] double zoom,
        [FromQuery] MapOverlayMetric metric,
        IMapService mapService,
        CancellationToken ct)
    {
        var tiles = await mapService.GetMapOverlayTilesAsync(minLat, minLon, maxLat, maxLon, zoom, metric, ct);
        return Results.Ok(tiles);
    }

    public static async Task<IResult> ProcessMapQueryHandler(
        [FromBody] MapQueryRequest request,
        IMapService mapService,
        CancellationToken ct)
    {
        var result = await mapService.ProcessQueryAsync(request, ct);
        return Results.Ok(result);
    }

    private static List<string>? ParseTypes(string? types)
    {
        return types?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToList();
    }
}
