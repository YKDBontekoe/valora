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
            if (!ValidateBbox(minLat, minLon, maxLat, maxLon, out var errorMessage))
            {
                return Results.BadRequest(errorMessage);
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
            if (!ValidateBbox(minLat, minLon, maxLat, maxLon, out var errorMessage))
            {
                return Results.BadRequest(errorMessage);
            }

            var overlays = await mapService.GetMapOverlaysAsync(minLat, minLon, maxLat, maxLon, metric, ct);
            return Results.Ok(overlays);
        })
        .RequireAuthorization()
        .WithName("GetMapOverlays");

        return group;
    }

    private static bool ValidateBbox(double minLat, double minLon, double maxLat, double maxLon, out string? errorMessage)
    {
        errorMessage = null;

        if (minLat < -90 || minLat > 90 || maxLat < -90 || maxLat > 90)
        {
            errorMessage = "Latitudes must be between -90 and 90.";
            return false;
        }

        if (minLon < -180 || minLon > 180 || maxLon < -180 || maxLon > 180)
        {
            errorMessage = "Longitudes must be between -180 and 180.";
            return false;
        }

        if (minLat >= maxLat)
        {
            errorMessage = "minLat must be less than maxLat.";
            return false;
        }

        if (minLon >= maxLon)
        {
            errorMessage = "minLon must be less than maxLon.";
            return false;
        }

        // Limit bbox size to prevent heavy queries (max 0.5 deg span in each direction)
        const double maxSpan = 0.5;
        if (maxLat - minLat > maxSpan || maxLon - minLon > maxSpan)
        {
            errorMessage = $"Bounding box span too large. Maximum allowed is {maxSpan} degrees.";
            return false;
        }

        return true;
    }
}
