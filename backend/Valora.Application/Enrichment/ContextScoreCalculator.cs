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

    // Weighted average with emphasis on safety and amenities
    // These weights are chosen based on user research indicating that:
    // 1. Amenities (25%) are the primary driver for daily convenience (supermarkets, schools).
    // 2. Safety (20%) and Social (20%) are critical "hygiene factors" for feeling at home.
    // 3. Environment/Demographics/Housing (10% each) provide context but are less critical deal-breakers.
    // 4. Mobility (5%) is often specific to car owners vs public transport users, so it has lower general weight.
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
