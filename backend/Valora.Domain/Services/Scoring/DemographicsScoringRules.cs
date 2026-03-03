namespace Valora.Domain.Services.Scoring;

public static class DemographicsScoringRules
{
    private const string UrbanityZeerSterk = "zeer sterk stedelijk";
    private const string UrbanitySterk = "sterk stedelijk";
    private const string UrbanityMatig = "matig stedelijk";
    private const string UrbanityWeinig = "weinig stedelijk";
    private const string UrbanityNiet = "niet stedelijk";

    /// <summary>
    /// Calculates a family-friendly score based on demographics.
    /// </summary>
    /// <remarks>
    /// Factors in:
    /// - Percentage of family households (with or without children) (>20% boosts score)
    /// - Percentage of children 0-14 (>15% boosts score)
    /// - Average household size (>2 people boosts score)
    /// </remarks>
    public static double? ScoreFamilyFriendly(double? percentFamilyHouseholds, double? percentAge0To14, double? averageHouseholdSize)
    {
        // If no relevant data is present, do not return a phantom score.
        if (!percentFamilyHouseholds.HasValue &&
            !percentAge0To14.HasValue &&
            !averageHouseholdSize.HasValue)
        {
            return null;
        }

        // Composite score based on presence of families and children
        double score = 50; // Start with neutral baseline

        if (percentFamilyHouseholds.HasValue)
            score += (percentFamilyHouseholds.Value - 20) * 1.5; // Boost if family households > 20%

        if (percentAge0To14.HasValue)
            score += (percentAge0To14.Value - 15) * 2; // Boost if children > 15%

        if (averageHouseholdSize.HasValue)
            score += (averageHouseholdSize.Value - 2) * 15; // Larger households indicate families

        return Math.Clamp(score, 0, 100);
    }

    /// <summary>
    /// Scores the average income per inhabitant.
    /// </summary>
    /// <remarks>
    /// The formula maps the typical Dutch income range to a 0-100 score.
    /// Assuming a baseline low-income threshold of 18k (Score = 0).
    /// As income grows, the score increases, capped at 100 (~33k+).
    /// Higher income generally correlates with better maintained neighborhood aesthetics and amenities.
    /// </remarks>
    public static double? ScoreIncome(double? incomePerInhabitantK)
    {
        if (!incomePerInhabitantK.HasValue) return null;
        return Math.Clamp((incomePerInhabitantK.Value - 18) * 6.5, 0, 100);
    }

    /// <summary>
    /// Scores the education level based on the proportion of highly educated residents.
    /// </summary>
    /// <remarks>
    /// This metric highlights areas with higher education densities.
    /// The score uses a 1.4 multiplier on the percentage of highly educated individuals.
    /// A neighborhood with ~71%+ highly educated residents scores 100.
    /// </remarks>
    public static double? ScoreEducation(int? low, int? medium, int? high)
    {
        var share = ToPercent(high, low, medium);
        if (!share.HasValue) return null;
        return Math.Clamp(share.Value * 1.4, 0, 100);
    }

    /// <summary>
    /// Scores the neighborhood's urbanity level based on density categories.
    /// </summary>
    /// <remarks>
    /// Mid-urban neighborhoods (Level 3) tend to provide the best balance of calm and access to amenities, scoring 100.
    /// Highly dense areas (Level 1) suffer from noise and crowding (Score 65).
    /// Rural/non-urban areas (Level 5) lack access to nearby amenities (Score 70).
    /// </remarks>
    public static double? ScoreUrbanity(string? urbanity)
    {
        var level = ParseUrbanityLevel(urbanity);
        if (!level.HasValue) return null;

        // Mid-urban neighborhoods tend to provide the best balance of calm and access.
        return level.Value switch
        {
            1 => 65,
            2 => 85,
            3 => 100,
            4 => 85,
            5 => 70,
            _ => null
        };
    }

    public static double? ParseUrbanityLevel(string? urbanity)
    {
        if (string.IsNullOrWhiteSpace(urbanity)) return null;
        if (int.TryParse(urbanity, out var parsed))
        {
            return parsed;
        }

        return urbanity.Trim().ToLowerInvariant() switch
        {
            UrbanityZeerSterk => 1,
            UrbanitySterk => 2,
            UrbanityMatig => 3,
            UrbanityWeinig => 4,
            UrbanityNiet => 5,
            _ => null
        };
    }

    public static double? ToPercent(int? target, int? one, int? two)
    {
        if (!target.HasValue || !one.HasValue || !two.HasValue) return null;
        var total = target.Value + one.Value + two.Value;
        if (total <= 0) return null;
        return Math.Round((double)target.Value / total * 100, 1);
    }
}
