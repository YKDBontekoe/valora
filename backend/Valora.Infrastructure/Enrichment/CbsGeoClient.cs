using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;
using Valora.Application.Enrichment;

namespace Valora.Infrastructure.Enrichment;

public sealed class CbsGeoClient : ICbsGeoClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ICbsNeighborhoodStatsClient _statsClient;
    private readonly ICbsCrimeStatsClient _crimeClient;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<CbsGeoClient> _logger;

    public CbsGeoClient(
        HttpClient httpClient,
        IMemoryCache cache,
        ICbsNeighborhoodStatsClient statsClient,
        ICbsCrimeStatsClient crimeClient,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<CbsGeoClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _statsClient = statsClient;
        _crimeClient = crimeClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<MapOverlayDto>> GetNeighborhoodOverlaysAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        MapOverlayMetric metric,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"cbs-geo:{minLat:F4}:{minLon:F4}:{maxLat:F4}:{maxLon:F4}:{metric}";
        if (_cache.TryGetValue(cacheKey, out List<MapOverlayDto>? cached))
        {
            return cached!;
        }

        var bbox = $"{minLat.ToString(CultureInfo.InvariantCulture)},{minLon.ToString(CultureInfo.InvariantCulture)},{maxLat.ToString(CultureInfo.InvariantCulture)},{maxLon.ToString(CultureInfo.InvariantCulture)}";
        var url = $"https://service.pdok.nl/cbs/wijkenenbuurten/2023/wfs/v1_0?service=WFS&version=2.0.0&request=GetFeature&typeName=buurten&outputFormat=json&srsName=EPSG:4326&bbox={bbox},urn:ogc:def:crs:EPSG::4326";

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PDOK WFS failed with status {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<MapOverlayDto>();
        foreach (var feature in features.EnumerateArray())
        {
            if (!feature.TryGetProperty("properties", out var props)) continue;

            if (!props.TryGetProperty("buurtcode", out var codeElem) || codeElem.ValueKind != JsonValueKind.String)
            {
                continue;
            }
            var neighborhoodCode = codeElem.GetString();
            if (string.IsNullOrEmpty(neighborhoodCode)) continue;

            var neighborhoodName = "Unknown";
            if (props.TryGetProperty("buurtnaam", out var nameElem) && nameElem.ValueKind == JsonValueKind.String)
            {
                neighborhoodName = nameElem.GetString() ?? "Unknown";
            }

            var (value, display) = await GetMetricValueAsync(neighborhoodCode, metric, cancellationToken);

            if (value.HasValue)
            {
                results.Add(new MapOverlayDto(
                    neighborhoodCode,
                    neighborhoodName,
                    metric.ToString(),
                    value.Value,
                    display,
                    feature.Clone()));
            }
        }

        _cache.Set(cacheKey, results, TimeSpan.FromHours(24));
        return results;
    }

    private async Task<(double? Value, string Display)> GetMetricValueAsync(string code, MapOverlayMetric metric, CancellationToken ct)
    {
        var loc = new Application.DTOs.ResolvedLocationDto(
            "", "", 0, 0, null, null, null, null, null, null, code, null, null);

        switch (metric)
        {
            case MapOverlayMetric.CrimeRate:
                var crime = await _crimeClient.GetStatsAsync(loc, ct);
                return (crime?.TotalCrimesPer1000, $"{(crime?.TotalCrimesPer1000?.ToString() ?? "N/A")} / 1000");

            case MapOverlayMetric.PopulationDensity:
                var stats = await _statsClient.GetStatsAsync(loc, ct);
                return (stats?.PopulationDensity, $"{(stats?.PopulationDensity?.ToString() ?? "N/A")} / km²");

            case MapOverlayMetric.AverageWoz:
                var statsWoz = await _statsClient.GetStatsAsync(loc, ct);
                return (statsWoz?.AverageWozValueKeur, $"€ {(statsWoz?.AverageWozValueKeur * 1000)?.ToString("N0") ?? "N/A"}");

            default:
                return (null, "N/A");
        }
    }
}
