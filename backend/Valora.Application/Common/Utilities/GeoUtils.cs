using System.Text.Json;
using Valora.Application.Common.Exceptions;

namespace Valora.Application.Common.Utilities;

public static class GeoUtils
{
    private const double MaxSpan = 0.5;

    public static bool IsPointInPolygon(double lat, double lon, JsonElement geoJson)
    {
        if (geoJson.ValueKind != JsonValueKind.Object) return false;

        // Handle Feature objects by extracting geometry
        if (geoJson.TryGetProperty("type", out var typeProp) &&
            string.Equals(typeProp.GetString(), "Feature", StringComparison.OrdinalIgnoreCase))
        {
            if (geoJson.TryGetProperty("geometry", out var geometry))
            {
                return IsPointInPolygon(lat, lon, geometry);
            }
            return false;
        }

        if (!geoJson.TryGetProperty("type", out typeProp) ||
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

        // Optimization: Removed redundant Bounding Box check.
        // IsPointInLinearRing iterates all points (O(N)).
        // Ray Casting is O(N).
        // Calculating BBox is O(N).
        // Doing both is O(2N).
        // Without BBox check, we do O(N).
        // If BBox check fails (point outside), we save Ray Casting cost (math ops), but we paid iteration cost.
        // Since we don't cache BBox, it's generally better to just do the Ray Cast or do BBox during Ray Cast (which is complex).
        // Given BBox was recalculated every time, removing it is a win.

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
        if (ring.ValueKind != JsonValueKind.Array) return false;

        bool inside = false;
        int count = ring.GetArrayLength();

        // Find last valid point to initialize j (the "previous" point)
        int j = -1;
        for (int k = count - 1; k >= 0; k--)
        {
             var pk = ring[k];
             if (pk.ValueKind == JsonValueKind.Array && pk.GetArrayLength() >= 2)
             {
                 j = k;
                 break;
             }
        }

        if (j == -1) return false; // No valid points in ring

        for (int i = 0; i < count; i++)
        {
            var pi = ring[i];

            // Ensure valid coordinates for current point
            if (pi.ValueKind != JsonValueKind.Array || pi.GetArrayLength() < 2)
            {
                continue; // Skip invalid point, j remains the previous valid point
            }

            var pj = ring[j];

            // GeoJSON is [lon, lat]
            double xi = pi[0].GetDouble();
            double yi = pi[1].GetDouble();
            double xj = pj[0].GetDouble();
            double yj = pj[1].GetDouble();

            bool intersect = ((yi > lat) != (yj > lat)) &&
                             (lon < (xj - xi) * (lat - yi) / (yj - yi) + xi);
            if (intersect) inside = !inside;

            // Update j to be the current valid point for the next iteration
            j = i;
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
