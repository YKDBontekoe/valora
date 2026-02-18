using Valora.Domain.Models;

namespace Valora.Domain.Services;

/// <summary>
/// Container for the various metric collections used during score calculation.
/// </summary>
public record CategoryMetricsInput(
    IReadOnlyList<ContextMetricModel> SocialMetrics,
    IReadOnlyList<ContextMetricModel> CrimeMetrics,
    IReadOnlyList<ContextMetricModel> DemographicsMetrics,
    IReadOnlyList<ContextMetricModel> HousingMetrics,
    IReadOnlyList<ContextMetricModel> MobilityMetrics,
    IReadOnlyList<ContextMetricModel> AmenityMetrics,
    IReadOnlyList<ContextMetricModel> EnvironmentMetrics
);
