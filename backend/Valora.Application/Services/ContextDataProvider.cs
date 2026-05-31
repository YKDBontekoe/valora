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
    /// <strong>Architecture Pattern: Fan-Out / Fan-In</strong><br/>
    /// This method is the core "Fan-Out" engine. It issues multiple parallel requests to external data providers (CBS, OSM, Luchtmeetnet)
    /// to fetch context data simultaneously using <c>Task.WhenAll</c>. This ensures the total wait time is bounded by the slowest API,
    /// rather than the sum of all APIs.
    /// </para>
    /// <para>
    /// <strong>Partial Failure Handling:</strong><br/>
    /// Each external API call is wrapped in <see cref="TryGetSourceAsync{T}"/>. If an external dependency is down,
    /// times out, or returns a 500, it is caught and handled gracefully. The method returns whatever data *did* succeed,
    /// appending a warning message. This prevents a failure in a secondary source (e.g., Luchtmeetnet) from failing
    /// the entire user request.
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
    /// <remarks>
    /// <para>
    /// Essential for the Fan-Out pattern. By catching generic exceptions and returning <c>default</c> (null),
    /// we prevent a single failing <c>Task</c> from bringing down the entire <c>Task.WhenAll</c> operation in <see cref="GetSourceDataAsync"/>.
    /// The failure is recorded in the concurrent <c>warnings</c> bag to inform the client that the report is degraded.
    /// </para>
    /// </remarks>
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
