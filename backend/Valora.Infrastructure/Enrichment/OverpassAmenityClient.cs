using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.DTOs.Map;
using Valora.Application.Enrichment;

namespace Valora.Infrastructure.Enrichment;

public sealed class OverpassAmenityClient : IAmenityClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<OverpassAmenityClient> _logger;

    public OverpassAmenityClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<OverpassAmenityClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AmenityStatsDto?> GetAmenitiesAsync(ResolvedLocationDto location, int radiusMeters, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"overpass:{location.Latitude:F5}:{location.Longitude:F5}:{radiusMeters}";
        if (_cache.TryGetValue(cacheKey, out AmenityStatsDto? cached))
        {
            return cached;
        }

        var query = OverpassQueryBuilder.BuildAmenityQuery(location.Latitude, location.Longitude, radiusMeters);

        var result = await FetchAndProcessAsync(
            query,
            elements => OverpassResponseParser.ParseAmenityStats(elements, location),
            cancellationToken);

        if (result != null)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.AmenitiesCacheMinutes));
        }
        return result;
    }

    public async Task<List<MapAmenityDto>> GetAmenitiesInBboxAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        List<string>? types = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"overpass-bbox:{minLat:F4}:{minLon:F4}:{maxLat:F4}:{maxLon:F4}:{string.Join(",", types ?? [])}";
        if (_cache.TryGetValue(cacheKey, out List<MapAmenityDto>? cached))
        {
            return cached!;
        }

        var query = OverpassQueryBuilder.BuildBboxQuery(minLat, minLon, maxLat, maxLon, types);
        var results = await FetchAndProcessAsync(
            query,
            elements => OverpassResponseParser.ParseMapAmenities(elements),
            cancellationToken);

        results ??= [];
        _cache.Set(cacheKey, results, TimeSpan.FromMinutes(_options.AmenitiesCacheMinutes));
        return results;
    }

    /// <summary>
    /// Executes a raw Overpass QL query and processes the result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Why POST with x-www-form-urlencoded?</strong><br/>
    /// Overpass queries can be large and complex. While GET requests are supported, they are limited by URL length.
    /// Using POST with the `data` parameter allows for arbitrarily complex queries without hitting length limits.
    /// </para>
    /// <para>
    /// <strong>Resilience:</strong><br/>
    /// This method wraps the HTTP call and JSON deserialization. It returns <c>default</c> (null) on failure
    /// rather than throwing, allowing the caller (ContextReportService) to continue with partial data ("Fan-Out" resilience).
    /// </para>
    /// </remarks>
    private async Task<T?> FetchAndProcessAsync<T>(string query, Func<List<OverpassElement>, T> processor, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.OverpassBaseUrl.TrimEnd('/')}/api/interpreter")
        {
            // The Overpass API expects the query in a form field named "data".
            Content = new StringContent($"data={Uri.EscapeDataString(query)}", Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Overpass lookup failed with status {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        using var content = await response.Content.ReadAsStreamAsync(ct);
        var overpassResponse = await JsonSerializer.DeserializeAsync<OverpassResponse>(content, cancellationToken: ct);

        if (overpassResponse?.Elements is null)
        {
            _logger.LogWarning("Overpass lookup response was missing expected elements array");
            return default;
        }

        return processor(overpassResponse.Elements);
    }
}
