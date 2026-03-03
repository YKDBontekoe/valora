namespace Valora.Domain.Common;

public static class GeoDistance
{
    private const double EarthRadiusMeters = 6371000;

    /// <summary>
    /// Calculates the distance in meters between two geographical coordinates.
    /// </summary>
    /// <remarks>
    /// This uses the Haversine formula to compute the great-circle distance between two points on a sphere.
    /// We use this instead of more complex geospatial calculations (like Vincenty's formulae) because:
    /// 1. Performance: It is computationally cheap and fast, crucial for scoring thousands of POIs on the fly.
    /// 2. Accuracy: Over the short distances we care about for neighborhood context (200m - 5km), the Earth's
    ///    ellipsoidal shape causes negligible error, making Haversine perfectly sufficient.
    /// </remarks>
    public static double BetweenMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }
}
