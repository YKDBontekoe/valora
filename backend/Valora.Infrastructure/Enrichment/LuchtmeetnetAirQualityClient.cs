using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
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

    public LuchtmeetnetAirQualityClient(HttpClient httpClient, IMemoryCache cache, IOptions<ContextEnrichmentOptions> options)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
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
            return null;
        }

        using var measurementContent = await measurementResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var measurementDocument = await JsonDocument.ParseAsync(measurementContent, cancellationToken: cancellationToken);

        var first = measurementDocument.RootElement
            .GetProperty("data")
            .EnumerateArray()
            .FirstOrDefault();

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

    private async Task<(string Id, string Name, double DistanceMeters)?> FindNearestStationAsync(
        ResolvedLocationDto location,
        CancellationToken cancellationToken)
    {
        (string Id, string Name, double DistanceMeters)? nearest = null;

        for (var page = 1; page <= 5; page++)
        {
            var url = $"{_options.LuchtmeetnetBaseUrl.TrimEnd('/')}/open_api/stations?page={page}";
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("data", out var stations) || stations.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var station in stations.EnumerateArray())
            {
                if (!station.TryGetProperty("number", out var idElement) || idElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var id = idElement.GetString();
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                var detailUrl = $"{_options.LuchtmeetnetBaseUrl.TrimEnd('/')}/open_api/stations/{id}";
                using var detailResponse = await _httpClient.GetAsync(detailUrl, cancellationToken);
                if (!detailResponse.IsSuccessStatusCode)
                {
                    continue;
                }

                using var detailContent = await detailResponse.Content.ReadAsStreamAsync(cancellationToken);
                using var detailDocument = await JsonDocument.ParseAsync(detailContent, cancellationToken: cancellationToken);

                if (!detailDocument.RootElement.TryGetProperty("data", out var data) ||
                    !data.TryGetProperty("geometry", out var geometry) ||
                    !geometry.TryGetProperty("coordinates", out var coordinates) ||
                    coordinates.ValueKind != JsonValueKind.Array ||
                    coordinates.GetArrayLength() < 2)
                {
                    continue;
                }

                var lon = coordinates[0].GetDouble();
                var lat = coordinates[1].GetDouble();

                var distance = GeoDistance.BetweenMeters(location.Latitude, location.Longitude, lat, lon);
                if (!nearest.HasValue || distance < nearest.Value.DistanceMeters)
                {
                    var stationName = data.TryGetProperty("location", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                        ? nameElement.GetString() ?? id
                        : id;

                    nearest = (id, stationName, distance);
                }
            }
        }

        return nearest;
    }
}
