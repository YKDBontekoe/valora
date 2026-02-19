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
            if (minLat < -90 || minLat > 90 || maxLat < -90 || maxLat > 90) return Results.BadRequest(new { error = "Latitudes must be between -90 and 90" });
            if (minLon < -180 || minLon > 180 || maxLon < -180 || maxLon > 180) return Results.BadRequest(new { error = "Longitudes must be between -180 and 180" });
            if (minLat >= maxLat) return Results.BadRequest(new { error = "minLat must be less than maxLat" });
            if (minLon >= maxLon) return Results.BadRequest(new { error = "minLon must be less than maxLon" });

            const double maxSpan = 0.5;
            if (maxLat - minLat > maxSpan || maxLon - minLon > maxSpan)
            {
                return Results.BadRequest(new { error = $"Bounding box span too large. Maximum allowed is {maxSpan} degrees." });
            }

            var typeList = types?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();

            if (typeList != null && typeList.Any(t => t.Length > 50 || !t.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')))
            {
                return Results.BadRequest(new { error = "Invalid amenity type format." });
            }

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
            if (minLat < -90 || minLat > 90 || maxLat < -90 || maxLat > 90) return Results.BadRequest(new { error = "Latitudes must be between -90 and 90" });
            if (minLon < -180 || minLon > 180 || maxLon < -180 || maxLon > 180) return Results.BadRequest(new { error = "Longitudes must be between -180 and 180" });
            if (minLat >= maxLat) return Results.BadRequest(new { error = "minLat must be less than maxLat" });
            if (minLon >= maxLon) return Results.BadRequest(new { error = "minLon must be less than maxLon" });

            const double maxSpan = 0.5;
            if (maxLat - minLat > maxSpan || maxLon - minLon > maxSpan)
            {
                return Results.BadRequest(new { error = $"Bounding box span too large. Maximum allowed is {maxSpan} degrees." });
            }

            if (!Enum.IsDefined(typeof(MapOverlayMetric), metric))
            {
                return Results.BadRequest(new { error = "Invalid map overlay metric." });
            }

            var overlays = await mapService.GetMapOverlaysAsync(minLat, minLon, maxLat, maxLon, metric, ct);
            return Results.Ok(overlays);
        })
        .RequireAuthorization()
        .WithName("GetMapOverlays");

        return group;
    }
}
