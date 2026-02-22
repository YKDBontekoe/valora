using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Valora.Api.Filters;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;
using Valora.Application.DTOs.Shared;

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
            .WithName("GetMapAmenities")
            .AddEndpointFilter<ValidationFilter<BoundsRequest>>();

        group.MapGet("/amenities/clusters", GetMapAmenityClustersHandler)
            .RequireAuthorization()
            .WithName("GetMapAmenityClusters")
            .AddEndpointFilter<ValidationFilter<BoundsRequest>>();

        group.MapGet("/overlays", GetMapOverlaysHandler)
            .RequireAuthorization()
            .WithName("GetMapOverlays")
            .AddEndpointFilter<ValidationFilter<BoundsRequest>>();

        group.MapGet("/overlays/tiles", GetMapOverlayTilesHandler)
            .RequireAuthorization()
            .WithName("GetMapOverlayTiles")
            .AddEndpointFilter<ValidationFilter<BoundsRequest>>();

        return group;
    }

    public static async Task<IResult> GetCityInsightsHandler(IMapService mapService, CancellationToken ct)
    {
        var insights = await mapService.GetCityInsightsAsync(ct);
        return Results.Ok(insights);
    }

    public static async Task<IResult> GetMapAmenitiesHandler(
        [AsParameters] BoundsRequest bounds,
        [FromQuery] string? types,
        IMapService mapService,
        CancellationToken ct)
    {
        var typeList = ParseTypes(types);
        var amenities = await mapService.GetMapAmenitiesAsync(bounds.MinLat, bounds.MinLon, bounds.MaxLat, bounds.MaxLon, typeList, ct);
        return Results.Ok(amenities);
    }

    public static async Task<IResult> GetMapAmenityClustersHandler(
        [AsParameters] BoundsRequest bounds,
        [FromQuery] double zoom,
        [FromQuery] string? types,
        IMapService mapService,
        CancellationToken ct)
    {
        var typeList = ParseTypes(types);
        var clusters = await mapService.GetMapAmenityClustersAsync(bounds.MinLat, bounds.MinLon, bounds.MaxLat, bounds.MaxLon, zoom, typeList, ct);
        return Results.Ok(clusters);
    }

    public static async Task<IResult> GetMapOverlaysHandler(
        [AsParameters] BoundsRequest bounds,
        [FromQuery] MapOverlayMetric metric,
        IMapService mapService,
        CancellationToken ct)
    {
        var overlays = await mapService.GetMapOverlaysAsync(bounds.MinLat, bounds.MinLon, bounds.MaxLat, bounds.MaxLon, metric, ct);
        return Results.Ok(overlays);
    }

    public static async Task<IResult> GetMapOverlayTilesHandler(
        [AsParameters] BoundsRequest bounds,
        [FromQuery] double zoom,
        [FromQuery] MapOverlayMetric metric,
        IMapService mapService,
        CancellationToken ct)
    {
        var tiles = await mapService.GetMapOverlayTilesAsync(bounds.MinLat, bounds.MinLon, bounds.MaxLat, bounds.MaxLon, zoom, metric, ct);
        return Results.Ok(tiles);
    }

    private static List<string>? ParseTypes(string? types)
    {
        return types?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToList();
    }
}
