using Valora.Application.DTOs;

namespace Valora.Application.Enrichment;

public static class ContextScoreCalculator
{
    public const string CategorySocial = "Social";
    public const string CategorySafety = "Safety";
    public const string CategoryDemographics = "Demographics";
    public const string CategoryHousing = "Housing";
    public const string CategoryMobility = "Mobility";
    public const string CategoryAmenities = "Amenities";
    public const string CategoryEnvironment = "Environment";

    /// <summary>
    /// Weights used for calculating the composite score.
    /// </summary>
    /// <remarks>
    /// <strong>Logic: User Research &amp; Priorities</strong>
    /// The weighting strategy reflects the "hierarchy of needs" for a typical home buyer:
    /// <list type="bullet">
    /// <item><strong>Amenities (25%):</strong> "Can I walk to a supermarket or school?" is the most frequent query.</item>
    /// <item><strong>Safety (20%) &amp; Social (20%):</strong> Critical "hygiene factors". If these are low, the house is often rejected regardless of other factors.</item>
    /// <item><strong>Environment (10%):</strong> Air quality and noise are growing concerns but often secondary to price/location.</item>
    /// <item><strong>Demographics/Housing (10%):</strong> Contextual factors that define the "vibe".</item>
    /// <item><strong>Mobility (5%):</strong> Highly user-specific (car vs bike vs train), so it carries lower universal weight.</item>
    /// </list>
    /// </remarks>
    private static readonly Dictionary<string, double> Weights = new()
    {
        [CategorySocial] = 0.20,
        [CategorySafety] = 0.20,
        [CategoryDemographics] = 0.10,
        [CategoryHousing] = 0.10,
        [CategoryMobility] = 0.05,
        [CategoryAmenities] = 0.25,
        [CategoryEnvironment] = 0.10
    };

    /// <summary>
    /// Aggregates individual metrics into category scores (0-100).
    /// </summary>
    /// <returns>A dictionary mapping category names to their average scores.</returns>
    public static Dictionary<string, double> ComputeCategoryScores(CategoryMetricsInput input)
    {
        var scores = new Dictionary<string, double>();

        AddScore(scores, CategorySocial, input.SocialMetrics);
        AddScore(scores, CategorySafety, input.CrimeMetrics);
        AddScore(scores, CategoryDemographics, input.DemographicsMetrics);
        AddScore(scores, CategoryHousing, input.HousingMetrics);
        AddScore(scores, CategoryMobility, input.MobilityMetrics);
        AddScore(scores, CategoryAmenities, input.AmenityMetrics);
        AddScore(scores, CategoryEnvironment, input.EnvironmentMetrics);

        return scores;
    }

    /// <summary>
    /// Calculates the final 0-100 "Valora Score" based on weighted category scores.
    /// </summary>
    public static double ComputeCompositeScore(IReadOnlyDictionary<string, double> categoryScores)
    {
        if (categoryScores.Count == 0)
        {
            return 0;
        }

        double totalWeight = 0;
        double weightedSum = 0;

        foreach (var kvp in categoryScores)
        {
            if (Weights.TryGetValue(kvp.Key, out var weight))
            {
                weightedSum += kvp.Value * weight;
                totalWeight += weight;
            }
        }

        // If totalWeight is 0 (shouldn't happen with default weights), avoid division by zero.
        // We normalize by totalWeight to handle cases where some categories might be missing/skipped in the future.
        return totalWeight > 0 ? weightedSum / totalWeight : 0;
    }

    private static void AddScore(Dictionary<string, double> scores, string category, IReadOnlyList<ContextMetricDto> metrics)
    {
        var average = AverageScore(metrics);
        if (average.HasValue)
        {
            scores[category] = Math.Round(average.Value, 1);
        }
    }

    /// <summary>
    /// Computes the average of all metrics that have a non-null Score.
    /// </summary>
    /// <returns>The average score, or null if no metrics have a score.</returns>
    private static double? AverageScore(IReadOnlyList<ContextMetricDto> metrics)
    {
        // Filter out metrics that failed to compute a score (e.g. missing data)
        var values = metrics.Where(m => m.Score.HasValue).Select(m => m.Score!.Value).ToList();

        if (values.Count == 0)
        {
            return null;
        }

        return values.Average();
    }
}
