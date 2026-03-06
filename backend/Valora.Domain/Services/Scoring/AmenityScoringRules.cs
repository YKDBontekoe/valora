namespace Valora.Domain.Services.Scoring;

public static class AmenityScoringRules
{
    public static double ScoreAmenityCount(int schoolCount, int supermarketCount, int parkCount, int healthcareCount, int transitStopCount, int chargingStationCount)
    {
        var total = schoolCount + supermarketCount + parkCount + healthcareCount + transitStopCount + chargingStationCount;
        return Math.Clamp(total * 4, 0, 100);
    }

    public static double? ScoreAmenityProximity(double? nearestDistanceMeters)
    {
        if (!nearestDistanceMeters.HasValue) return null;

        return nearestDistanceMeters.Value switch
        {
            <= 250 => 100,
            <= 500 => 85,
            <= 1000 => 70,
            <= 1500 => 55,
            <= 2000 => 40,
            _ => 25
        };
    }

    public static double? ScoreProximity(double? distanceKm, double optimalKm, double acceptableKm)
    {
        if (!distanceKm.HasValue) return null;

        if (distanceKm <= optimalKm) return 100;
        if (distanceKm <= acceptableKm) return 70;
        return 40;
    }
}
