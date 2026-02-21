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
            if (!AreCoordinatesValid(minLat, minLon, maxLat, maxLon, out var error))
            {
                return Results.BadRequest(new { error });
            }

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
            if (!AreCoordinatesValid(minLat, minLon, maxLat, maxLon, out var error))
            {
                return Results.BadRequest(new { error });
            }

            var overlays = await mapService.GetMapOverlaysAsync(minLat, minLon, maxLat, maxLon, metric, ct);
            return Results.Ok(overlays);
        })
        .RequireAuthorization()
        .WithName("GetMapOverlays");

        return group;
    }

    private static bool AreCoordinatesValid(double minLat, double minLon, double maxLat, double maxLon, out string? error)
    {
        if (minLat < -90 || minLat > 90 || maxLat < -90 || maxLat > 90)
        {
            error = "Latitude must be between -90 and 90.";
            return false;
        }
        if (minLon < -180 || minLon > 180 || maxLon < -180 || maxLon > 180)
        {
            error = "Longitude must be between -180 and 180.";
            return false;
        }
        if (minLat > maxLat)
        {
            error = "minLat cannot be greater than maxLat.";
            return false;
        }
        if (minLon > maxLon)
        {
            error = "minLon cannot be greater than maxLon.";
            return false;
        }
        error = null;
        return true;
    }
}
