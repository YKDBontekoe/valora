using System.Globalization;
using System.Text;

namespace Valora.Infrastructure.Enrichment;

internal static class OverpassQueryBuilder
{
    /// <summary>
    /// Builds an Overpass QL query to find specific amenities around a point.
    /// </summary>
    /// <remarks>
    /// <para><strong>Overpass QL Explanation:</strong></para>
    /// <list type="bullet">
    /// <item><c>[out:json]</c>: Request JSON output format.</item>
    /// <item><c>[timeout:25]</c>: Server-side timeout in seconds. Essential for public servers.</item>
    /// <item><c>nwr</c>: Search for Nodes, Ways, and Relations (all geometry types).</item>
    /// <item><c>(around:{radius},{lat},{lon})</c>: Spatial filter for a circular area.</item>
    /// <item><c>[key=value]</c>: Tag filter (e.g., finding schools).</item>
    /// <item><c>out center tags;</c>: Output only the center coordinate (centroid) and tags. This minimizes payload size by not returning full geometry for large polygons (ways/relations).</item>
    /// </list>
    /// </remarks>
    public static string BuildAmenityQuery(double latitude, double longitude, int radiusMeters)
    {
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);

        return $"[out:json][timeout:25];("
             + $"nwr(around:{radiusMeters},{lat},{lon})[amenity=school];"
             + $"nwr(around:{radiusMeters},{lat},{lon})[shop=supermarket];"
             + $"nwr(around:{radiusMeters},{lat},{lon})[leisure=park];"
             + $"nwr(around:{radiusMeters},{lat},{lon})[amenity~\"hospital|clinic|doctors|pharmacy\"];"
             + $"nwr(around:{radiusMeters},{lat},{lon})[highway=bus_stop];"
             + $"nwr(around:{radiusMeters},{lat},{lon})[railway=station];"
             + $"nwr(around:{radiusMeters},{lat},{lon})[amenity=charging_station];"
             + ");out center tags;";
    }

    /// <summary>
    /// Builds an Overpass QL query to find amenities within a rectangular bounding box (viewport).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used for map visualization. Filters by specific amenity types to prevent returning too much data.
    /// Uses <c>nwr(min_lat, min_lon, max_lat, max_lon)</c> for efficient bounding box queries.
    /// </para>
    /// </remarks>
    public static string BuildBboxQuery(double minLat, double minLon, double maxLat, double maxLon, List<string>? types)
    {
        var bbox = $"{minLat.ToString(CultureInfo.InvariantCulture)},{minLon.ToString(CultureInfo.InvariantCulture)},{maxLat.ToString(CultureInfo.InvariantCulture)},{maxLon.ToString(CultureInfo.InvariantCulture)}";

        var queryBuilder = new StringBuilder();
        queryBuilder.Append($"[out:json][timeout:25];(");

        if (types == null || types.Contains("school")) queryBuilder.Append($"nwr({bbox})[amenity=school];");
        if (types == null || types.Contains("supermarket")) queryBuilder.Append($"nwr({bbox})[shop=supermarket];");
        if (types == null || types.Contains("park")) queryBuilder.Append($"nwr({bbox})[leisure=park];");
        if (types == null || types.Contains("healthcare")) queryBuilder.Append($"nwr({bbox})[amenity~\"hospital|clinic|doctors|pharmacy\"];");
        if (types == null || types.Contains("transit"))
        {
            queryBuilder.Append($"nwr({bbox})[highway=bus_stop];");
            queryBuilder.Append($"nwr({bbox})[railway=station];");
        }
        if (types == null || types.Contains("charging_station")) queryBuilder.Append($"nwr({bbox})[amenity=charging_station];");

        queryBuilder.Append(");out center tags;");
        return queryBuilder.ToString();
    }
}
