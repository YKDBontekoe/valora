using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;

namespace Valora.Infrastructure.Enrichment;

public sealed class PdokBuildingClient : IPdokBuildingClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<PdokBuildingClient> _logger;

    public PdokBuildingClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<PdokBuildingClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SolarPotentialDto?> GetSolarPotentialAsync(ResolvedLocationDto location, CancellationToken cancellationToken = default)
    {
        if (!location.RdX.HasValue || !location.RdY.HasValue)
        {
            return null;
        }

        var cacheKey = $"building:{location.RdX}:{location.RdY}";
        if (_cache.TryGetValue(cacheKey, out SolarPotentialDto? cached))
        {
            return cached;
        }

        var x = location.RdX.Value.ToString(CultureInfo.InvariantCulture);
        var y = location.RdY.Value.ToString(CultureInfo.InvariantCulture);

        // PDOK BAG WFS
        // Note: Using outputFormat=application/json
        var url = $"https://service.pdok.nl/lv/bag/wfs/v2_0?service=WFS&version=2.0.0&request=GetFeature&typeName=bag:pand&outputFormat=application/json&cql_filter=INTERSECTS(geometrie,POINT({x}%20{y}))";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PDOK Building lookup failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("features", out var features) ||
                features.ValueKind != JsonValueKind.Array ||
                features.GetArrayLength() == 0)
            {
                _cache.Set(cacheKey, null as SolarPotentialDto, TimeSpan.FromHours(24));
                return null;
            }

            var feature = features[0];
            double? area = null;
            int? year = null;

            if (feature.TryGetProperty("properties", out var props))
            {
                area = GetDouble(props, "oppervlakte");
                year = GetInt(props, "bouwjaar");
            }

            if (!area.HasValue || area.Value <= 0)
            {
                return null;
            }

            // Simple Heuristic for Solar Potential
            // Assume 40% of footprint is usable roof area
            var usableArea = area.Value * 0.4;
            // 1 panel = ~1.6m2 (approx 2m2 with spacing)
            var panels = (int)(usableArea / 2.0);
            // ~300 kWh per panel per year
            var kwh = panels * 300.0;

            string potential = "Low";
            if (kwh > 3500) potential = "High";     // 12+ panels
            else if (kwh > 2000) potential = "Medium"; // 7+ panels

            // Penalty for very old buildings (risk of monument status or weak roof)
            if (year.HasValue && year.Value < 1930 && potential == "High")
            {
                potential = "Medium";
            }

            var result = new SolarPotentialDto(
                Potential: potential,
                RoofAreaM2: area.Value,
                InstallablePanels: panels,
                EstimatedGenerationKwh: Math.Round(kwh, 0),
                RetrievedAtUtc: DateTimeOffset.UtcNow);

            _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching building data from PDOK");
            return null;
        }
    }

    private static double? GetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)) return null;
        if (property.ValueKind == JsonValueKind.Number) return property.GetDouble();
        if (property.ValueKind == JsonValueKind.String && double.TryParse(property.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var val)) return val;
        return null;
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)) return null;
        if (property.ValueKind == JsonValueKind.Number) return property.GetInt32();
        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var val)) return val;
        return null;
    }
}
