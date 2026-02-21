using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Constants;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Application.Enrichment.Builders;
using Valora.Domain.Models;
using Valora.Domain.Services;

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
    /// <strong>Architecture Pattern: Fan-Out / Fan-In</strong><br/>
    /// This method acts as the orchestrator for the "Fan-Out" pattern. It does NOT query databases sequentially.
    /// Instead, it delegates the parallel fetching of data to <see cref="IContextDataProvider"/> (Fan-Out),
    /// waits for all tasks to complete, and then aggregates the results (Fan-In) into a single report.
    /// This reduces total latency to the duration of the slowest single source (plus overhead), rather than the sum of all sources.
    /// </para>
    /// <para>
    /// <strong>Why Real-Time?</strong><br/>
    /// We do not store pre-computed reports for every address in the Netherlands because:
    /// <list type="bullet">
    /// <item><strong>Data Volume:</strong> Storing millions of addresses with constantly changing public data is inefficient.</item>
    /// <item><strong>Freshness:</strong> Public data (e.g., Air Quality) changes frequently. Real-time fetching ensures accuracy.</item>
    /// </list>
    /// Instead, we generate them on-demand and cache them aggressively (Read-Through Cache).
    /// </para>
    /// <para>
    /// Key Decisions:
    /// <list type="bullet">
    /// <item>
    /// <strong>Radius Clamping:</strong> The search radius is clamped between 200m and 5000m.
    /// Values > 5km cause excessive load on the Overpass API (OSM) and result in timeouts or HTTP 429s.
    /// </item>
    /// <item>
    /// <strong>High-Precision Caching:</strong> We resolve the location to coordinates first, then use
    /// 5-decimal precision (~1 meter) for the cache key. This ensures "Damrak 1" and "Damrak 1, Amsterdam"
    /// resolve to the same coordinates and share the same expensive report generation result.
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
        var normalizedRadius = Math.Clamp(request.RadiusMeters, ReportConstants.MinRadiusMeters, ReportConstants.MaxRadiusMeters);

        // 1. Resolve Location First
        var location = await _locationResolver.ResolveAsync(request.Input, cancellationToken);
        if (location is null)
        {
            throw new ValidationException(new[] { "Could not resolve input to a Dutch address." });
        }

        // 2. Check Report Cache using stable location key
        var cacheKey = GetCacheKey(location, normalizedRadius);

        if (_cache.TryGetValue(cacheKey, out ContextReportDto? cached) && cached is not null)
        {
            return cached;
        }

        // 3. Fetch Data from Provider (Fan-Out)
        var sourceData = await _contextDataProvider.GetSourceDataAsync(location, normalizedRadius, cancellationToken);
        var warnings = new List<string>(sourceData.Warnings);

        if (normalizedRadius != request.RadiusMeters)
        {
            warnings.Add($"Radius clamped from {request.RadiusMeters}m to {normalizedRadius}m to respect system limits.");
        }

        // 4. Build normalized metrics and compute scores (Fan-In)
        var report = BuildReport(location, sourceData, warnings);

        // Cache the result for the configured duration (default: 24h)
        _cache.Set(cacheKey, report, TimeSpan.FromMinutes(_options.ReportCacheMinutes));
        return report;
    }

    private static string GetCacheKey(ResolvedLocationDto location, int radius)
    {
        // Key format: context-report:v3:{lat_f5}_{lon_f5}:{radius}
        // Design Decision: 5 decimal places (F5) gives precision to approx 1.1 meters.
        // This is precise enough to distinguish buildings but coarse enough to handle minor floating point drift.
        var latKey = location.Latitude.ToString("F5", CultureInfo.InvariantCulture);
        var lonKey = location.Longitude.ToString("F5", CultureInfo.InvariantCulture);
        return $"{ReportConstants.CacheKeyPrefix}:{latKey}_{lonKey}:{radius}";
    }

    /// <summary>
    /// Aggregates raw data into a scored report (The "Fan-In" phase).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Normalization Strategy:</strong><br/>
    /// Raw values (e.g., "45 crimes per 1000 residents") are meaningless to end-users.
    /// We use specific "Builders" (e.g., <see cref="SocialMetricBuilder"/>) to convert these raw numbers
    /// into a normalized 0-100 score based on national averages and heuristics.
    /// </para>
    /// <para>
    /// <strong>Separation of Concerns:</strong><br/>
    /// This method delegates the specific scoring logic to domain services (<see cref="ContextScoreCalculator"/>)
    /// to keep the Application layer focused on orchestration, not business rules.
    /// </para>
    /// </remarks>
    private static ContextReportDto BuildReport(
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
