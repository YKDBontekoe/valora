namespace Valora.Domain.Services.Scoring;

public static class MobilityScoringRules
{
    public static double? ScoreCarDependency(double? carsPerHousehold)
    {
        if (!carsPerHousehold.HasValue) return null;

        return carsPerHousehold.Value switch
        {
            <= 0.7 => 100,
            <= 1.0 => 85,
            <= 1.3 => 70,
            <= 1.6 => 55,
            _ => 40
        };
    }

    public static double? ScoreTransitAccess(double? distSupermarket, double? distGp, double? distSchool)
    {
        var distances = new[] { distSupermarket, distGp, distSchool }.Where(d => d.HasValue).Select(d => d!.Value).ToList();
        if (distances.Count == 0) return null;

        var average = distances.Average();
        return average switch
        {
            <= 0.75 => 100,
            <= 1.25 => 85,
            <= 2.00 => 70,
            <= 3.00 => 50,
            _ => 30
        };
    }
}
