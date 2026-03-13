using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Common;

namespace Valora.Application.Services;

/// <summary>
/// Provides data from multiple external sources for context reports.
/// </summary>
public sealed class ContextDataProvider : IContextDataProvider
{
    private readonly ICbsNeighborhoodStatsClient _cbsClient;
    private readonly ICbsCrimeStatsClient _crimeClient;
    private readonly IAmenityClient _amenityClient;
    private readonly IAirQualityClient _airQualityClient;
    private readonly ILogger<ContextDataProvider> _logger;

    public ContextDataProvider(
        ICbsNeighborhoodStatsClient cbsClient,
        ICbsCrimeStatsClient crimeClient,
        IAmenityClient amenityClient,
        IAirQualityClient airQualityClient,
        ILogger<ContextDataProvider> logger)
    {
        _cbsClient = cbsClient;
        _crimeClient = crimeClient;
        _amenityClient = amenityClient;
        _airQualityClient = airQualityClient;
        _logger = logger;
    }

    /// <summary>
    /// Fetches data from CBS, PDOK, Overpass, etc., concurrently.
    /// </summary>
    public async Task<ContextSourceData> GetSourceDataAsync(ResolvedLocationDto location, int radiusMeters, CancellationToken cancellationToken)
    {
        var warnings = new ConcurrentBag<string>();

        // Fetch all data sources in parallel (Fan-out)
        // Each task is wrapped in a safe executor that returns null on failure instead of throwing
        var cbsStatsTask = TryGetSourceAsync("CBS", token => _cbsClient.GetStatsAsync(location, token), warnings, cancellationToken);
        var crimeStatsTask = TryGetSourceAsync("CBS Crime", token => _crimeClient.GetStatsAsync(location, token), warnings, cancellationToken);
        var amenitiesStatsTask = TryGetSourceAsync("Overpass", token => _amenityClient.GetAmenitiesAsync(location, radiusMeters, token), warnings, cancellationToken);
        var airQualitySnapshotTask = TryGetSourceAsync("Luchtmeetnet", token => _airQualityClient.GetSnapshotAsync(location, token), warnings, cancellationToken);

        await Task.WhenAll(cbsStatsTask, crimeStatsTask, amenitiesStatsTask, airQualitySnapshotTask);

        var cbsStats = await cbsStatsTask;
        var crimeStats = await crimeStatsTask;
        var amenityStats = await amenitiesStatsTask;
        var airQualitySnapshot = await airQualitySnapshotTask;

        var sources = BuildSourceAttributions(cbsStats, crimeStats, amenityStats, airQualitySnapshot);

        return new ContextSourceData(
            NeighborhoodStats: cbsStats,
            CrimeStats: crimeStats,
            AmenityStats: amenityStats,
            AirQualitySnapshot: airQualitySnapshot,
            Sources: sources,
            Warnings: warnings.ToList());
    }

    /// <summary>
    /// Wraps an external API call in a try-catch block to ensure partial success.
    /// </summary>
    private async Task<T?> TryGetSourceAsync<T>(
        string sourceName,
        Func<CancellationToken, Task<T?>> sourceCall,
        ConcurrentBag<string> warnings,
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
            warnings.Add($"Source {sourceName} unavailable");
            return default;
        }
    }

    private static List<SourceAttributionDto> BuildSourceAttributions(
        NeighborhoodStatsDto? cbsStats,
        CrimeStatsDto? crimeStats,
        AmenityStatsDto? amenityStats,
        AirQualitySnapshotDto? airQualitySnapshot)
    {
        var sources = new List<SourceAttributionDto>
        {
            new("PDOK Locatieserver", "https://api.pdok.nl", "Publiek", DateTimeOffset.UtcNow)
        };

        if (cbsStats is not null)
        {
            sources.Add(new SourceAttributionDto(DataSources.CbsStatLine, "https://opendata.cbs.nl", "Publiek", cbsStats.RetrievedAtUtc));
        }

        if (crimeStats is not null)
        {
            sources.Add(new SourceAttributionDto(DataSources.CbsCrimeStatLine, "https://opendata.cbs.nl", "Publiek", crimeStats.RetrievedAtUtc));
        }

        if (amenityStats is not null)
        {
            sources.Add(new SourceAttributionDto("OpenStreetMap Overpass", "https://overpass-api.de", "ODbL", amenityStats.RetrievedAtUtc));
        }

        if (airQualitySnapshot is not null)
        {
            sources.Add(new SourceAttributionDto(DataSources.Luchtmeetnet, "https://api.luchtmeetnet.nl", "Publiek", airQualitySnapshot.RetrievedAtUtc));
        }

        return sources;
    }
}
