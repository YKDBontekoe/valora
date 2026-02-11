using Valora.Application.DTOs;

namespace Valora.Application.Enrichment.Builders;

public static class HousingMetricBuilder
{
    public static List<ContextMetricDto> Build(NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        if (cbs is null) return [];

        return
        [
            new("housing_owner", "Owner-Occupied", cbs.PercentageOwnerOccupied, "%", null, "CBS StatLine 85618NED"),
            new("housing_rental", "Rental Properties", cbs.PercentageRental, "%", null, "CBS StatLine 85618NED"),
            new("housing_social", "Social Housing", cbs.PercentageSocialHousing, "%", null, "CBS StatLine 85618NED"),
            new("housing_pre2000", "Built Pre-2000", cbs.PercentagePre2000, "%", null, "CBS StatLine 85618NED"),
            new("housing_post2000", "Built Post-2000", cbs.PercentagePost2000, "%", null, "CBS StatLine 85618NED"),
            new("housing_multifamily", "Multi-Family Homes", cbs.PercentageMultiFamily, "%", null, "CBS StatLine 85618NED")
        ];
    }
}
