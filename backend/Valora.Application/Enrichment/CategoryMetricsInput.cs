using Valora.Application.DTOs;

namespace Valora.Application.Enrichment;

/// <summary>
/// Container for the various metric collections used during score calculation.
/// </summary>
public record CategoryMetricsInput(
    IReadOnlyList<ContextMetricDto> SocialMetrics,
    IReadOnlyList<ContextMetricDto> CrimeMetrics,
    IReadOnlyList<ContextMetricDto> DemographicsMetrics,
    IReadOnlyList<ContextMetricDto> HousingMetrics,
    IReadOnlyList<ContextMetricDto> MobilityMetrics,
    IReadOnlyList<ContextMetricDto> AmenityMetrics,
    IReadOnlyList<ContextMetricDto> EnvironmentMetrics
);
