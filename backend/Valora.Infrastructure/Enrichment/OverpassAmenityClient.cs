using System.Globalization;
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
using Valora.Domain.Common;

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

        var result = await FetchAndProcessAsync(query, elements => {
            var schoolCount = 0;
            var supermarketCount = 0;
            var parkCount = 0;
            var healthcareCount = 0;
            var transitCount = 0;
            var chargingStationCount = 0;
            double? nearestDistance = null;

            foreach (var element in elements)
            {
                if (!TryGetCoordinates(element, out var lat, out var lon)) continue;

                var distance = GeoDistance.BetweenMeters(location.Latitude, location.Longitude, lat, lon);
                if (!nearestDistance.HasValue || distance < nearestDistance.Value)
                {
                    nearestDistance = distance;
                }

                var tags = GetTags(element);
                var amenity = tags.GetValueOrDefault("amenity");
                var shop = tags.GetValueOrDefault("shop");
                var leisure = tags.GetValueOrDefault("leisure");
                var highway = tags.GetValueOrDefault("highway");
                var railway = tags.GetValueOrDefault("railway");

                if (amenity == "school") schoolCount++;
                if (shop == "supermarket") supermarketCount++;
                if (leisure == "park") parkCount++;
                if (amenity is "hospital" or "clinic" or "doctors" or "pharmacy") healthcareCount++;
                if (amenity == "charging_station") chargingStationCount++;
                if (highway == "bus_stop" || railway == "station") transitCount++;
            }

            var populatedCategoryCount = new[] { schoolCount, supermarketCount, parkCount, healthcareCount, transitCount, chargingStationCount }.Count(c => c > 0);
            var diversityScore = populatedCategoryCount / 6d * 100d;

            return new AmenityStatsDto(
                SchoolCount: schoolCount,
                SupermarketCount: supermarketCount,
                ParkCount: parkCount,
                HealthcareCount: healthcareCount,
                TransitStopCount: transitCount,
                NearestAmenityDistanceMeters: nearestDistance,
                DiversityScore: diversityScore,
                RetrievedAtUtc: DateTimeOffset.UtcNow,
                ChargingStationCount: chargingStationCount);
        }, cancellationToken);

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

        var query = BuildOverpassBboxQuery(minLat, minLon, maxLat, maxLon, types);
        var results = await FetchAndProcessAsync(query, elements => {
            var list = new List<MapAmenityDto>();
            foreach (var element in elements)
            {
                if (!TryGetCoordinates(element, out var lat, out var lon)) continue;

                var tags = GetTags(element);
                var name = tags.GetValueOrDefault("name") ?? tags.GetValueOrDefault("operator") ?? "Amenity";
                var type = GetAmenityType(tags);
                var id = element.Id != 0 ? element.Id.ToString() : Guid.NewGuid().ToString();

                list.Add(new MapAmenityDto(id, type, name, lat, lon, tags));
            }
            return list;
        }, cancellationToken);

        results ??= [];
        _cache.Set(cacheKey, results, TimeSpan.FromMinutes(_options.AmenitiesCacheMinutes));
        return results;
    }

    private async Task<T?> FetchAndProcessAsync<T>(string query, Func<List<OverpassElement>, T> processor, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.OverpassBaseUrl.TrimEnd('/')}/api/interpreter")
        {
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
             + $"nwr(around:{radiusMeters},{lat},{lon})[amenity=charging_station];"
             + ");out center tags;";
    }

    private static string BuildOverpassBboxQuery(double minLat, double minLon, double maxLat, double maxLon, List<string>? types)
    {
        var bbox = $"{minLat.ToString(CultureInfo.InvariantCulture)},{minLon.ToString(CultureInfo.InvariantCulture)},{maxLat.ToString(CultureInfo.InvariantCulture)},{maxLon.ToString(CultureInfo.InvariantCulture)}";

        var queryBuilder = new StringBuilder();
        queryBuilder.Append($"[out:json][timeout:25];(");

        if (types == null || types.Contains("school")) queryBuilder.Append($"nwr({bbox})[amenity=school];");
        if (types == null || types.Contains("supermarket")) queryBuilder.Append($"nwr({bbox})[shop=supermarket];");
        if (types == null || types.Contains("park")) queryBuilder.Append($"nwr({bbox})[leisure=park];");
        if (types == null || types.Contains("healthcare")) queryBuilder.Append($"nwr({bbox})[amenity~\"hospital|clinic|doctors|pharmacy\"];");
        if (types == null || types.Contains("transit"))
        {
            queryBuilder.Append($"nwr({bbox})[highway=bus_stop];");
            queryBuilder.Append($"nwr({bbox})[railway=station];");
        }
        if (types == null || types.Contains("charging_station")) queryBuilder.Append($"nwr({bbox})[amenity=charging_station];");

        queryBuilder.Append(");out center tags;");
        return queryBuilder.ToString();
    }

    private static string GetAmenityType(Dictionary<string, string> tags)
    {
        if (tags.TryGetValue("amenity", out var a))
        {
            if (a == "school") return "school";
            if (a is "hospital" or "clinic" or "doctors" or "pharmacy") return "healthcare";
            if (a == "charging_station") return "charging_station";
        }
        if (tags.TryGetValue("shop", out var s) && s == "supermarket") return "supermarket";
        if (tags.TryGetValue("leisure", out var l) && l == "park") return "park";
        if (tags.TryGetValue("highway", out var h) && h == "bus_stop") return "transit";
        if (tags.TryGetValue("railway", out var r) && r == "station") return "transit";

        return "other";
    }

    private static Dictionary<string, string> GetTags(OverpassElement element)
    {
        var tags = new Dictionary<string, string>();
        if (element.Tags.HasValue && element.Tags.Value.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.Tags.Value.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    tags[prop.Name] = prop.Value.GetString()!;
                }
            }
        }
        return tags;
    }

    private static bool TryGetCoordinates(OverpassElement element, out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;

        if (element.Lat.HasValue && element.Lon.HasValue)
        {
            latitude = element.Lat.Value;
            longitude = element.Lon.Value;
            return true;
        }

        if (element.Center is not null)
        {
            latitude = element.Center.Lat;
            longitude = element.Center.Lon;
            return true;
        }

        return false;
    }
}
