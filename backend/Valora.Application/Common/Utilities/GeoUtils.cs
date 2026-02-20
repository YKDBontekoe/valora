using Valora.Application.Common.Exceptions;

namespace Valora.Application.Common.Utilities;

public static class GeoUtils
{
    private const double MaxSpan = 0.5;

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
