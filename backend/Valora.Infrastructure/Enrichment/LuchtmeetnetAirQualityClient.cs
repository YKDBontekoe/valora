using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;

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

        var measurementUrl =
            $"{_options.LuchtmeetnetBaseUrl.TrimEnd('/')}/open_api/stations/{station.Value.Id}/measurements?formula=PM25&order_by=timestamp_measured&order_direction=desc&page=1";

        using var measurementResponse = await _httpClient.GetAsync(measurementUrl, cancellationToken);
        if (!measurementResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Luchtmeetnet measurement lookup failed for station {StationId} with status {StatusCode}",
                station.Value.Id,
                measurementResponse.StatusCode);
            return null;
        }

        JsonDocument measurementDocument;
        try
        {
            using var measurementContent = await measurementResponse.Content.ReadAsStreamAsync(cancellationToken);
            measurementDocument = await JsonDocument.ParseAsync(measurementContent, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Luchtmeetnet measurement lookup returned invalid JSON for station {StationId}", station.Value.Id);
            return null;
        }

        using (measurementDocument)
        {
            if (!measurementDocument.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Luchtmeetnet measurement lookup did not include a data array for station {StationId}", station.Value.Id);
                return null;
            }

            var first = data.EnumerateArray().FirstOrDefault();

            double? pm25 = null;
            DateTimeOffset? measuredAt = null;

            if (first.ValueKind == JsonValueKind.Object)
            {
                if (first.TryGetProperty("value", out var valueElement) && valueElement.TryGetDouble(out var parsedValue))
                {
                    pm25 = parsedValue;
                }

                if (first.TryGetProperty("timestamp_measured", out var tsElement) &&
                    tsElement.ValueKind == JsonValueKind.String &&
                    DateTimeOffset.TryParse(tsElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
                {
                    measuredAt = timestamp;
                }
            }

            var snapshot = new AirQualitySnapshotDto(
                StationId: station.Value.Id,
                StationName: station.Value.Name,
                StationDistanceMeters: station.Value.DistanceMeters,
                Pm25: pm25,
                MeasuredAtUtc: measuredAt,
                RetrievedAtUtc: DateTimeOffset.UtcNow);

            _cache.Set(cacheKey, snapshot, TimeSpan.FromMinutes(_options.AirQualityCacheMinutes));
            return snapshot;
        }
    }

    private async Task<(string Id, string Name, double DistanceMeters)?> FindNearestStationAsync(
        ResolvedLocationDto location,
        CancellationToken cancellationToken)
    {
        // Phase 1: Get all station IDs (lightweight list)
        var stationIds = new HashSet<string>();
        
        // Loop through pages to get all station IDs
        // Pagination seems to go up to ~5-6 pages currently
        for (var page = 1; page <= 10; page++)
        {
            var url = $"{_options.LuchtmeetnetBaseUrl.TrimEnd('/')}/open_api/stations?page={page}";
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Luchtmeetnet station list lookup failed for page {Page} with status {StatusCode}", page, response.StatusCode);
                continue;
            }

            JsonDocument document;
            try
            {
                using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
                document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Luchtmeetnet station list returned invalid JSON for page {Page}", page);
                continue;
            }

            using (document)
            {
                if (!document.RootElement.TryGetProperty("data", out var stations) || stations.ValueKind != JsonValueKind.Array)
                {
                    _logger.LogWarning("Luchtmeetnet station list did not include a data array for page {Page}", page);
                    break; // Likely end of pages or bad response
                }

                if (stations.GetArrayLength() == 0)
                {
                    break; // End of pages
                }

                foreach (var station in stations.EnumerateArray())
                {
                    if (station.TryGetProperty("number", out var idElement) && idElement.ValueKind == JsonValueKind.String)
                    {
                        var id = idElement.GetString();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            stationIds.Add(id);
                        }
                    }
                }
                
                // Check if we reached the last page
                if (document.RootElement.TryGetProperty("pagination", out var pagination) && 
                    pagination.TryGetProperty("last_page", out var lastPageElement) &&
                    lastPageElement.TryGetInt32(out var lastPage) &&
                    page >= lastPage)
                {
                    break;
                }
            }
        }

        // Phase 2: Fetch details for each station to get coordinates (Parallel with limits)
        (string Id, string Name, double DistanceMeters)? nearest = null;
        var parallelOptions = new ParallelOptions 
        { 
            MaxDegreeOfParallelism = 5,
            CancellationToken = cancellationToken 
        };

        // We use a lock to safely update the 'nearest' variable from parallel tasks
        var lockObj = new object();

        await Parallel.ForEachAsync(stationIds, parallelOptions, async (id, token) =>
        {
            var detail = await GetStationDetailAsync(id, token);
            if (detail is null) return;

            var (name, lat, lon) = detail.Value;
            var distance = GeoDistance.BetweenMeters(location.Latitude, location.Longitude, lat, lon);

            lock (lockObj)
            {
                if (!nearest.HasValue || distance < nearest.Value.DistanceMeters)
                {
                    nearest = (id, name, distance);
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
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("data", out var data)) return null;

            if (!TryGetStationCoordinates(data, out var lat, out var lon)) return null;

            var name = data.TryGetProperty("location", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                ? nameElement.GetString() ?? stationId
                : stationId;

            var result = (name, lat, lon);
            
            // Cache station metadata for 24 hours since coordinates rarely change
            _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch details for station {StationId}", stationId);
            return null;
        }
    }

    private static bool TryGetStationCoordinates(JsonElement station, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;

        if (!station.TryGetProperty("geometry", out var geometry) ||
            !geometry.TryGetProperty("coordinates", out var coordinates) ||
            coordinates.ValueKind != JsonValueKind.Array ||
            coordinates.GetArrayLength() < 2)
        {
            return false;
        }

        var lonElement = coordinates[0];
        var latElement = coordinates[1];
        if (!lonElement.TryGetDouble(out longitude) || !latElement.TryGetDouble(out latitude))
        {
            return false;
        }

        return true;
    }
}
