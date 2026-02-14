using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Domain.Common;

namespace Valora.Infrastructure.Enrichment;

public sealed class LuchtmeetnetAirQualityClient : IAirQualityClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<LuchtmeetnetAirQualityClient> _logger;

    public LuchtmeetnetAirQualityClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<LuchtmeetnetAirQualityClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AirQualitySnapshotDto?> GetSnapshotAsync(ResolvedLocationDto location, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"lucht:{location.Latitude:F4}:{location.Longitude:F4}";
        if (_cache.TryGetValue(cacheKey, out AirQualitySnapshotDto? cached))
        {
            return cached;
        }

        var station = await FindNearestStationAsync(location, cancellationToken);
        if (station is null)
        {
            return null;
        }

        try
        {
            var pm25Task = GetLatestMeasurementAsync(station.Value.Id, "PM25", cancellationToken);
            var pm10Task = GetLatestMeasurementAsync(station.Value.Id, "PM10", cancellationToken);
            var no2Task = GetLatestMeasurementAsync(station.Value.Id, "NO2", cancellationToken);
            var o3Task = GetLatestMeasurementAsync(station.Value.Id, "O3", cancellationToken);
            await Task.WhenAll(pm25Task, pm10Task, no2Task, o3Task);

            var pm25 = await pm25Task;
            var pm10 = await pm10Task;
            var no2 = await no2Task;
            var o3 = await o3Task;

            if (pm25 is null && pm10 is null && no2 is null && o3 is null)
            {
                _logger.LogWarning("Luchtmeetnet measurement lookup did not include supported formulas for station {StationId}", station.Value.Id);
                return null;
            }

            var latest = pm25 ?? pm10 ?? no2 ?? o3;
            DateTimeOffset? measuredAt = null;
            if (latest is not null && DateTimeOffset.TryParse(latest.TimestampMeasured, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
            {
                measuredAt = timestamp;
            }

            var snapshot = new AirQualitySnapshotDto(
                StationId: station.Value.Id,
                StationName: station.Value.Name,
                StationDistanceMeters: station.Value.DistanceMeters,
                Pm25: pm25?.Value,
                MeasuredAtUtc: measuredAt,
                RetrievedAtUtc: DateTimeOffset.UtcNow,
                Pm10: pm10?.Value,
                No2: no2?.Value,
                O3: o3?.Value);

            _cache.Set(cacheKey, snapshot, TimeSpan.FromMinutes(_options.AirQualityCacheMinutes));
            return snapshot;
        }
        catch (Exception ex)
        {
             _logger.LogWarning(ex, "Luchtmeetnet measurement lookup failed for station {StationId}", station.Value.Id);
             return null;
        }
    }

    private async Task<LuchtmeetnetMeasurement?> GetLatestMeasurementAsync(string stationId, string formula, CancellationToken cancellationToken)
    {
        var encodedStationId = Uri.EscapeDataString(stationId);
        var encodedFormula = Uri.EscapeDataString(formula);
        var measurementUrl = $"{_options.LuchtmeetnetBaseUrl.TrimEnd('/')}/open_api/stations/{encodedStationId}/measurements?formula={encodedFormula}&order_by=timestamp_measured&order_direction=desc&page=1";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<LuchtmeetnetMeasurementResponse>(measurementUrl, cancellationToken);
            return response?.Data?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Luchtmeetnet formula lookup failed for station {StationId}, formula {Formula}", stationId, formula);
            return null;
        }
    }

    /// <summary>
    /// Implements a "Two-Phase Lookup" to find the nearest air quality station.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why Two Phases?</b>
    /// The Luchtmeetnet API does not support geospatial queries (e.g., "find stations near X,Y").
    /// It only lists stations with basic metadata.
    /// </para>
    /// <para>
    /// <b>Phase 1:</b> Fetch all available station IDs by iterating through pagination pages.
    /// </para>
    /// <para>
    /// <b>Phase 2:</b> For each ID, fetch the station details (coordinates) to calculate the distance.
    /// This is done in parallel using <see cref="Parallel.ForEachAsync{T}"/>.
    /// </para>
    /// <para>
    /// <b>Optimization:</b> Station details are heavily cached (24h) since locations don't move.
    /// This makes the seemingly expensive "fetch all details" operation fast after the first run.
    /// </para>
    /// </remarks>
    private async Task<(string Id, string Name, double DistanceMeters)?> FindNearestStationAsync(
        ResolvedLocationDto location,
        CancellationToken cancellationToken)
    {
        // Phase 1: Get all IDs
        var stationIds = await FetchAllStationIdsAsync(cancellationToken);
        if (stationIds.Count == 0) return null;

        // Phase 2: Parallel fetch details + distance calculation
        return await FindNearestFromIdsAsync(stationIds, location, cancellationToken);
    }

    private async Task<HashSet<string>> FetchAllStationIdsAsync(CancellationToken cancellationToken)
    {
        var stationIds = new HashSet<string>();
        // API usually has < 10 pages of stations (approx 100-150 stations total in NL)
        for (var page = 1; page <= 10; page++)
        {
            var url = $"{_options.LuchtmeetnetBaseUrl.TrimEnd('/')}/open_api/stations?page={page}";
            try
            {
                var response = await _httpClient.GetFromJsonAsync<LuchtmeetnetStationListResponse>(url, cancellationToken);

                if (response?.Data is not null)
                {
                    foreach (var station in response.Data)
                    {
                        if (!string.IsNullOrWhiteSpace(station.Id))
                        {
                            stationIds.Add(station.Id);
                        }
                    }
                }

                if (response?.Pagination is not null && page >= response.Pagination.LastPage)
                {
                    break;
                }
                
                if (response?.Data is null || response.Data.Count == 0)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Luchtmeetnet station list lookup failed for page {Page}", page);
                continue;
            }
        }
        return stationIds;
    }

    private async Task<(string Id, string Name, double DistanceMeters)?> FindNearestFromIdsAsync(
        HashSet<string> stationIds,
        ResolvedLocationDto location,
        CancellationToken cancellationToken)
    {
        (string Id, string Name, double DistanceMeters)? nearest = null;
        var parallelOptions = new ParallelOptions 
        { 
            MaxDegreeOfParallelism = 5,
            CancellationToken = cancellationToken 
        };
        var lockObj = new object();

        await Parallel.ForEachAsync(stationIds, parallelOptions, async (stationId, token) =>
        {
            // Fetch detail to get coordinates
            var detail = await GetStationDetailAsync(stationId, token);
            if (detail is null) return;

            var (name, lat, lon) = detail.Value;
            var distance = GeoDistance.BetweenMeters(location.Latitude, location.Longitude, lat, lon);

            lock (lockObj)
            {
                if (!nearest.HasValue || distance < nearest.Value.DistanceMeters)
                {
                    nearest = (stationId, name, distance);
                }
            }
        });

        return nearest;
    }

    private async Task<(string Name, double Latitude, double Longitude)?> GetStationDetailAsync(string stationId, CancellationToken cancellationToken)
    {
        var cacheKey = $"lucht:station:{stationId}";
        if (_cache.TryGetValue(cacheKey, out (string Name, double Latitude, double Longitude) cached))
        {
            return cached;
        }

        var encodedId = Uri.EscapeDataString(stationId);
        var url = $"{_options.LuchtmeetnetBaseUrl.TrimEnd('/')}/open_api/stations/{encodedId}";

        try 
        {
            var response = await _httpClient.GetFromJsonAsync<LuchtmeetnetStationDetailResponse>(url, cancellationToken);
            if (response?.Data?.Geometry?.Coordinates is null || response.Data.Geometry.Coordinates.Length < 2)
            {
                return null;
            }

            var lon = response.Data.Geometry.Coordinates[0];
            var lat = response.Data.Geometry.Coordinates[1];
            var name = !string.IsNullOrWhiteSpace(response.Data.Name) ? response.Data.Name : stationId;

            var result = (name, lat, lon);
            _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch details for station {StationId}", stationId);
            return null;
        }
    }
}
