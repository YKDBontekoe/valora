using Valora.Application.Common.Constants;
using Valora.Application.DTOs;

namespace Valora.Application.Enrichment.Builders;

public static class HousingMetricBuilder
{
    public static List<ContextMetricDto> Build(NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        if (cbs is null) return [];

        var ownerScore = ScoreOwnerOccupied(cbs.PercentageOwnerOccupied);
        var privateRentalScore = ScorePrivateRental(cbs.PercentagePrivateRental);
        var buildMixScore = ScoreBuildMix(cbs.PercentagePre2000, cbs.PercentagePost2000);

        return
        [
            new("housing_owner", "Owner-Occupied", cbs.PercentageOwnerOccupied, "%", ownerScore, DataSources.CbsStatLine),
            new("housing_rental", "Rental Properties", cbs.PercentageRental, "%", null, DataSources.CbsStatLine),
            new("housing_social", "Social Housing", cbs.PercentageSocialHousing, "%", null, DataSources.CbsStatLine),
            new("housing_private_rental", "Private Rental", cbs.PercentagePrivateRental, "%", privateRentalScore, DataSources.CbsStatLine),
            new("housing_pre2000", "Built Pre-2000", cbs.PercentagePre2000, "%", null, DataSources.CbsStatLine),
            new("housing_post2000", "Built Post-2000", cbs.PercentagePost2000, "%", null, DataSources.CbsStatLine),
            new("housing_build_mix", "Build-Year Mix", cbs.PercentagePost2000, "%", buildMixScore, DataSources.ValoraComposite),
            new("housing_multifamily", "Multi-Family Homes", cbs.PercentageMultiFamily, "%", null, DataSources.CbsStatLine)
        ];
    }

    private static double? ScoreOwnerOccupied(int? value)
    {
        if (!value.HasValue) return null;
        return Math.Clamp(value.Value * 1.25, 0, 100);
    }

    private static double? ScorePrivateRental(int? value)
    {
        if (!value.HasValue) return null;
        return value.Value switch
        {
            <= 10 => 70,
            <= 20 => 85,
            <= 35 => 100,
            <= 50 => 80,
            _ => 60
        };
    }

    private static double? ScoreBuildMix(int? pre2000, int? post2000)
    {
        if (!pre2000.HasValue && !post2000.HasValue) return null;
        if (!pre2000.HasValue || !post2000.HasValue)
        {
            return 70;
        }

        var delta = Math.Abs(pre2000.Value - post2000.Value);
        return Math.Clamp(100 - (delta * 1.2), 40, 100);
    }
}
