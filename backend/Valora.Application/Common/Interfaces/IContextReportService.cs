using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IContextReportService
{
    Task<ResolvedLocationDto?> ResolveLocationAsync(string input, CancellationToken ct = default);

    Task<List<ContextMetricDto>> GetSocialMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default);
    Task<List<ContextMetricDto>> GetSafetyMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default);
    Task<List<ContextMetricDto>> GetAmenityMetricsAsync(ResolvedLocationDto location, int radiusMeters, List<string> warnings, CancellationToken ct = default);
    Task<List<ContextMetricDto>> GetEnvironmentMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default);
    Task<List<ContextMetricDto>> GetDemographicsMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default);
    Task<List<ContextMetricDto>> GetHousingMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default);
    Task<List<ContextMetricDto>> GetMobilityMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default);

    Task<ContextReportDto> BuildAsync(ContextReportRequestDto request, CancellationToken cancellationToken = default);
}
