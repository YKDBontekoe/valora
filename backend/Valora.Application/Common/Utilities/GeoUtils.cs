using System.Text.Json;
using Valora.Application.Common.Exceptions;

namespace Valora.Application.Common.Utilities;

public static class GeoUtils
{
    private const double MaxSpan = 0.5;

    public static bool IsPointInPolygon(double lat, double lon, JsonElement geoJson)
    {
        if (geoJson.ValueKind != JsonValueKind.Object) return false;

        if (!geoJson.TryGetProperty("type", out var typeProp) ||
            !geoJson.TryGetProperty("coordinates", out var coordsProp))
        {
            return false;
        }

        var type = typeProp.GetString();

        if (string.Equals(type, "Polygon", StringComparison.OrdinalIgnoreCase))
        {
            return IsPointInPolygonCoordinates(lat, lon, coordsProp);
        }
        else if (string.Equals(type, "MultiPolygon", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var polygonCoords in coordsProp.EnumerateArray())
            {
                if (IsPointInPolygonCoordinates(lat, lon, polygonCoords))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsPointInPolygonCoordinates(double lat, double lon, JsonElement polygonCoordinates)
    {
        // GeoJSON Polygon coordinates are an array of LinearRings.
        // The first ring is the exterior boundary.
        // Subsequent rings are interior boundaries (holes).

        var rings = polygonCoordinates.EnumerateArray();
        if (!rings.MoveNext()) return false; // No rings

        var exteriorRing = rings.Current;
        if (!IsPointInLinearRing(lat, lon, exteriorRing))
        {
            return false;
        }

        // Check holes (if point is in a hole, it's NOT in the polygon)
        while (rings.MoveNext())
        {
            if (IsPointInLinearRing(lat, lon, rings.Current))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPointInLinearRing(double lat, double lon, JsonElement ring)
    {
        bool inside = false;
        var points = ring.EnumerateArray().ToList();
        int count = points.Count;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            // GeoJSON is [lon, lat]
            double xi = points[i][0].GetDouble();
            double yi = points[i][1].GetDouble();
            double xj = points[j][0].GetDouble();
            double yj = points[j][1].GetDouble();

            bool intersect = ((yi > lat) != (yj > lat)) &&
                             (lon < (xj - xi) * (lat - yi) / (yj - yi) + xi);
            if (intersect) inside = !inside;
        }

        return inside;
    }

    public static void ValidateBoundingBox(double minLat, double minLon, double maxLat, double maxLon)
    {
        if (double.IsNaN(minLat) || double.IsNaN(minLon) || double.IsNaN(maxLat) || double.IsNaN(maxLon) ||
            double.IsInfinity(minLat) || double.IsInfinity(minLon) || double.IsInfinity(maxLat) || double.IsInfinity(maxLon))
        {
            throw new ValidationException("Coordinates must be valid finite numbers.");
        }

        if (minLat < -90 || minLat > 90 || maxLat < -90 || maxLat > 90)
        {
            throw new ValidationException("Latitudes must be between -90 and 90.");
        }

        if (minLon < -180 || minLon > 180 || maxLon < -180 || maxLon > 180)
        {
            throw new ValidationException("Longitudes must be between -180 and 180.");
        }

        if (minLat >= maxLat)
        {
            throw new ValidationException("minLat must be less than maxLat.");
        }

        if (minLon >= maxLon)
        {
            throw new ValidationException("minLon must be less than maxLon.");
        }

        // Limit bbox size to prevent heavy queries
        if (maxLat - minLat > MaxSpan || maxLon - minLon > MaxSpan)
        {
            throw new ValidationException($"Bounding box span too large. Maximum allowed is {MaxSpan} degrees.");
        }
    }
}
