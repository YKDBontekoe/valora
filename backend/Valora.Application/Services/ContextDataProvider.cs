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
    /// <remarks>
    /// <para>
    /// <strong>Architecture Pattern: Fan-Out</strong><br/>
    /// This method implements the data fetching phase of the Context Report generation. It issues parallel requests to multiple external sources to minimize the overall response time.
    /// </para>
    /// <para>
    /// <strong>Data Flow:</strong>
    /// <code>
    /// ```mermaid
    /// graph TD
    ///     A[GetSourceDataAsync] -->|Parallel Task| B(CBS Stats API)
    ///     A -->|Parallel Task| C(CBS Crime API)
    ///     A -->|Parallel Task| D(Overpass API)
    ///     A -->|Parallel Task| E(Luchtmeetnet API)
    ///
    ///     B --> F{Task.WhenAll}
    ///     C --> F
    ///     D --> F
    ///     E --> F
    ///
    ///     F --> G[ContextSourceData]
    /// ```
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Partial Failure Tolerance:</strong>
    /// Each external API call is wrapped in <see cref="TryGetSourceAsync{T}"/>. If a non-critical source (e.g., Luchtmeetnet) fails or times out, the overall report generation does not fail. Instead, the task returns <c>null</c> for that specific source, a warning is logged and appended to the final report, and the system degrades gracefully. This is critical for maintaining high availability.
    /// </para>
    /// </remarks>
    public async Task<ContextSourceData> GetSourceDataAsync(ResolvedLocationDto location, int radiusMeters, CancellationToken cancellationToken)
    {
        var warnings = new ConcurrentBag<string>();

        // Fetch all data sources in parallel (Fan-out)
        // Each task is wrapped in a safe executor that returns null on failure instead of throwing
        var cbsTask = TryGetSourceAsync("CBS", token => _cbsClient.GetStatsAsync(location, token), warnings, cancellationToken);
        var crimeTask = TryGetSourceAsync("CBS Crime", token => _crimeClient.GetStatsAsync(location, token), warnings, cancellationToken);
        var amenitiesTask = TryGetSourceAsync("Overpass", token => _amenityClient.GetAmenitiesAsync(location, radiusMeters, token), warnings, cancellationToken);
        var airQualityTask = TryGetSourceAsync("Luchtmeetnet", token => _airQualityClient.GetSnapshotAsync(location, token), warnings, cancellationToken);

        await Task.WhenAll(cbsTask, crimeTask, amenitiesTask, airQualityTask);

        var cbs = await cbsTask;
        var crime = await crimeTask;
        var amenities = await amenitiesTask;
        var air = await airQualityTask;

        var sources = BuildSourceAttributions(cbs, crime, amenities, air);

        return new ContextSourceData(
            NeighborhoodStats: cbs,
            CrimeStats: crime,
            AmenityStats: amenities,
            AirQualitySnapshot: air,
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
            sources.Add(new SourceAttributionDto(DataSources.CbsStatLine, "https://opendata.cbs.nl", "Publiek", cbs.RetrievedAtUtc));
        }

        if (crime is not null)
        {
            sources.Add(new SourceAttributionDto(DataSources.CbsCrimeStatLine, "https://opendata.cbs.nl", "Publiek", crime.RetrievedAtUtc));
        }

        if (amenities is not null)
        {
            sources.Add(new SourceAttributionDto("OpenStreetMap Overpass", "https://overpass-api.de", "ODbL", amenities.RetrievedAtUtc));
        }

        if (air is not null)
        {
            sources.Add(new SourceAttributionDto(DataSources.Luchtmeetnet, "https://api.luchtmeetnet.nl", "Publiek", air.RetrievedAtUtc));
        }

        return sources;
    }
}
