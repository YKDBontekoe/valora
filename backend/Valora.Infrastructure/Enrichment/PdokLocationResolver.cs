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
    /// <strong>Process:</strong>
    /// <list type="number">
    /// <item><strong>Normalization:</strong> Uses <see cref="UrlNormalizationUtils.NormalizeInput"/> to detect if the input is a listing URL (e.g., Funda). If so, it extracts the address slug. Otherwise, it treats it as a raw search string.</item>
    /// <item><strong>Cache Check:</strong> Checks in-memory cache to prevent redundant external API calls for popular locations.</item>
    /// <item><strong>PDOK Query:</strong> Calls the <c>suggest</c> or <c>free</c> endpoint of the PDOK Locatieserver.
    /// We use the <c>free</c> endpoint with <c>fq=type:adres</c> to ensure we only match specific addresses, not general city names or streets without numbers.</item>
    /// <item><strong>Coordinate Extraction:</strong> PDOK returns coordinates in both WGS84 (Lat/Lon) and RD New (Rijksdriehoek). We extract WGS84 for the mobile app and RD New for accurate distance calculations if needed later.</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="input">Raw user input (address string or URL).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ResolvedLocationDto"/> if a matching address is found; otherwise <c>null</c>.</returns>
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
        // fq=type:adres restricts results to specific addresses, filtering out general place names
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
            var pointLl = GeoUtils.TryParseWktPoint(doc.GetStringSafe("centroide_ll"));
            var pointRd = GeoUtils.TryParseWktPoint(doc.GetStringSafe("centroide_rd"));

            if (pointLl is null)
            {
                _logger.LogWarning("PDOK response did not include valid coordinates");
                return null;
            }

            var result = new ResolvedLocationDto(
                Query: input,
                DisplayAddress: doc.GetStringSafe("weergavenaam") ?? normalizedInput,
                Latitude: pointLl.Value.Y,
                Longitude: pointLl.Value.X,
                RdX: pointRd?.X,
                RdY: pointRd?.Y,
                MunicipalityCode: PrefixCode(doc.GetStringSafe("gemeentecode"), "GM"),
                MunicipalityName: doc.GetStringSafe("gemeentenaam"),
                DistrictCode: doc.GetStringSafe("wijkcode"),
                DistrictName: doc.GetStringSafe("wijknaam"),
                NeighborhoodCode: doc.GetStringSafe("buurtcode"),
                NeighborhoodName: doc.GetStringSafe("buurtnaam"),
                PostalCode: doc.GetStringSafe("postcode"));

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
