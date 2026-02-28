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
            // Optimization: Fetch all formulas in one request instead of 4 separate ones
            var measurements = await GetLatestMeasurementsAsync(station.Value.Id, cancellationToken);

            var pm25 = measurements.FirstOrDefault(m => m.Formula == "PM25");
            var pm10 = measurements.FirstOrDefault(m => m.Formula == "PM10");
            var no2 = measurements.FirstOrDefault(m => m.Formula == "NO2");
            var o3 = measurements.FirstOrDefault(m => m.Formula == "O3");

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

    private async Task<List<LuchtmeetnetMeasurement>> GetLatestMeasurementsAsync(string stationId, CancellationToken cancellationToken)
    {
        var encodedStationId = Uri.EscapeDataString(stationId);
        // Omit formula to get all components for the station
        var measurementUrl = $"{_options.LuchtmeetnetBaseUrl.TrimEnd('/')}/open_api/stations/{encodedStationId}/measurements?order_by=timestamp_measured&order_direction=desc&page=1";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<LuchtmeetnetMeasurementResponse>(measurementUrl, cancellationToken);
            return response?.Data ?? new List<LuchtmeetnetMeasurement>();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Luchtmeetnet measurements lookup failed for station {StationId}", stationId);
            return new List<LuchtmeetnetMeasurement>();
        }
    }

    private async Task<(string Id, string Name, double DistanceMeters)?> FindNearestStationAsync(
        ResolvedLocationDto location,
        CancellationToken cancellationToken)
    {
        // Optimization: Cache the entire station coordinates list to avoid hundreds of requests on every cache miss
        var allStations = await GetCachedStationListAsync(cancellationToken);
        if (allStations.Count == 0) return null;

        (string Id, string Name, double Lat, double Lon)? nearest = null;
        double minDistance = double.MaxValue;

        foreach (var station in allStations)
        {
            var distance = GeoDistance.BetweenMeters(location.Latitude, location.Longitude, station.Lat, station.Lon);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = station;
            }
        }

        if (nearest == null) return null;
        return (nearest.Value.Id, nearest.Value.Name, minDistance);
    }

    private async Task<List<(string Id, string Name, double Lat, double Lon)>> GetCachedStationListAsync(CancellationToken cancellationToken)
    {
        const string cacheKey = "lucht:all-stations-metadata";
        if (_cache.TryGetValue(cacheKey, out List<(string Id, string Name, double Lat, double Lon)>? cached))
        {
            return cached!;
        }

        var stations = await DiscoverAllStationsWithCoordinatesAsync(cancellationToken);
        if (stations.Count > 0)
        {
            _cache.Set(cacheKey, stations, TimeSpan.FromHours(24));
        }
        return stations;
    }

    private async Task<List<(string Id, string Name, double Lat, double Lon)>> DiscoverAllStationsWithCoordinatesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Luchtmeetnet station discovery...");
        var stationIds = await FetchAllStationIdsAsync(cancellationToken);
        var results = new List<(string Id, string Name, double Lat, double Lon)>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 5,
            CancellationToken = cancellationToken
        };
        var lockObj = new object();

        await Parallel.ForEachAsync(stationIds, parallelOptions, async (stationId, token) =>
        {
            var detail = await GetStationDetailAsync(stationId, token);
            if (detail is null) return;

            lock (lockObj)
            {
                results.Add((stationId, detail.Value.Name, detail.Value.Latitude, detail.Value.Longitude));
            }
        });

        _logger.LogInformation("Discovered {Count} Luchtmeetnet stations with coordinates.", results.Count);
        return results;
    }

    private async Task<HashSet<string>> FetchAllStationIdsAsync(CancellationToken cancellationToken)
    {
        var stationIds = new HashSet<string>();
        for (var page = 1; page <= 15; page++)
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

    private async Task<(string Name, double Latitude, double Longitude)?> GetStationDetailAsync(string stationId, CancellationToken cancellationToken)
    {
        var cacheKey = $"lucht:station-detail:{stationId}";
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
            _cache.Set(cacheKey, result, TimeSpan.FromHours(48));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch details for station {StationId}", stationId);
            return null;
        }
    }
}
