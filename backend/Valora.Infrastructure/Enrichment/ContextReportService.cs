using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Application.Enrichment.Builders;

namespace Valora.Infrastructure.Enrichment;

/// <summary>
/// Orchestrates the generation of context reports by aggregating data from multiple external sources.
/// </summary>
public sealed class ContextReportService : IContextReportService
{
    private readonly ILocationResolver _locationResolver;
    private readonly ICbsNeighborhoodStatsClient _cbsClient;
    private readonly ICbsCrimeStatsClient _crimeClient;
    private readonly IAmenityClient _amenityClient;
    private readonly IAirQualityClient _airQualityClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<ContextReportService> _logger;

    public ContextReportService(
        ILocationResolver locationResolver,
        ICbsNeighborhoodStatsClient cbsClient,
        ICbsCrimeStatsClient crimeClient,
        IAmenityClient amenityClient,
        IAirQualityClient airQualityClient,
        IMemoryCache cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<ContextReportService> logger)
    {
        _locationResolver = locationResolver;
        _cbsClient = cbsClient;
        _crimeClient = crimeClient;
        _amenityClient = amenityClient;
        _airQualityClient = airQualityClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Coordinates the retrieval of data from multiple public sources and builds a unified context report.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method employs a "fan-out" pattern to query all external APIs in parallel.
    /// It is designed for resilience: if a non-critical source fails (e.g., air quality),
    /// the method catches the exception via <see cref="TryGetSourceAsync{T}"/> and returns a partial report
    /// with a warning, rather than failing the entire request.
    /// </para>
    /// <para>
    /// The process involves:
    /// 1. Resolving the input to a standardized Dutch address/location.
    /// 2. Checking the cache for an existing report.
    /// 3. Fetching data from CBS, PDOK, Overpass, etc., concurrently.
    /// 4. Normalizing raw data into 0-100 scores using heuristics.
    /// 5. Aggregating scores into categories and a final composite score.
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

        // Fetch all data sources in parallel (Fan-out)
        // Each task is wrapped in a safe executor that returns null on failure instead of throwing
        var cbsTask = TryGetSourceAsync("CBS", token => _cbsClient.GetStatsAsync(location, token), cancellationToken);
        var crimeTask = TryGetSourceAsync("CBS Crime", token => _crimeClient.GetStatsAsync(location, token), cancellationToken);
        var amenitiesTask = TryGetSourceAsync("Overpass", token => _amenityClient.GetAmenitiesAsync(location, normalizedRadius, token), cancellationToken);
        var airQualityTask = TryGetSourceAsync("Luchtmeetnet", token => _airQualityClient.GetSnapshotAsync(location, token), cancellationToken);

        await Task.WhenAll(cbsTask, crimeTask, amenitiesTask, airQualityTask);

        var cbs = await cbsTask;
        var crime = await crimeTask;
        var amenities = await amenitiesTask;
        var air = await airQualityTask;

        var warnings = new List<string>();

        if (normalizedRadius != request.RadiusMeters)
        {
            warnings.Add($"Radius clamped from {request.RadiusMeters}m to {normalizedRadius}m to respect system limits.");
        }

        // Build normalized metrics for each category (Fan-in)
        var socialMetrics = SocialMetricBuilder.Build(cbs, warnings);
        var crimeMetrics = CrimeMetricBuilder.Build(crime, warnings);
        var demographicsMetrics = DemographicsMetricBuilder.Build(cbs, warnings);
        var housingMetrics = HousingMetricBuilder.Build(cbs, warnings); // Phase 2
        var mobilityMetrics = MobilityMetricBuilder.Build(cbs, warnings); // Phase 2
        var amenityMetrics = AmenityMetricBuilder.Build(amenities, cbs, warnings); // Phase 2: CBS Proximity
        var environmentMetrics = EnvironmentMetricBuilder.Build(air, warnings);

        // Compute category scores for radar chart
        var categoryScores = ContextScoreCalculator.ComputeCategoryScores(socialMetrics, crimeMetrics, demographicsMetrics, housingMetrics, mobilityMetrics, amenityMetrics, environmentMetrics);
        var compositeScore = ContextScoreCalculator.ComputeCompositeScore(categoryScores);

        var sources = BuildSourceAttributions(cbs, crime, amenities, air);

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
            Sources: sources,
            Warnings: warnings);

        _cache.Set(cacheKey, report, TimeSpan.FromMinutes(_options.ReportCacheMinutes));
        return report;
    }

    /// <summary>
    /// Wraps an external API call in a try-catch block to ensure partial success.
    /// </summary>
    /// <remarks>
    /// If an exception occurs (other than cancellation), it is logged as an error,
    /// and the method returns <c>default</c> (null), allowing the report builder to continue
    /// without that specific data source.
    /// </remarks>
    private async Task<T?> TryGetSourceAsync<T>(
        string sourceName,
        Func<CancellationToken, Task<T?>> sourceCall,
        CancellationToken cancellationToken)
    {
        try
        {
            return await sourceCall(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Context source {SourceName} failed; report will continue with partial data", sourceName);
            return default;
        }
    }


    private static List<SourceAttributionDto> BuildSourceAttributions(
        NeighborhoodStatsDto? cbs,
        CrimeStatsDto? crime,
        AmenityStatsDto? amenities,
        AirQualitySnapshotDto? air)
    {
        var sources = new List<SourceAttributionDto>
        {
            new("PDOK Locatieserver", "https://api.pdok.nl", "Publiek", DateTimeOffset.UtcNow)
        };

        if (cbs is not null)
        {
            sources.Add(new SourceAttributionDto("CBS StatLine 85618NED", "https://opendata.cbs.nl", "Publiek", cbs.RetrievedAtUtc));
        }

        if (crime is not null)
        {
            sources.Add(new SourceAttributionDto("CBS StatLine 47018NED", "https://opendata.cbs.nl", "Publiek", crime.RetrievedAtUtc));
        }

        if (amenities is not null)
        {
            sources.Add(new SourceAttributionDto("OpenStreetMap Overpass", "https://overpass-api.de", "ODbL", amenities.RetrievedAtUtc));
        }

        if (air is not null)
        {
            sources.Add(new SourceAttributionDto("Luchtmeetnet", "https://api.luchtmeetnet.nl", "Publiek", air.RetrievedAtUtc));
        }

        return sources;
    }


}
