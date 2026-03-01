using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
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
            .RequireRateLimiting(Valora.Api.Constants.RateLimitPolicies.Strict)
            .WithTags("Map");

        group.MapGet("/cities", GetCityInsightsHandler)
            .RequireAuthorization()
            .WithName("GetCityInsights");

        group.MapGet("/city-insights", GetCityInsightsHandler)
            .RequireAuthorization()
            .WithName("GetCityInsightsAlias");

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

    public static async Task<IResult> GetCityInsightsHandler(IMapService mapService, HttpContext httpContext, CancellationToken ct)
    {
        var insights = await mapService.GetCityInsightsAsync(ct);
        // City insights change at most every 30 min (batch job cadence); allow brief client-side caching.
        httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(5),
        };
        return Results.Ok(insights);
    }

    public static async Task<IResult> GetMapAmenitiesHandler(
        [AsParameters] BoundsRequest bounds,
        [FromQuery] string? types,
        IMapService mapService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var typeList = ParseTypes(types);
        var amenities = await mapService.GetMapAmenitiesAsync(bounds.MinLat, bounds.MinLon, bounds.MaxLat, bounds.MaxLon, typeList, ct);
        
        httpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(10)
        }.ToString();

        return TypedResults.Ok(amenities);
    }

    public static async Task<IResult> GetMapAmenityClustersHandler(
        [AsParameters] BoundsRequest bounds,
        [FromQuery] double zoom,
        [FromQuery] string? types,
        IMapService mapService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var typeList = ParseTypes(types);
        var clusters = await mapService.GetMapAmenityClustersAsync(bounds.MinLat, bounds.MinLon, bounds.MaxLat, bounds.MaxLon, zoom, typeList, ct);
        
        httpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(10)
        }.ToString();

        return TypedResults.Ok(clusters);
    }

    public static async Task<IResult> GetMapOverlaysHandler(
        [AsParameters] BoundsRequest bounds,
        [FromQuery] MapOverlayMetric metric,
        IMapService mapService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var overlays = await mapService.GetMapOverlaysAsync(bounds.MinLat, bounds.MinLon, bounds.MaxLat, bounds.MaxLon, metric, ct);
        
        httpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(10)
        }.ToString();

        return TypedResults.Ok(overlays);
    }

    public static async Task<IResult> GetMapOverlayTilesHandler(
        [AsParameters] BoundsRequest bounds,
        [FromQuery] double zoom,
        [FromQuery] MapOverlayMetric metric,
        IMapService mapService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tiles = await mapService.GetMapOverlayTilesAsync(bounds.MinLat, bounds.MinLon, bounds.MaxLat, bounds.MaxLon, zoom, metric, ct);

        // Cache for 10 minutes
        httpContext.Response.Headers[HeaderNames.CacheControl] = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(10)
        }.ToString();

        return TypedResults.Ok(tiles);
    }
    private static List<string>? ParseTypes(string? types)
    {
        return types?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToList();
    }
}
