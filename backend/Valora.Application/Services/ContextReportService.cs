using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Application.Enrichment.Builders;

namespace Valora.Application.Services;

public sealed class ContextReportService : IContextReportService
{
    private readonly ILocationResolver _locationResolver;
    private readonly IContextDataProvider _contextDataProvider;
    private readonly ICacheService _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<ContextReportService> _logger;

    public ContextReportService(
        ILocationResolver locationResolver,
        IContextDataProvider contextDataProvider,
        ICacheService cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<ContextReportService> logger)
    {
        _locationResolver = locationResolver;
        _contextDataProvider = contextDataProvider;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ResolvedLocationDto?> ResolveLocationAsync(string input, CancellationToken ct = default)
    {
        return await _locationResolver.ResolveAsync(input, ct);
    }

    public async Task<List<ContextMetricDto>> GetSocialMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default)
    {
        var data = await _contextDataProvider.GetNeighborhoodStatsAsync(location, ct);
        return SocialMetricBuilder.Build(data, warnings);
    }

    public async Task<List<ContextMetricDto>> GetSafetyMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default)
    {
        var data = await _contextDataProvider.GetCrimeStatsAsync(location, ct);
        return CrimeMetricBuilder.Build(data, warnings);
    }

    public async Task<List<ContextMetricDto>> GetAmenityMetricsAsync(ResolvedLocationDto location, int radiusMeters, List<string> warnings, CancellationToken ct = default)
    {
        var amenities = await _contextDataProvider.GetAmenityStatsAsync(location, radiusMeters, ct);
        var cbs = await _contextDataProvider.GetNeighborhoodStatsAsync(location, ct);
        return AmenityMetricBuilder.Build(amenities, cbs, warnings);
    }

    public async Task<List<ContextMetricDto>> GetEnvironmentMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default)
    {
        var data = await _contextDataProvider.GetAirQualitySnapshotAsync(location, ct);
        return EnvironmentMetricBuilder.Build(data, warnings);
    }

    public async Task<List<ContextMetricDto>> GetDemographicsMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default)
    {
        var data = await _contextDataProvider.GetNeighborhoodStatsAsync(location, ct);
        return DemographicsMetricBuilder.Build(data, warnings);
    }

    public async Task<List<ContextMetricDto>> GetHousingMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default)
    {
        var data = await _contextDataProvider.GetNeighborhoodStatsAsync(location, ct);
        return HousingMetricBuilder.Build(data, warnings);
    }

    public async Task<List<ContextMetricDto>> GetMobilityMetricsAsync(ResolvedLocationDto location, List<string> warnings, CancellationToken ct = default)
    {
        var data = await _contextDataProvider.GetNeighborhoodStatsAsync(location, ct);
        return MobilityMetricBuilder.Build(data, warnings);
    }

    public async Task<ContextReportDto> BuildAsync(ContextReportRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            throw new ValidationException(new[] { "Input is required." });
        }

        var normalizedRadius = Math.Clamp(request.RadiusMeters, 200, 5000);
        var location = await _locationResolver.ResolveAsync(request.Input, cancellationToken);
        if (location is null)
        {
            throw new ValidationException(new[] { "Could not resolve input to a Dutch address." });
        }

        var latKey = location.Latitude.ToString("F5");
        var lonKey = location.Longitude.ToString("F5");
        var cacheKey = $"context-report:v3:{latKey}_{lonKey}:{normalizedRadius}";

        if (_cache.TryGetValue(cacheKey, out ContextReportDto? cached) && cached is not null)
        {
            return cached;
        }

        var sourceData = await _contextDataProvider.GetSourceDataAsync(location, normalizedRadius, cancellationToken);
        var warnings = new List<string>(sourceData.Warnings);

        if (normalizedRadius != request.RadiusMeters)
        {
            warnings.Add($"Radius clamped from {request.RadiusMeters}m to {normalizedRadius}m to respect system limits.");
        }

        var socialMetrics = SocialMetricBuilder.Build(sourceData.NeighborhoodStats, warnings);
        var crimeMetrics = CrimeMetricBuilder.Build(sourceData.CrimeStats, warnings);
        var demographicsMetrics = DemographicsMetricBuilder.Build(sourceData.NeighborhoodStats, warnings);
        var housingMetrics = HousingMetricBuilder.Build(sourceData.NeighborhoodStats, warnings);
        var mobilityMetrics = MobilityMetricBuilder.Build(sourceData.NeighborhoodStats, warnings);
        var amenityMetrics = AmenityMetricBuilder.Build(sourceData.AmenityStats, sourceData.NeighborhoodStats, warnings);
        var environmentMetrics = EnvironmentMetricBuilder.Build(sourceData.AirQualitySnapshot, warnings);

        var categoryScores = ContextScoreCalculator.ComputeCategoryScores(socialMetrics, crimeMetrics, demographicsMetrics, housingMetrics, mobilityMetrics, amenityMetrics, environmentMetrics);
        var compositeScore = ContextScoreCalculator.ComputeCompositeScore(categoryScores);

        var report = new ContextReportDto(
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

        _cache.Set(cacheKey, report, TimeSpan.FromMinutes(_options.ReportCacheMinutes));
        return report;
    }
}
