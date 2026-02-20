using Valora.Application.DTOs;
using Valora.Domain.Common;
using Valora.Domain.Services.Scoring;

namespace Valora.Application.Enrichment.Builders;

public static class SocialMetricBuilder
{
    public static List<ContextMetricDto> Build(NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        if (cbs is null)
        {
            warnings.Add("CBS neighborhood indicators were unavailable; social score is partial.");
            return [];
        }

        var densityScore = SocialScoringRules.ScoreDensity(cbs.PopulationDensity);
        var lowIncomeScore = SocialScoringRules.ScoreLowIncome(cbs.LowIncomeHouseholdsPercent);
        var wozScore = SocialScoringRules.ScoreWoz(cbs.AverageWozValueKeur);

        return
        [
            new("residents", "Residents", cbs.Residents, "people", null, DataSources.CbsStatLine),
            new("population_density", "Population Density", cbs.PopulationDensity, "people/km²", densityScore, DataSources.CbsStatLine),
            new("low_income_households", "Low Income Households", cbs.LowIncomeHouseholdsPercent, "%", lowIncomeScore, DataSources.CbsStatLine),
            new("average_woz", "Average WOZ Value", cbs.AverageWozValueKeur, "k€", wozScore, DataSources.CbsStatLine)
        ];
    }
}
