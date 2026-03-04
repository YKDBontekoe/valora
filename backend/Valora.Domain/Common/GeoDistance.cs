namespace Valora.Domain.Common;

public static class GeoDistance
{
    private const double EarthRadiusMeters = 6371000;

    /// <summary>
    /// Calculates the shortest distance over the earth's surface using the Haversine formula.
    /// </summary>
    /// <remarks>
    /// Haversine formula provides a relatively accurate distance calculation assuming a spherical Earth.
    /// The formula computes the great-circle distance between two points given their longitudes and latitudes.
    /// </remarks>
    public static double BetweenMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var deltaLat = DegreesToRadians(lat2 - lat1);
        var deltaLon = DegreesToRadians(lon2 - lon1);

        var haversineA = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                         Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                         Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var safeA = Math.Clamp(haversineA, 0.0, 1.0);
        var haversineC = 2 * Math.Atan2(Math.Sqrt(safeA), Math.Sqrt(1 - safeA));
        return EarthRadiusMeters * haversineC;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }
}
