using Valora.Application.DTOs;

namespace Valora.Application.Enrichment.Builders;

public static class MobilityMetricBuilder
{
    public static List<ContextMetricDto> Build(NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        if (cbs is null) return [];

        var transitAccessScore = ScoreTransitAccess(cbs.DistanceToSupermarket, cbs.DistanceToGp, cbs.DistanceToSchool);
        var carDependencyScore = ScoreCarDependency(cbs.CarsPerHousehold);

        return
        [
            new("mobility_cars_household", "Cars per Household", cbs.CarsPerHousehold, "cars/hh", carDependencyScore, "CBS StatLine 85618NED"),
            new("mobility_car_density", "Car Density", cbs.CarDensity, "cars/km²", null, "CBS StatLine 85618NED"),
            new("mobility_total_cars", "Total Cars", cbs.TotalCars, "cars", null, "CBS StatLine 85618NED"),
            new("mobility_transit_access", "Local Access Score", transitAccessScore, "score", transitAccessScore, "Valora Composite")
        ];
    }

    private static double? ScoreCarDependency(double? carsPerHousehold)
    {
        if (!carsPerHousehold.HasValue) return null;

        // Lower car dependency can indicate stronger local accessibility and transit alternatives.
        return carsPerHousehold.Value switch
        {
            <= 0.7 => 100,
            <= 1.0 => 85,
            <= 1.3 => 70,
            <= 1.6 => 55,
            _ => 40
        };
    }

    private static double? ScoreTransitAccess(double? distSupermarket, double? distGp, double? distSchool)
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
