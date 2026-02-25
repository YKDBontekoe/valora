using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Utilities;
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
    /// Process:
    /// 1. Normalize input (extract address from URL if applicable).
    /// 2. Check local memory cache.
    /// 3. Query PDOK "free" endpoint (fuzzy search) filtered by <c>type:adres</c>.
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

        var normalizedInput = UrlNormalizationUtils.NormalizeInput(input);
        var cacheKey = $"pdok-resolve:{normalizedInput}";

        if (_cache.TryGetValue(cacheKey, out ResolvedLocationDto? cached))
        {
            return cached;
        }

        // Query the "free" endpoint which allows for flexible/fuzzy input
        // Why fq=type:adres?
        // We only want exact addresses (e.g., "Damrak 1"). Without this filter, PDOK returns
        // streets ("Damrak"), neighborhoods, or municipalities, which don't have a specific
        // lat/lon point suitable for a context report.
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
                // Why? User typos (e.g., "Damrak 99999") are common. Repeatedly querying PDOK for
                // known invalid addresses wastes bandwidth and latency. We cache the "null" result.
                _cache.Set(cacheKey, null as ResolvedLocationDto, TimeSpan.FromMinutes(_options.ResolverCacheMinutes));
                return null;
            }

            var doc = docsElement[0];
            // Why two coordinate systems?
            // 1. WGS84 (centroide_ll): Standard GPS Lat/Lon used for the mobile app, map display, and Overpass API.
            // 2. Rijksdriehoek (centroide_rd): Dutch national grid (X/Y in meters). Used for accurate
            //    distance calculations and querying CBS data (which often uses RD coordinates).
            var pointLl = GeoUtils.TryParseWktPoint(GetString(doc, "centroide_ll"));
            var pointRd = GeoUtils.TryParseWktPoint(GetString(doc, "centroide_rd"));

            if (pointLl is null)
            {
                _logger.LogWarning("PDOK response did not include valid coordinates");
                return null;
            }

            var result = new ResolvedLocationDto(
                Query: input,
                DisplayAddress: GetString(doc, "weergavenaam") ?? normalizedInput,
                Latitude: pointLl.Value.Y,
                Longitude: pointLl.Value.X,
                RdX: pointRd?.X,
                RdY: pointRd?.Y,
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
}
