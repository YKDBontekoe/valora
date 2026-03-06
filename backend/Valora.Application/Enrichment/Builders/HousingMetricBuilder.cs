using Valora.Application.DTOs;
using Valora.Domain.Services.Scoring;

namespace Valora.Application.Enrichment.Builders;

public static class HousingMetricBuilder
{
    public static List<ContextMetricDto> Build(NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        if (cbs is null) return [];

        var ownerScore = HousingScoringRules.ScoreOwnerOccupied(cbs.PercentageOwnerOccupied);
        var privateRentalScore = HousingScoringRules.ScorePrivateRental(cbs.PercentagePrivateRental);
        var buildMixScore = HousingScoringRules.ScoreBuildMix(cbs.PercentagePre2000, cbs.PercentagePost2000);

        return
        [
            new("housing_owner", "Owner-Occupied", cbs.PercentageOwnerOccupied, "%", ownerScore, "CBS StatLine 85618NED"),
            new("housing_rental", "Rental Properties", cbs.PercentageRental, "%", null, "CBS StatLine 85618NED"),
            new("housing_social", "Social Housing", cbs.PercentageSocialHousing, "%", null, "CBS StatLine 85618NED"),
            new("housing_private_rental", "Private Rental", cbs.PercentagePrivateRental, "%", privateRentalScore, "CBS StatLine 85618NED"),
            new("housing_pre2000", "Built Pre-2000", cbs.PercentagePre2000, "%", null, "CBS StatLine 85618NED"),
            new("housing_post2000", "Built Post-2000", cbs.PercentagePost2000, "%", null, "CBS StatLine 85618NED"),
            new("housing_build_mix", "Build-Year Mix", cbs.PercentagePost2000, "%", buildMixScore, "Valora Composite"),
            new("housing_multifamily", "Multi-Family Homes", cbs.PercentageMultiFamily, "%", null, "CBS StatLine 85618NED")
        ];
    }
}
