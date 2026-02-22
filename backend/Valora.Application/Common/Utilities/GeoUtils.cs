using System.Globalization;
using System.Text.Json;
using Valora.Application.Common.Exceptions;

namespace Valora.Application.Common.Utilities;

public static class GeoUtils
{
    private const double MaxSpan = 0.5;

    public class ParsedGeometry
    {
        public List<List<List<Coordinate>>> Polygons { get; set; } = new();
        public BoundingBox BBox { get; set; } = new();
    }

    public struct Coordinate
    {
        public double Lon;
        public double Lat;

        public Coordinate(double lon, double lat)
        {
            Lon = lon;
            Lat = lat;
        }
    }

    public struct BoundingBox
    {
        public double MinLat;
        public double MaxLat;
        public double MinLon;
        public double MaxLon;

        public BoundingBox()
        {
            MinLat = double.MaxValue;
            MaxLat = double.MinValue;
            MinLon = double.MaxValue;
            MaxLon = double.MinValue;
        }
    }

    /// <summary>
    /// Parses a WKT POINT string (e.g., "POINT(4.895 52.370)") into (X, Y) coordinates.
    /// </summary>
    public static (double X, double Y)? TryParseWktPoint(string? point)
    {
        if (string.IsNullOrWhiteSpace(point))
        {
            return null;
        }

        const string prefix = "POINT(";
        if (!point.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !point.EndsWith(')'))
        {
            return null;
        }

        var body = point[prefix.Length..^1];
        var parts = body.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return null;
        }

        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
        {
            return null;
        }

        return (x, y);
    }

    public static bool IsPointInPolygon(double lat, double lon, JsonElement geoJson)
    {
        var parsed = ParseGeometry(geoJson);
        return IsPointInPolygon(lat, lon, parsed);
    }

    public static ParsedGeometry ParseGeometry(JsonElement geoJson)
    {
        var parsed = new ParsedGeometry();
        var bbox = new BoundingBox();

        if (geoJson.ValueKind != JsonValueKind.Object) return parsed;

        if (geoJson.TryGetProperty("type", out var typeProp) &&
            string.Equals(typeProp.GetString(), "Feature", StringComparison.OrdinalIgnoreCase))
        {
            if (geoJson.TryGetProperty("geometry", out var geometry))
            {
                return ParseGeometry(geometry);
            }
            return parsed;
        }

        if (!geoJson.TryGetProperty("type", out typeProp) ||
            !geoJson.TryGetProperty("coordinates", out var coordsProp))
        {
            return parsed;
        }

        var type = typeProp.GetString();

        if (string.Equals(type, "Polygon", StringComparison.OrdinalIgnoreCase))
        {
            parsed.Polygons.Add(ParsePolygonCoordinates(coordsProp, ref bbox));
        }
        else if (string.Equals(type, "MultiPolygon", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var polygonCoords in coordsProp.EnumerateArray())
            {
                parsed.Polygons.Add(ParsePolygonCoordinates(polygonCoords, ref bbox));
            }
        }

        parsed.BBox = bbox;
        return parsed;
    }

    private static List<List<Coordinate>> ParsePolygonCoordinates(JsonElement polygonCoordinates, ref BoundingBox bbox)
    {
        var rings = new List<List<Coordinate>>();
        foreach (var ringElement in polygonCoordinates.EnumerateArray())
        {
            var ring = new List<Coordinate>();
            foreach (var pointElement in ringElement.EnumerateArray())
            {
                if (pointElement.ValueKind == JsonValueKind.Array && pointElement.GetArrayLength() >= 2)
                {
                    double lon = pointElement[0].GetDouble();
                    double lat = pointElement[1].GetDouble();
                    ring.Add(new Coordinate(lon, lat));

                    // Update bbox
                    if (lat < bbox.MinLat) bbox.MinLat = lat;
                    if (lat > bbox.MaxLat) bbox.MaxLat = lat;
                    if (lon < bbox.MinLon) bbox.MinLon = lon;
                    if (lon > bbox.MaxLon) bbox.MaxLon = lon;
                }
            }
            rings.Add(ring);
        }
        return rings;
    }

    public static bool IsPointInPolygon(double lat, double lon, ParsedGeometry geometry)
    {
        // 1. Quick Bounding Box Check
        if (lat < geometry.BBox.MinLat || lat > geometry.BBox.MaxLat ||
            lon < geometry.BBox.MinLon || lon > geometry.BBox.MaxLon)
        {
            return false;
        }

        // 2. Check actual polygons
        foreach (var polygon in geometry.Polygons)
        {
            if (IsPointInPolygonRings(lat, lon, polygon))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPointInPolygonRings(double lat, double lon, List<List<Coordinate>> rings)
    {
        if (rings.Count == 0) return false;

        var exteriorRing = rings[0];
        if (!IsPointInLinearRing(lat, lon, exteriorRing))
        {
            return false;
        }

        // Check holes (if point is in a hole, it's NOT in the polygon)
        for (int i = 1; i < rings.Count; i++)
        {
            if (IsPointInLinearRing(lat, lon, rings[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPointInLinearRing(double lat, double lon, List<Coordinate> ring)
    {
        bool inside = false;
        int count = ring.Count;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            double xi = ring[i].Lon;
            double yi = ring[i].Lat;
            double xj = ring[j].Lon;
            double yj = ring[j].Lat;

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
