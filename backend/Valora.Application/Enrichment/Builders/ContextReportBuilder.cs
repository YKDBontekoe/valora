using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Domain.Models;
using Valora.Domain.Services;

namespace Valora.Application.Enrichment.Builders;

/// <summary>
/// Aggregates raw data into a scored report (The "Fan-In" phase).
/// </summary>
public static class ContextReportBuilder
{
    /// <summary>
    /// Builds the context report by normalizing metrics and computing scores.
    /// </summary>
    /// <param name="location">The resolved location.</param>
    /// <param name="sourceData">The raw source data.</param>
    /// <param name="warnings">A list of warnings to populate.</param>
    /// <returns>A fully constructed ContextReportDto.</returns>
    public static ContextReportDto Build(
        ResolvedLocationDto location,
        ContextSourceData sourceData,
        List<string> warnings)
    {
        var cbs = sourceData.NeighborhoodStats;
        var crime = sourceData.CrimeStats;
        var amenities = sourceData.AmenityStats;
        var air = sourceData.AirQualitySnapshot;

        // Raw data is converted to uniform ContextMetricDto objects.
        // Each Builder encapsulates the logic for a specific domain (Social, Crime, etc.)
        var socialMetrics = SocialMetricBuilder.Build(cbs, warnings);
        var crimeMetrics = CrimeMetricBuilder.Build(crime, warnings);
        var demographicsMetrics = DemographicsMetricBuilder.Build(cbs, warnings);
        var housingMetrics = HousingMetricBuilder.Build(cbs, warnings); // Phase 2
        var mobilityMetrics = MobilityMetricBuilder.Build(cbs, warnings); // Phase 2
        var amenityMetrics = AmenityMetricBuilder.Build(amenities, cbs, warnings); // Phase 2: CBS Proximity
        var environmentMetrics = EnvironmentMetricBuilder.Build(air, warnings);

        // Compute scores
        // We map DTOs to Domain Models here to enforce Clean Architecture boundaries.
        // The Domain layer calculates the final scores.
        var metricsInput = new CategoryMetricsModel(
            MapToDomain(socialMetrics),
            MapToDomain(crimeMetrics),
            MapToDomain(demographicsMetrics),
            MapToDomain(housingMetrics),
            MapToDomain(mobilityMetrics),
            MapToDomain(amenityMetrics),
            MapToDomain(environmentMetrics));

        var categoryScores = ContextScoreCalculator.ComputeCategoryScores(metricsInput);
        var compositeScore = ContextScoreCalculator.ComputeCompositeScore(categoryScores);

        return new ContextReportDto(
            Location: location,
            SocialMetrics: socialMetrics,
            CrimeMetrics: crimeMetrics,
            DemographicsMetrics: demographicsMetrics,
            HousingMetrics: housingMetrics,
            MobilityMetrics: mobilityMetrics,
            AmenityMetrics: amenityMetrics,
            EnvironmentMetrics: environmentMetrics,
            CompositeScore: Math.Round(compositeScore, 1),
            CategoryScores: categoryScores,
            Sources: sourceData.Sources,
            Warnings: warnings);
    }

    private static IReadOnlyList<ContextMetricModel> MapToDomain(IEnumerable<ContextMetricDto> dtos)
    {
        return dtos.Select(d => new ContextMetricModel(
            d.Key,
            d.Label,
            d.Value,
            d.Unit,
            d.Score,
            d.Source,
            d.Note
        )).ToList();
    }
}
