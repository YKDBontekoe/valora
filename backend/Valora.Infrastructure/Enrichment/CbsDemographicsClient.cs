using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;

namespace Valora.Infrastructure.Enrichment;

/// <summary>
/// Fetches demographic data from CBS Open Data table 83765NED (extended fields for age and household composition).
/// </summary>
public sealed class CbsDemographicsClient : IDemographicsClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<CbsDemographicsClient> _logger;

    public CbsDemographicsClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<CbsDemographicsClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DemographicsDto?> GetDemographicsAsync(ResolvedLocationDto location, CancellationToken cancellationToken = default)
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

    private async Task<DemographicsDto?> GetForCodeAsync(string regionCode, CancellationToken cancellationToken)
    {
        var cacheKey = $"cbs-demo:{regionCode}";
        if (_cache.TryGetValue(cacheKey, out DemographicsDto? cached))
        {
            return cached;
        }

        var escapedCode = Uri.EscapeDataString(regionCode);
        // CBS table 83765NED extended fields for demographics
        var url =
            $"{_options.CbsBaseUrl.TrimEnd('/')}/83765NED/TypedDataSet?$filter=WijkenEnBuurten%20eq%20'{escapedCode}'&$top=1&$select=WijkenEnBuurten,k_0Tot15Jaar_8,k_15Tot25Jaar_9,k_25Tot45Jaar_10,k_45Tot65Jaar_11,k_65JaarOfOuder_12,GemiddeldeHuishoudensgrootte_32,Koopwoningen_40,Eenpersoonshuishoudens_29,HuishoudensMetKinderen_31";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CBS demographics lookup failed for region {RegionCode} with status {StatusCode}", regionCode.Trim(), response.StatusCode);
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
            _logger.LogWarning(ex, "CBS demographics lookup returned invalid JSON for region {RegionCode}", regionCode.Trim());
            return null;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("value", out var values) ||
                values.ValueKind != JsonValueKind.Array ||
                values.GetArrayLength() == 0)
            {
                _cache.Set(cacheKey, null as DemographicsDto, TimeSpan.FromMinutes(_options.CbsCacheMinutes));
                return null;
            }

            var row = values[0];

            var result = new DemographicsDto(
                PercentAge0To14: GetInt(row, "k_0Tot15Jaar_8"),
                PercentAge15To24: GetInt(row, "k_15Tot25Jaar_9"),
                PercentAge25To44: GetInt(row, "k_25Tot45Jaar_10"),
                PercentAge45To64: GetInt(row, "k_45Tot65Jaar_11"),
                PercentAge65Plus: GetInt(row, "k_65JaarOfOuder_12"),
                AverageHouseholdSize: GetDouble(row, "GemiddeldeHuishoudensgrootte_32"),
                PercentOwnerOccupied: GetInt(row, "Koopwoningen_40"),
                PercentSingleHouseholds: GetInt(row, "Eenpersoonshuishoudens_29"),
                PercentFamilyHouseholds: GetInt(row, "HuishoudensMetKinderen_31"),
                RetrievedAtUtc: DateTimeOffset.UtcNow);

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CbsCacheMinutes));
            return result;
        }
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
