using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Application.Enrichment.Builders;

namespace Valora.Application.Services;

/// <summary>
/// Orchestrates the generation of context reports by aggregating data from multiple external sources.
/// </summary>
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

    /// <summary>
    /// Coordinates the retrieval of data from multiple public sources and builds a unified context report.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method employs a "fan-out" pattern to query all external APIs in parallel via <see cref="IContextDataProvider"/>.
    /// </para>
    /// <para>
    /// Key Decisions:
    /// <list type="bullet">
    /// <item>
    /// <strong>Radius Clamping:</strong> The search radius is clamped between 200m and 5000m.
    /// Values > 5km cause excessive load on the Overpass API (OSM) and result in timeouts.
    /// </item>
    /// <item>
    /// <strong>High-Precision Caching:</strong> We resolve the location to coordinates first, then use
    /// 5-decimal precision (~1 meter) for the cache key. This ensures "Damrak 1" and "Damrak 1, Amsterdam"
    /// share the same expensive report generation.
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
    public async Task<ContextReportDto> BuildAsync(ContextReportRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            throw new ValidationException(new[] { "Input is required." });
        }

        // Radius is clamped to prevent excessive load on Overpass/external APIs.
        // Queries > 5km can cause timeouts or massive memory usage in the Overpass client.
        // Minimum 200m ensures we have a meaningful neighborhood scope.
        var normalizedRadius = Math.Clamp(request.RadiusMeters, 200, 5000);

        // 1. Resolve Location First
        // The location resolver has its own cache. We need the resolved coordinates/IDs
        // to build a stable cache key for the expensive report generation.
        var location = await _locationResolver.ResolveAsync(request.Input, cancellationToken);
        if (location is null)
        {
            throw new ValidationException(new[] { "Could not resolve input to a Dutch address." });
        }

        // 2. Check Report Cache using stable location key
        // Key format: context-report:v3:{lat_f5}_{lon_f5}:{radius}
        // Using 5 decimal places (F5) gives precision to ~1 meter.
        // This ensures that variations like "Damrak 1" and "Damrak 1, Amsterdam" hit the same cache
        // if they resolve to the same coordinate, preventing duplicate expensive API calls.
        var latKey = location.Latitude.ToString("F5");
        var lonKey = location.Longitude.ToString("F5");
        var cacheKey = $"context-report:v3:{latKey}_{lonKey}:{normalizedRadius}";

        if (_cache.TryGetValue(cacheKey, out ContextReportDto? cached) && cached is not null)
        {
            return cached;
        }

        // 3. Fetch Data from Provider (Fan-Out)
        // The IContextDataProvider implementation handles the parallel execution of
        // CBS, PDOK, Overpass, and Luchtmeetnet clients.
        var sourceData = await _contextDataProvider.GetSourceDataAsync(location, normalizedRadius, cancellationToken);

        var cbs = sourceData.NeighborhoodStats;
        var crime = sourceData.CrimeStats;
        var amenities = sourceData.AmenityStats;
        var air = sourceData.AirQualitySnapshot;

        var warnings = new List<string>(sourceData.Warnings);

        if (normalizedRadius != request.RadiusMeters)
        {
            warnings.Add($"Radius clamped from {request.RadiusMeters}m to {normalizedRadius}m to respect system limits.");
        }

        // 4. Build normalized metrics for each category (Fan-In)
        // Raw data is converted to uniform ContextMetricDto objects.
        var socialMetrics = SocialMetricBuilder.Build(cbs, warnings);
        var crimeMetrics = CrimeMetricBuilder.Build(crime, warnings);
        var demographicsMetrics = DemographicsMetricBuilder.Build(cbs, warnings);
        var housingMetrics = HousingMetricBuilder.Build(cbs, warnings); // Phase 2
        var mobilityMetrics = MobilityMetricBuilder.Build(cbs, warnings); // Phase 2
        var amenityMetrics = AmenityMetricBuilder.Build(amenities, cbs, warnings); // Phase 2: CBS Proximity
        var environmentMetrics = EnvironmentMetricBuilder.Build(air, warnings);

        // 5. Compute scores
        // The ContextScoreCalculator applies weights to these metrics to produce category scores
        // and a final weighted composite score.
        // We map Application DTOs to Domain Models to invoke the Domain Service.
        List<Valora.Domain.Models.ContextMetricModel> ToDomain(IEnumerable<ContextMetricDto> dtos)
        {
            return dtos.Select(d => new Valora.Domain.Models.ContextMetricModel(
                d.Key, d.Label, d.Value, d.Unit, d.Score, d.Source, d.Note)).ToList();
        }

        var metricsInput = new Valora.Domain.Services.CategoryMetricsInput(
            ToDomain(socialMetrics),
            ToDomain(crimeMetrics),
            ToDomain(demographicsMetrics),
            ToDomain(housingMetrics),
            ToDomain(mobilityMetrics),
            ToDomain(amenityMetrics),
            ToDomain(environmentMetrics));

        var categoryScores = Valora.Domain.Services.ContextScoreCalculator.ComputeCategoryScores(metricsInput);
        var compositeScore = Valora.Domain.Services.ContextScoreCalculator.ComputeCompositeScore(categoryScores);

        var report = new ContextReportDto(
            Location: location,
            SocialMetrics: socialMetrics,
            CrimeMetrics: crimeMetrics,
            DemographicsMetrics: demographicsMetrics,
            HousingMetrics: housingMetrics, // Phase 2
            MobilityMetrics: mobilityMetrics, // Phase 2
            AmenityMetrics: amenityMetrics,
            EnvironmentMetrics: environmentMetrics,
            CompositeScore: Math.Round(compositeScore, 1),
            CategoryScores: categoryScores,
            Sources: sourceData.Sources,
            Warnings: warnings);

        // Cache the result for the configured duration (default: 24h)
        _cache.Set(cacheKey, report, TimeSpan.FromMinutes(_options.ReportCacheMinutes));
        return report;
    }
}
