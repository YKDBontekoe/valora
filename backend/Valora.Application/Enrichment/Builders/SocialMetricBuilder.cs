using Valora.Application.DTOs;

namespace Valora.Application.Enrichment.Builders;

public class SocialMetricBuilder
{
    public List<ContextMetricDto> Build(NeighborhoodStatsDto? cbs, List<string> warnings)
    {
        if (cbs is null)
        {
            warnings.Add("CBS neighborhood indicators were unavailable; social score is partial.");
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

    /// <summary>
    /// Scores population density.
    /// Optimal density (~3500 people/km²) is preferred for access to amenities without overcrowding.
    /// </summary>
    private static double? ScoreDensity(int? density)
    {
        if (!density.HasValue) return null;

        return density.Value switch
        {
            <= 500 => 65,    // Rural / Isolated
            <= 1500 => 85,   // Suburban spacious
            <= 3500 => 100,  // Urban optimal
            <= 7000 => 70,   // Urban dense
            _ => 50          // Overcrowded
        };
    }

    /// <summary>
    /// Penalizes neighborhoods with a high percentage of low-income households.
    /// </summary>
    private static double? ScoreLowIncome(double? lowIncomePercent)
    {
        if (!lowIncomePercent.HasValue) return null;
        // Inverse linear relationship: 0% low income -> 100 score, 12.5% -> 0 score
        // The multiplier 8 is aggressive to highlight socio-economic challenges.
        return Math.Clamp(100 - (lowIncomePercent.Value * 8), 0, 100);
    }

    /// <summary>
    /// Scores WOZ value (property valuation).
    /// </summary>
    private static double? ScoreWoz(double? wozKeur)
    {
        if (!wozKeur.HasValue) return null;
        // Example: 450k -> (450-150)/3 = 100 score. 150k -> 0 score.
        return Math.Clamp((wozKeur.Value - 150) / 3, 0, 100);
    }
}
