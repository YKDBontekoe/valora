using System.Text.Json;
using Valora.Application.Common.Exceptions;

namespace Valora.Application.Common.Utilities;

public static class GeoUtils
{
    private const double MaxSpan = 0.5;

    /// <summary>
    /// Determines if a point is inside a GeoJSON Polygon or MultiPolygon.
    /// </summary>
    /// <param name="lat">Latitude of the point.</param>
    /// <param name="lon">Longitude of the point.</param>
    /// <param name="geoJson">The GeoJSON element (Feature, Polygon, or MultiPolygon).</param>
    /// <returns>True if the point is strictly inside the polygon(s).</returns>
    /// <remarks>
    /// <para>
    /// <strong>Performance Optimization:</strong><br/>
    /// This method iterates directly over the <see cref="JsonElement"/> structure.
    /// It avoids deserializing the entire JSON into C# objects (like List&lt;Coordinate&gt;) to minimize heap allocations
    /// and Garbage Collection pressure, which is critical when processing large MultiPolygons (e.g., detailed city boundaries).
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Implements the Point-in-Polygon check for a single Polygon geometry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Algorithm: Ray Casting</strong><br/>
    /// Uses the Ray Casting (Even-Odd) algorithm via <see cref="IsPointInLinearRing"/>.
    /// </para>
    /// <para>
    /// <strong>Optimization: Bounding Box Pre-Check</strong><br/>
    /// Before running the expensive Ray Casting algorithm (O(N)), we calculate the Bounding Box of the exterior ring (O(N))
    /// and check if the point is within it. This acts as a fast "fail-early" mechanism, rejecting points that are far outside
    /// the polygon with minimal computation.
    /// </para>
    /// </remarks>
    private static bool IsPointInPolygonCoordinates(double lat, double lon, JsonElement polygonCoordinates)
    {
        // GeoJSON Polygon coordinates are an array of LinearRings.
        // The first ring is the exterior boundary.
        // Subsequent rings are interior boundaries (holes).

        var rings = polygonCoordinates.EnumerateArray();
        if (!rings.MoveNext()) return false; // No rings

        var exteriorRing = rings.Current;

        // Optimization: Check Bounding Box of exterior ring first
        if (!IsPointInBoundingBox(lat, lon, exteriorRing))
        {
            return false;
        }

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

    /// <summary>
    /// Calculates the Bounding Box of a linear ring and checks if the point is inside it.
    /// </summary>
    /// <remarks>
    /// This is an O(N) operation that iterates through all points in the ring to find min/max lat/lon.
    /// Although O(N), it involves only simple comparisons, making it much faster than the trigonometric/arithmetic
    /// operations required for the Ray Casting algorithm.
    /// </remarks>
    private static bool IsPointInBoundingBox(double lat, double lon, JsonElement ring)
    {
        double minLat = double.MaxValue;
        double maxLat = double.MinValue;
        double minLon = double.MaxValue;
        double maxLon = double.MinValue;

        foreach (var point in ring.EnumerateArray())
        {
            if (point.ValueKind != JsonValueKind.Array || point.GetArrayLength() < 2) continue;
            double pLon = point[0].GetDouble();
            double pLat = point[1].GetDouble();

            if (pLat < minLat) minLat = pLat;
            if (pLat > maxLat) maxLat = pLat;
            if (pLon < minLon) minLon = pLon;
            if (pLon > maxLon) maxLon = pLon;
        }

        return lat >= minLat && lat <= maxLat && lon >= minLon && lon <= maxLon;
    }

    /// <summary>
    /// Determines if a point is inside a Linear Ring using the Ray Casting algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Ray Casting Algorithm (Even-Odd Rule):</strong><br/>
    /// The algorithm projects a ray from the point in a fixed direction (horizontal) and counts how many times it intersects
    /// the polygon's edges. An odd number of intersections means the point is inside; an even number means it's outside.
    /// </para>
    /// </remarks>
    private static bool IsPointInLinearRing(double lat, double lon, JsonElement ring)
    {
        bool inside = false;
        // Filter out invalid points
        var points = ring.EnumerateArray()
            .Where(p => p.ValueKind == JsonValueKind.Array && p.GetArrayLength() >= 2)
            .ToList();

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
