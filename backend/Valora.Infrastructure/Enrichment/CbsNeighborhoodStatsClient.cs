using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;

namespace Valora.Infrastructure.Enrichment;

public sealed class CbsNeighborhoodStatsClient : ICbsNeighborhoodStatsClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;

    public CbsNeighborhoodStatsClient(HttpClient httpClient, IMemoryCache cache, IOptions<ContextEnrichmentOptions> options)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<NeighborhoodStatsDto?> GetStatsAsync(ResolvedLocationDto location, CancellationToken cancellationToken = default)
    {
        var candidates = BuildCandidates(location).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        foreach (var code in candidates)
        {
            var result = await GetForCodeAsync(code, cancellationToken);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private async Task<NeighborhoodStatsDto?> GetForCodeAsync(string regionCode, CancellationToken cancellationToken)
    {
        var cacheKey = $"cbs:{regionCode}";
        if (_cache.TryGetValue(cacheKey, out NeighborhoodStatsDto? cached))
        {
            return cached;
        }

        var escapedCode = Uri.EscapeDataString(regionCode);
        var url =
            $"{_options.CbsBaseUrl.TrimEnd('/')}/83765NED/TypedDataSet?$filter=WijkenEnBuurten%20eq%20'{escapedCode}'&$top=1&$select=WijkenEnBuurten,SoortRegio_2,AantalInwoners_5,Bevolkingsdichtheid_33,GemiddeldeWOZWaardeVanWoningen_35,HuishoudensMetEenLaagInkomen_72";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("value", out var values) ||
            values.ValueKind != JsonValueKind.Array ||
            values.GetArrayLength() == 0)
        {
            _cache.Set(cacheKey, null as NeighborhoodStatsDto, TimeSpan.FromMinutes(_options.CbsCacheMinutes));
            return null;
        }

        var row = values[0];

        var result = new NeighborhoodStatsDto(
            RegionCode: GetString(row, "WijkenEnBuurten")?.Trim() ?? regionCode.Trim(),
            RegionType: GetString(row, "SoortRegio_2")?.Trim() ?? "Onbekend",
            Residents: GetInt(row, "AantalInwoners_5"),
            PopulationDensity: GetInt(row, "Bevolkingsdichtheid_33"),
            AverageWozValueKeur: GetDouble(row, "GemiddeldeWOZWaardeVanWoningen_35"),
            LowIncomeHouseholdsPercent: GetDouble(row, "HuishoudensMetEenLaagInkomen_72"),
            RetrievedAtUtc: DateTimeOffset.UtcNow);

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CbsCacheMinutes));
        return result;
    }

    private static IEnumerable<string> BuildCandidates(ResolvedLocationDto location)
    {
        if (!string.IsNullOrWhiteSpace(location.NeighborhoodCode))
        {
            yield return location.NeighborhoodCode.Trim().PadRight(10);
        }

        if (!string.IsNullOrWhiteSpace(location.DistrictCode))
        {
            yield return location.DistrictCode.Trim().PadRight(10);
        }

        if (!string.IsNullOrWhiteSpace(location.MunicipalityCode))
        {
            yield return location.MunicipalityCode.Trim().PadRight(10);
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
        {
            return value;
        }

        if (property.ValueKind == JsonValueKind.String &&
            int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static double? GetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var value))
        {
            return value;
        }

        if (property.ValueKind == JsonValueKind.String &&
            double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
