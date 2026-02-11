using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;

namespace Valora.Infrastructure.Enrichment;

public sealed class PdokSoilClient : IPdokSoilClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<PdokSoilClient> _logger;

    public PdokSoilClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<PdokSoilClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FoundationRiskDto?> GetFoundationRiskAsync(ResolvedLocationDto location, CancellationToken cancellationToken = default)
    {
        if (!location.RdX.HasValue || !location.RdY.HasValue)
        {
            return null;
        }

        var cacheKey = $"soil:{location.RdX}:{location.RdY}";
        if (_cache.TryGetValue(cacheKey, out FoundationRiskDto? cached))
        {
            return cached;
        }

        var x = location.RdX.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var y = location.RdY.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // PDOK BRO Bodemkaart WFS
        // Layer: bodemkaart
        // Geometry column: geometrie (standard for PDOK)
        var featureType = "bodemkaart";
        var url = $"https://service.pdok.nl/bzk/bro-bodemkaart/wfs/v1_0?service=WFS&version=2.0.0&request=GetFeature&typeName={featureType}&outputFormat=application/json&cql_filter=INTERSECTS(geometrie,POINT({x}%20{y}))";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PDOK Soil lookup failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("features", out var features) ||
                features.ValueKind != JsonValueKind.Array ||
                features.GetArrayLength() == 0)
            {
                _cache.Set(cacheKey, null as FoundationRiskDto, TimeSpan.FromHours(24));
                return null;
            }

            var feature = features[0];
            string? soilGroup = null;

            if (feature.TryGetProperty("properties", out var props))
            {
                 if (props.TryGetProperty("bodemhoofdgroep", out var group)) soilGroup = group.GetString();
            }

            if (string.IsNullOrWhiteSpace(soilGroup))
            {
                return null;
            }

            var (risk, desc) = MapSoilToRisk(soilGroup);

            var result = new FoundationRiskDto(
                RiskLevel: risk,
                SoilType: soilGroup,
                Description: desc,
                RetrievedAtUtc: DateTimeOffset.UtcNow);

            _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching soil data from PDOK");
            return null;
        }
    }

    private static (string Risk, string Desc) MapSoilToRisk(string soilGroup)
    {
        return soilGroup.ToLowerInvariant() switch
        {
            "veen" => ("High", "Peat soil carries a high risk of subsidence and foundation issues."),
            "klei" => ("Medium", "Clay soil can be stable but may compress over time."),
            "zand" => ("Low", "Sand is generally stable and good for foundations."),
            "leem" => ("Low", "Loam is generally stable."),
            _ => ("Unknown", $"Soil type: {soilGroup}")
        };
    }
}
