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
    /// Based on user research:
    /// <list type="bullet">
    /// <item><strong>Amenities (25%):</strong> Primary driver for daily convenience.</item>
    /// <item><strong>Safety (20%) &amp; Social (20%):</strong> Critical "hygiene factors".</item>
    /// <item><strong>Environment/Demographics/Housing (10%):</strong> Contextual factors.</item>
    /// <item><strong>Mobility (5%):</strong> User-specific preference.</item>
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
    public static Dictionary<string, double> ComputeCategoryScores(
        IReadOnlyList<ContextMetricDto> socialMetrics,
        IReadOnlyList<ContextMetricDto> crimeMetrics,
        IReadOnlyList<ContextMetricDto> demographicsMetrics,
        IReadOnlyList<ContextMetricDto> housingMetrics,
        IReadOnlyList<ContextMetricDto> mobilityMetrics,
        IReadOnlyList<ContextMetricDto> amenityMetrics,
        IReadOnlyList<ContextMetricDto> environmentMetrics)
    {
        var scores = new Dictionary<string, double>();

        AddScore(scores, CategorySocial, socialMetrics);
        AddScore(scores, CategorySafety, crimeMetrics);
        AddScore(scores, CategoryDemographics, demographicsMetrics);
        AddScore(scores, CategoryHousing, housingMetrics);
        AddScore(scores, CategoryMobility, mobilityMetrics);
        AddScore(scores, CategoryAmenities, amenityMetrics);
        AddScore(scores, CategoryEnvironment, environmentMetrics);

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

    private static double? AverageScore(IReadOnlyList<ContextMetricDto> metrics)
    {
        var values = metrics.Where(m => m.Score.HasValue).Select(m => m.Score!.Value).ToList();
        if (values.Count == 0)
        {
            return null;
        }

        return values.Average();
    }
}
