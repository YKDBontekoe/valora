using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;

namespace Valora.Infrastructure.Enrichment;

/// <summary>
/// Implements location resolution using the official Dutch PDOK Locatieserver.
/// </summary>
public sealed class PdokLocationResolver : ILocationResolver
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<PdokLocationResolver> _logger;

    public PdokLocationResolver(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<PdokLocationResolver> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Resolves an address string or URL to a normalized location object using the PDOK Locatieserver.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method serves as the entry point for turning user input (e.g., "Damrak 1" or a Funda URL)
    /// into a structured location entity with coordinates and administrative hierarchy codes.
    /// </para>
    /// <para>
    /// <strong>Why the "free" endpoint?</strong>
    /// We use <c>/free</c> instead of <c>/suggest</c> or <c>/lookup</c> because it handles "fuzzy" inputs best.
    /// It can interpret "Damrak Amsterdam" just as well as "Damrak 1, 1012LG Amsterdam".
    /// </para>
    /// <para>
    /// Process:
    /// 1. Normalize input (extract address from URL if applicable).
    /// 2. Check local memory cache.
    /// 3. Query PDOK "free" endpoint filtered by <c>type:adres</c>.
    /// 4. Parse response to extract WGS84 (GPS) and RD New (Rijksdriehoek) coordinates.
    /// </para>
    /// </remarks>
    /// <param name="input">Raw user input (address string or URL).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ResolvedLocationDto"/> if found, otherwise <c>null</c>.</returns>
    public async Task<ResolvedLocationDto?> ResolveAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var normalizedInput = NormalizeInput(input);
        var cacheKey = $"pdok-resolve:{normalizedInput}";

        if (_cache.TryGetValue(cacheKey, out ResolvedLocationDto? cached))
        {
            return cached;
        }

        // Query the "free" endpoint which allows for flexible/fuzzy input
        // fq=type:adres restricts results to specific addresses (house numbers), filtering out general place names like cities or streets.
        // Example: It ensures we get "Damrak 1" (a building) rather than just "Damrak" (the street) or "Amsterdam" (the city).
        var encodedQ = WebUtility.UrlEncode(normalizedInput);
        var url = $"{_options.PdokBaseUrl.TrimEnd('/')}/bzk/locatieserver/search/v3_1/free?q={encodedQ}&fq=type:adres&rows=1";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PDOK resolve failed with status {StatusCode} for input '{InputHash}'", response.StatusCode, normalizedInput.GetHashCode());
                return null;
            }

            using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("response", out var responseElement) ||
                !responseElement.TryGetProperty("docs", out var docsElement) ||
                docsElement.ValueKind != JsonValueKind.Array ||
                docsElement.GetArrayLength() == 0)
            {
                // Cache negative results to prevent repeated bad calls
                _cache.Set(cacheKey, null as ResolvedLocationDto, TimeSpan.FromMinutes(_options.ResolverCacheMinutes));
                return null;
            }

            var doc = docsElement[0];
            // centroide_ll = WGS84 (Lat/Lon), centroide_rd = Rijksdriehoek (X/Y)
            var pointLl = TryParsePoint(GetString(doc, "centroide_ll"));
            var pointRd = TryParsePoint(GetString(doc, "centroide_rd"));

            if (pointLl is null)
            {
                _logger.LogWarning("PDOK response did not include valid coordinates");
                return null;
            }

            var result = new ResolvedLocationDto(
                Query: input,
                DisplayAddress: GetString(doc, "weergavenaam") ?? normalizedInput,
                Latitude: pointLl.Value.Latitude,
                Longitude: pointLl.Value.Longitude,
                RdX: pointRd?.Longitude,
                RdY: pointRd?.Latitude,
                MunicipalityCode: PrefixCode(GetString(doc, "gemeentecode"), "GM"),
                MunicipalityName: GetString(doc, "gemeentenaam"),
                DistrictCode: GetString(doc, "wijkcode"),
                DistrictName: GetString(doc, "wijknaam"),
                NeighborhoodCode: GetString(doc, "buurtcode"),
                NeighborhoodName: GetString(doc, "buurtnaam"),
                PostalCode: GetString(doc, "postcode"));

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.ResolverCacheMinutes));
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDOK resolve failed exceptionally for input '{InputHash}'", normalizedInput.GetHashCode());
            return null;
        }
    }

    /// <summary>
    /// Normalizes raw user input into a searchable address string.
    /// Handles extraction of address parts from supported listing URLs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Funda URL Heuristic:</strong>
    /// Funda URLs typically follow the pattern: <c>/koop/city/id-street-number/</c>.
    /// We extract the last path segment (the "slug") and replace hyphens with spaces to create a search query.
    /// Example: <c>.../huis-424242-straatnaam-1</c> -> <c>huis 424242 straatnaam 1</c>.
    /// PDOK's fuzzy search is smart enough to ignore the "huis" and ID and match the street/number.
    /// </para>
    /// </remarks>
    private static string NormalizeInput(string input)
    {
        // Heuristic: If input looks like a Funda URL, extract the last path segment
        // which typically contains the address (e.g., .../huis-424242-straatnaam-1)
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) &&
            uri.Host.Contains("funda.nl", StringComparison.OrdinalIgnoreCase))
        {
            var segment = uri.Segments
                .Select(s => s.Trim('/'))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .LastOrDefault();

            if (!string.IsNullOrWhiteSpace(segment))
            {
                // Funda slugs are usually kebab-case; replace hyphens with spaces for better searchability
                return Uri.UnescapeDataString(segment).Replace('-', ' ');
            }
        }

        return input.Trim();
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            _ => null
        };
    }

    private static string? PrefixCode(string? code, string prefix)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        if (code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return code.ToUpperInvariant();
        }

        return $"{prefix}{code}";
    }

    private static (double Longitude, double Latitude)? TryParsePoint(string? point)
    {
        if (string.IsNullOrWhiteSpace(point))
        {
            return null;
        }

        const string prefix = "POINT(";
        if (!point.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !point.EndsWith(')'))
        {
            return null;
        }

        var body = point[prefix.Length..^1];
        var parts = body.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return null;
        }

        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
        {
            return null;
        }

        return (x, y);
    }
}
