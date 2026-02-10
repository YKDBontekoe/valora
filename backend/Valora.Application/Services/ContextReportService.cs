using Microsoft.Extensions.Caching.Memory;
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
    private readonly ICbsNeighborhoodStatsClient _cbsClient;
    private readonly ICbsCrimeStatsClient _crimeClient;
    private readonly IDemographicsClient _demographicsClient;
    private readonly IAmenityClient _amenityClient;
    private readonly IAirQualityClient _airQualityClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<ContextReportService> _logger;

    private readonly SocialMetricBuilder _socialBuilder;
    private readonly CrimeMetricBuilder _crimeBuilder;
    private readonly DemographicsMetricBuilder _demographicsBuilder;
    private readonly AmenityMetricBuilder _amenityBuilder;
    private readonly EnvironmentMetricBuilder _environmentBuilder;
    private readonly ScoringCalculator _scoringCalculator;

    public ContextReportService(
        ILocationResolver locationResolver,
        ICbsNeighborhoodStatsClient cbsClient,
        ICbsCrimeStatsClient crimeClient,
        IDemographicsClient demographicsClient,
        IAmenityClient amenityClient,
        IAirQualityClient airQualityClient,
        IMemoryCache cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<ContextReportService> logger,
        SocialMetricBuilder socialBuilder,
        CrimeMetricBuilder crimeBuilder,
        DemographicsMetricBuilder demographicsBuilder,
        AmenityMetricBuilder amenityBuilder,
        EnvironmentMetricBuilder environmentBuilder,
        ScoringCalculator scoringCalculator)
    {
        _locationResolver = locationResolver;
        _cbsClient = cbsClient;
        _crimeClient = crimeClient;
        _demographicsClient = demographicsClient;
        _amenityClient = amenityClient;
        _airQualityClient = airQualityClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
        _socialBuilder = socialBuilder;
        _crimeBuilder = crimeBuilder;
        _demographicsBuilder = demographicsBuilder;
        _amenityBuilder = amenityBuilder;
        _environmentBuilder = environmentBuilder;
        _scoringCalculator = scoringCalculator;
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
            // Return a copy with the correct query and any request-specific warnings
            var warnings = new List<string>(cached.Warnings);
            if (normalizedRadius != request.RadiusMeters)
            {
                warnings.Add($"Radius clamped from {request.RadiusMeters}m to {normalizedRadius}m to respect system limits.");
            }

            return cached with
            {
                Location = cached.Location with { Query = request.Input },
                Warnings = warnings
            };
        }

        var cbsTask = TryGetSourceAsync("CBS", token => _cbsClient.GetStatsAsync(location, token), cancellationToken);
        var crimeTask = TryGetSourceAsync("CBS Crime", token => _crimeClient.GetStatsAsync(location, token), cancellationToken);
        var demographicsTask = TryGetSourceAsync("CBS Demographics", token => _demographicsClient.GetDemographicsAsync(location, token), cancellationToken);
        var amenitiesTask = TryGetSourceAsync("Overpass", token => _amenityClient.GetAmenitiesAsync(location, normalizedRadius, token), cancellationToken);
        var airQualityTask = TryGetSourceAsync("Luchtmeetnet", token => _airQualityClient.GetSnapshotAsync(location, token), cancellationToken);

        await Task.WhenAll(cbsTask, crimeTask, demographicsTask, amenitiesTask, airQualityTask);

        var cbs = await cbsTask;
        var crime = await crimeTask;
        var demographics = await demographicsTask;
        var amenities = await amenitiesTask;
        var air = await airQualityTask;

        // Warnings stored in cache should only be those related to data fetching failures
        var invariantWarnings = new List<string>();

        var socialMetrics = _socialBuilder.Build(cbs, invariantWarnings);
        var crimeMetrics = _crimeBuilder.Build(crime, invariantWarnings);
        var demographicsMetrics = _demographicsBuilder.Build(demographics, invariantWarnings);
        var amenityMetrics = _amenityBuilder.Build(amenities, invariantWarnings);
        var environmentMetrics = _environmentBuilder.Build(air, invariantWarnings);

        var categoryScores = _scoringCalculator.ComputeCategoryScores(socialMetrics, crimeMetrics, demographicsMetrics, amenityMetrics, environmentMetrics);
        var compositeScore = _scoringCalculator.ComputeCompositeScore(categoryScores);

        var sources = BuildSourceAttributions(cbs, crime, demographics, amenities, air);

        var report = new ContextReportDto(
            Location: location,
            SocialMetrics: socialMetrics,
            CrimeMetrics: crimeMetrics,
            DemographicsMetrics: demographicsMetrics,
            AmenityMetrics: amenityMetrics,
            EnvironmentMetrics: environmentMetrics,
            CompositeScore: Math.Round(compositeScore, 1),
            CategoryScores: categoryScores,
            Sources: sources,
            Warnings: invariantWarnings);

        _cache.Set(cacheKey, report, TimeSpan.FromMinutes(_options.ReportCacheMinutes));

        // Prepare the return object which includes request-specific details
        var returnWarnings = new List<string>(invariantWarnings);
        if (normalizedRadius != request.RadiusMeters)
        {
            returnWarnings.Add($"Radius clamped from {request.RadiusMeters}m to {normalizedRadius}m to respect system limits.");
        }

        // Return a copy with the original request input query
        return report with
        {
            Location = location with { Query = request.Input },
            Warnings = returnWarnings
        };
    }

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
        DemographicsDto? demographics,
        AmenityStatsDto? amenities,
        AirQualitySnapshotDto? air)
    {
        var sources = new List<SourceAttributionDto>
        {
            new("PDOK Locatieserver", "https://api.pdok.nl", "Publiek", DateTimeOffset.UtcNow)
        };

        if (cbs is not null)
        {
            sources.Add(new SourceAttributionDto("CBS StatLine 83765NED", "https://opendata.cbs.nl", "Publiek", cbs.RetrievedAtUtc));
        }

        if (crime is not null)
        {
            sources.Add(new SourceAttributionDto("CBS StatLine 47018NED", "https://opendata.cbs.nl", "Publiek", crime.RetrievedAtUtc));
        }

        if (demographics is not null)
        {
            sources.Add(new SourceAttributionDto("CBS Demographics", "https://opendata.cbs.nl", "Publiek", demographics.RetrievedAtUtc));
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
