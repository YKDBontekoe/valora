namespace Valora.Application.Enrichment.Scoring;

public static class DemographicsScorer
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

    public static double? ScoreIncome(double? incomePerInhabitantK)
    {
        if (!incomePerInhabitantK.HasValue) return null;
        return Math.Clamp((incomePerInhabitantK.Value - 18) * 6.5, 0, 100);
    }

    public static double? ScoreEducation(int? low, int? medium, int? high)
    {
        var share = ToPercent(high, low, medium);
        if (!share.HasValue) return null;
        return Math.Clamp(share.Value * 1.4, 0, 100);
    }

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
