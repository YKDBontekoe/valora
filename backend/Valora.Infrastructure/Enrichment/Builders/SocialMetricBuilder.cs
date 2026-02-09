using Valora.Application.DTOs;

namespace Valora.Infrastructure.Enrichment.Builders;

public class SocialMetricBuilder
{
    public List<ContextMetricDto> Build(NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        if (cbs is null)
        {
            warnings.Add("Social indicators were unavailable; social score is partial.");
            return [];
        }

        var densityScore = ScoreDensity(cbs.PopulationDensity);
        var lowIncomeScore = ScoreLowIncome(cbs.LowIncomeHouseholdsPercent);
        var wozScore = ScoreWoz(cbs.AverageWozValueKeur);

        return
        [
            new("residents", "Residents", cbs.Residents, "people", null, "CBS StatLine 83765NED"),
            new("population_density", "Population Density", cbs.PopulationDensity, "people/km²", densityScore, "CBS StatLine 83765NED"),
            new("low_income_households", "Low Income Households", cbs.LowIncomeHouseholdsPercent, "%", lowIncomeScore, "CBS StatLine 83765NED"),
            new("average_woz", "Average WOZ Value", cbs.AverageWozValueKeur, "k€", wozScore, "CBS StatLine 83765NED")
        ];
    }

    private static double? ScoreDensity(int? density)
    {
        if (!density.HasValue) return null;

        return density.Value switch
        {
            <= 500 => 65,
            <= 1500 => 85,
            <= 3500 => 100,
            <= 7000 => 70,
            _ => 50
        };
    }

    private static double? ScoreLowIncome(double? lowIncomePercent)
    {
        if (!lowIncomePercent.HasValue) return null;
        return Math.Clamp(100 - (lowIncomePercent.Value * 8), 0, 100);
    }

    private static double? ScoreWoz(double? wozKeur)
    {
        if (!wozKeur.HasValue) return null;
        return Math.Clamp((wozKeur.Value - 150) / 3, 0, 100);
    }
}
