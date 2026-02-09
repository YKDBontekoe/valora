using Valora.Application.DTOs;

namespace Valora.Infrastructure.Enrichment.Builders;

public class ScoringCalculator
{
    public Dictionary<string, double> CalculateCategoryScores(
        IReadOnlyList<ContextMetricDto> socialMetrics,
        IReadOnlyList<ContextMetricDto> crimeMetrics,
        IReadOnlyList<ContextMetricDto> demographicsMetrics,
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

        var amenity = AverageScore(amenityMetrics);
        if (amenity.HasValue) scores["Amenities"] = Math.Round(amenity.Value, 1);

        var environment = AverageScore(environmentMetrics);
        if (environment.HasValue) scores["Environment"] = Math.Round(environment.Value, 1);

        return scores;
    }

    public double CalculateCompositeScore(IReadOnlyDictionary<string, double> categoryScores)
    {
        if (categoryScores.Count == 0)
        {
            return 0;
        }

        // Weighted average with emphasis on safety and amenities
        var weights = new Dictionary<string, double>
        {
            ["Social"] = 0.20,
            ["Safety"] = 0.25,
            ["Demographics"] = 0.10,
            ["Amenities"] = 0.30,
            ["Environment"] = 0.15
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
