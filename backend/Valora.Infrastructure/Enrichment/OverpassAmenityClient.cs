using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
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

        var query = BuildOverpassQuery(location.Latitude, location.Longitude, radiusMeters);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.OverpassBaseUrl.TrimEnd('/')}/api/interpreter")
        {
            Content = new StringContent($"data={Uri.EscapeDataString(query)}", Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Overpass amenity lookup failed with status {StatusCode}", response.StatusCode);
            return null;
        }

        JsonDocument document;
        try
        {
            using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Overpass amenity lookup returned invalid JSON");
            return null;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("elements", out var elements) || elements.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Overpass amenity lookup response was missing expected elements array");
                return null;
            }

            var schoolCount = 0;
            var supermarketCount = 0;
            var parkCount = 0;
            var healthcareCount = 0;
            var transitCount = 0;
            double? nearestDistance = null;

            foreach (var element in elements.EnumerateArray())
            {
                if (!TryGetCoordinates(element, out var lat, out var lon))
                {
                    continue;
                }

                var distance = GeoDistance.BetweenMeters(location.Latitude, location.Longitude, lat, lon);
                if (!nearestDistance.HasValue || distance < nearestDistance.Value)
                {
                    nearestDistance = distance;
                }

                if (!element.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var amenity = GetTag(tags, "amenity");
                var shop = GetTag(tags, "shop");
                var leisure = GetTag(tags, "leisure");
                var highway = GetTag(tags, "highway");
                var railway = GetTag(tags, "railway");

                if (amenity == "school") schoolCount++;
                if (shop == "supermarket") supermarketCount++;
                if (leisure == "park") parkCount++;
                if (amenity is "hospital" or "clinic" or "doctors" or "pharmacy") healthcareCount++;
                if (highway == "bus_stop" || railway == "station") transitCount++;
            }

            var populatedCategoryCount = new[] { schoolCount, supermarketCount, parkCount, healthcareCount, transitCount }.Count(c => c > 0);
            var diversityScore = populatedCategoryCount / 5d * 100d;

            var result = new AmenityStatsDto(
                SchoolCount: schoolCount,
                SupermarketCount: supermarketCount,
                ParkCount: parkCount,
                HealthcareCount: healthcareCount,
                TransitStopCount: transitCount,
                NearestAmenityDistanceMeters: nearestDistance,
                DiversityScore: diversityScore,
                RetrievedAtUtc: DateTimeOffset.UtcNow);

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.AmenitiesCacheMinutes));
            return result;
        }
    }

    private static string BuildOverpassQuery(double latitude, double longitude, int radiusMeters)
    {
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);

        return $"[out:json][timeout:25];("
             + $"nwr(around:{radiusMeters},{lat},{lon})[amenity=school];"
             + $"nwr(around:{radiusMeters},{lat},{lon})[shop=supermarket];"
             + $"nwr(around:{radiusMeters},{lat},{lon})[leisure=park];"
             + $"nwr(around:{radiusMeters},{lat},{lon})[amenity~\"hospital|clinic|doctors|pharmacy\"];"
             + $"nwr(around:{radiusMeters},{lat},{lon})[highway=bus_stop];"
             + $"nwr(around:{radiusMeters},{lat},{lon})[railway=station];"
             + ");out center tags;";
    }

    private static string? GetTag(JsonElement tags, string key)
    {
        if (!tags.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    private static bool TryGetCoordinates(JsonElement element, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;

        if (element.TryGetProperty("lat", out var lat) && element.TryGetProperty("lon", out var lon) &&
            lat.TryGetDouble(out latitude) && lon.TryGetDouble(out longitude))
        {
            return true;
        }

        if (element.TryGetProperty("center", out var center) &&
            center.TryGetProperty("lat", out var centerLat) &&
            center.TryGetProperty("lon", out var centerLon) &&
            centerLat.TryGetDouble(out latitude) && centerLon.TryGetDouble(out longitude))
        {
            return true;
        }

        return false;
    }
}
