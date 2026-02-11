using Valora.Application.DTOs;

namespace Valora.Application.Enrichment.Builders;

public static class MobilityMetricBuilder
{
    public static List<ContextMetricDto> Build(NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        if (cbs is null) return [];

        return
        [
            new("mobility_cars_household", "Cars per Household", cbs.CarsPerHousehold, "cars/hh", null, "CBS StatLine 85618NED"),
            new("mobility_car_density", "Car Density", cbs.CarDensity, "cars/km²", null, "CBS StatLine 85618NED"),
            new("mobility_total_cars", "Total Cars", cbs.TotalCars, "cars", null, "CBS StatLine 85618NED")
        ];
    }
}
