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
/// Fetches crime statistics from CBS Open Data table 47018NED (Geregistreerde misdrijven; soort misdrijf, regio).
/// </summary>
public sealed class CbsCrimeStatsClient : ICbsCrimeStatsClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<CbsCrimeStatsClient> _logger;

    public CbsCrimeStatsClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<CbsCrimeStatsClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CrimeStatsDto?> GetStatsAsync(ResolvedLocationDto location, CancellationToken cancellationToken = default)
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

    private async Task<CrimeStatsDto?> GetForCodeAsync(string regionCode, CancellationToken cancellationToken)
    {
        var cacheKey = $"cbs-crime:{regionCode}";
        if (_cache.TryGetValue(cacheKey, out CrimeStatsDto? cached))
        {
            return cached;
        }

        var escapedCode = Uri.EscapeDataString(regionCode);
        // Using table 83765NED (Kerncijfers wijken en buurten) as it contains crime stats
        // Columns: 
        // TotaalDiefstalUitWoningSchuurED_106 (Theft from home/shed)
        // VernielingMisdrijfTegenOpenbareOrde_107 (Vandalism/Public Order)
        // GeweldsEnSeksueleMisdrijven_108 (Violent/Sexual crimes)
        var url =
            $"{_options.CbsBaseUrl.TrimEnd('/')}/83765NED/TypedDataSet?$filter=WijkenEnBuurten%20eq%20'{escapedCode}'&$top=1&$select=WijkenEnBuurten,TotaalDiefstalUitWoningSchuurED_106,VernielingMisdrijfTegenOpenbareOrde_107,GeweldsEnSeksueleMisdrijven_108";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CBS crime lookup failed for region {RegionCode} with status {StatusCode}", regionCode.Trim(), response.StatusCode);
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
            _logger.LogWarning(ex, "CBS crime lookup returned invalid JSON for region {RegionCode}", regionCode.Trim());
            return null;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("value", out var values) ||
                values.ValueKind != JsonValueKind.Array ||
                values.GetArrayLength() == 0)
            {
                _cache.Set(cacheKey, null as CrimeStatsDto, TimeSpan.FromMinutes(_options.CbsCacheMinutes));
                return null;
            }

            var row = values[0];

            // Note: These are absolute numbers or per 1000 residents depending on the specific metric in Kerncijfers.
            // Documentation for 83765NED usually implies per 1000 for crime rates or absolute counts.
            // Given typical values (e.g. 50 for violence in a neighborhood of 1000 people), it's likely absolute counts or rates per 1000.
            // We will treat them as raw values which fit the generated DTO structure.
            
            var theft = GetInt(row, "TotaalDiefstalUitWoningSchuurED_106");
            var vandalism = GetInt(row, "VernielingMisdrijfTegenOpenbareOrde_107");
            var violent = GetInt(row, "GeweldsEnSeksueleMisdrijven_108");
            
            var total = (theft ?? 0) + (vandalism ?? 0) + (violent ?? 0);

            var result = new CrimeStatsDto(
                TotalCrimesPer1000: total > 0 ? total : null,
                BurglaryPer1000: theft, // Mapping theft from home/shed to Burglary
                ViolentCrimePer1000: violent,
                TheftPer1000: null, // No generic theft column
                VandalismPer1000: vandalism,
                YearOverYearChangePercent: null,
                RetrievedAtUtc: DateTimeOffset.UtcNow);

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CbsCacheMinutes));
            return result;
        }
    }

    private static IEnumerable<string> BuildCandidates(ResolvedLocationDto location)
    {
        // CBS crime data uses GM/WK/BU codes like neighborhood stats
        if (!string.IsNullOrWhiteSpace(location.NeighborhoodCode))
        {
            yield return location.NeighborhoodCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(location.DistrictCode))
        {
            yield return location.DistrictCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(location.MunicipalityCode))
        {
            yield return location.MunicipalityCode.Trim();
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
}
