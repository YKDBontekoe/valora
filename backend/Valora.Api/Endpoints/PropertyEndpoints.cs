using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;

namespace Valora.Api.Endpoints;

public static class PropertyEndpoints
{
    public static RouteGroupBuilder MapPropertyEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api")
            .WithTags("Properties");

        group.MapGet("/properties/{id}", GetPropertyDetailHandler)
            .RequireAuthorization()
            .WithName("GetPropertyDetail");

        group.MapGet("/map/properties", GetMapPropertiesHandler)
            .RequireAuthorization()
            .WithName("GetMapProperties");

        return group;
    }

    public static async Task<IResult> GetPropertyDetailHandler(
        Guid id,
        IMapService mapService,
        CancellationToken ct)
    {
        var property = await mapService.GetPropertyDetailAsync(id, ct);
        if (property == null)
        {
            return Results.NotFound();
        }
        return Results.Ok(property);
    }

    public static async Task<IResult> GetMapPropertiesHandler(
        [FromQuery] double minLat,
        [FromQuery] double minLon,
        [FromQuery] double maxLat,
        [FromQuery] double maxLon,
        IMapService mapService,
        CancellationToken ct)
    {
        if (!AreCoordinatesValid(minLat, minLon, maxLat, maxLon, out var error))
        {
            return Results.BadRequest(new { error });
        }

        var properties = await mapService.GetMapPropertiesAsync(minLat, minLon, maxLat, maxLon, ct);
        return Results.Ok(properties);
    }

    private static bool AreCoordinatesValid(double minLat, double minLon, double maxLat, double maxLon, out string? error)
    {
        if (!double.IsFinite(minLat) || !double.IsFinite(minLon) || !double.IsFinite(maxLat) || !double.IsFinite(maxLon))
        {
            error = "Coordinates must be finite numbers.";
            return false;
        }
        if (minLat < -90 || minLat > 90 || maxLat < -90 || maxLat > 90)
        {
            error = "Latitudes must be between -90 and 90.";
            return false;
        }
        if (minLon < -180 || minLon > 180 || maxLon < -180 || maxLon > 180)
        {
            error = "Longitudes must be between -180 and 180.";
            return false;
        }
        if (minLat > maxLat)
        {
            error = "minLat must be less than maxLat.";
            return false;
        }
        if (minLon > maxLon)
        {
            error = "minLon must be less than maxLon.";
            return false;
        }
        error = null;
        return true;
    }
}
