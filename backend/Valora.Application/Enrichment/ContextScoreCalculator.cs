using Valora.Application.DTOs;

namespace Valora.Application.Enrichment;

public static class ContextScoreCalculator
{
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

        var social = AverageScore(socialMetrics);
        if (social.HasValue) scores["Social"] = Math.Round(social.Value, 1);

        var crime = AverageScore(crimeMetrics);
        if (crime.HasValue) scores["Safety"] = Math.Round(crime.Value, 1);

        var demographics = AverageScore(demographicsMetrics);
        if (demographics.HasValue) scores["Demographics"] = Math.Round(demographics.Value, 1);

        var housing = AverageScore(housingMetrics);
        if (housing.HasValue) scores["Housing"] = Math.Round(housing.Value, 1);

        var mobility = AverageScore(mobilityMetrics);
        if (mobility.HasValue) scores["Mobility"] = Math.Round(mobility.Value, 1);

        var amenity = AverageScore(amenityMetrics);
        if (amenity.HasValue) scores["Amenities"] = Math.Round(amenity.Value, 1);

        var environment = AverageScore(environmentMetrics);
        if (environment.HasValue) scores["Environment"] = Math.Round(environment.Value, 1);

        return scores;
    }

    /// <summary>
    /// Calculates the final 0-100 "Valora Score" based on weighted category scores.
    /// </summary>
    /// <remarks>
    /// Weights are determined heuristically based on general resident priorities:
    /// - Amenities (25%) and Safety (20%) are the strongest drivers for location desirability.
    /// - Social (20%) reflects the neighborhood vibe and stability.
    /// - Environment, Demographics, and Housing have lower weights as they are often secondary or highly subjective factors.
    /// </remarks>
    public static double ComputeCompositeScore(IReadOnlyDictionary<string, double> categoryScores)
    {
        if (categoryScores.Count == 0)
        {
            return 0;
        }

        // Weighted average with emphasis on safety and amenities
        var weights = new Dictionary<string, double>
        {
            ["Social"] = 0.20,
            ["Safety"] = 0.20,
            ["Demographics"] = 0.10,
            ["Housing"] = 0.10,
            ["Mobility"] = 0.05,
            ["Amenities"] = 0.25,
            ["Environment"] = 0.10
        };

        double totalWeight = 0;
        double weightedSum = 0;

        foreach (var kvp in categoryScores)
        {
            if (weights.TryGetValue(kvp.Key, out var weight))
            {
                weightedSum += kvp.Value * weight;
                totalWeight += weight;
            }
        }

        return totalWeight > 0 ? weightedSum / totalWeight : 0;
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
