using Valora.Application.DTOs;
using Valora.Domain.Services.Scoring;

namespace Valora.Application.Enrichment.Builders;

public static class MobilityMetricBuilder
{
    public static List<ContextMetricDto> Build(NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        if (cbs is null) return [];

        var transitAccessScore = MobilityScoringRules.ScoreTransitAccess(cbs.DistanceToSupermarket, cbs.DistanceToGp, cbs.DistanceToSchool);
        var carDependencyScore = MobilityScoringRules.ScoreCarDependency(cbs.CarsPerHousehold);

        return
        [
            new("mobility_cars_household", "Cars per Household", cbs.CarsPerHousehold, "cars/hh", carDependencyScore, "CBS StatLine 85618NED"),
            new("mobility_car_density", "Car Density", cbs.CarDensity, "cars/km²", null, "CBS StatLine 85618NED"),
            new("mobility_total_cars", "Total Cars", cbs.TotalCars, "cars", null, "CBS StatLine 85618NED"),
            new("mobility_transit_access", "Local Access Score", transitAccessScore, "score", transitAccessScore, "Valora Composite")
        ];
    }
}
