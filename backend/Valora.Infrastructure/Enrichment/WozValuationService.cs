using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;

namespace Valora.Infrastructure.Enrichment;

public class WozValuationService : IWozValuationService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<WozValuationService> _logger;

    public WozValuationService(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<WozValuationService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<WozValuationDto?> GetWozValuationAsync(string street, int number, string? suffix, string city, string? nummeraanduidingId = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"woz:{city}:{street}:{number}{suffix}";
        if (_cache.TryGetValue(cacheKey, out WozValuationDto? cached))
        {
            return cached;
        }

        try
        {
            // 0. Initialize Session (Cookies)
            // WOZ-waardeloket requires a session cookie which is set on the main page
            var homeRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.wozwaardeloket.nl/");
            AddWozHeaders(homeRequest);

            using var homeResponse = await _httpClient.SendAsync(homeRequest, cancellationToken);
            if (!homeResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to initialize WOZ session. Status: {Status}, Reason: {Reason}",
                    homeResponse.StatusCode, homeResponse.ReasonPhrase);
            }

            long? targetId = null;

            // 1. Get Target ID (either from parameter or suggest API)
            if (!string.IsNullOrEmpty(nummeraanduidingId) && long.TryParse(nummeraanduidingId, out var parsedId))
            {
                targetId = parsedId;
            }
            else
            {
                // Fallback to Suggest API
                // Format: "{City} {Street} {Number}" seems more reliable than "{Street} {Number} {City}"
                var query = $"{city} {street} {number}{suffix}".Trim();
                var suggestUrl = $"https://api.kadaster.nl/lvwoz/wozwaardeloket-api/v1/suggest?q={Uri.EscapeDataString(query)}";

                var suggestRequest = new HttpRequestMessage(HttpMethod.Get, suggestUrl);
                AddWozHeaders(suggestRequest);

                using var suggestResponse = await _httpClient.SendAsync(suggestRequest, cancellationToken);
                suggestResponse.EnsureSuccessStatusCode();

                var suggestResult = await suggestResponse.Content.ReadFromJsonAsync<WozSuggestResponse>(cancellationToken: cancellationToken);
                var match = suggestResult?.Docs?.FirstOrDefault();

                if (match?.AdresseerbaarObjectId != null)
                {
                    targetId = match.AdresseerbaarObjectId;
                }
            }

            if (targetId == null)
            {
                _logger.LogWarning("No WOZ object ID found for requested property (Hash: {CacheKey})", cacheKey);
                return null;
            }

            // 2. Fetch the WOZ value using the Object Number
            // Note: The API endpoint says 'nummeraanduiding', but observation shows it uses the nummeraanduiding_id (BAG)
            var detailsUrl = $"https://api.kadaster.nl/lvwoz/wozwaardeloket-api/v1/wozwaarde/nummeraanduiding/{targetId}";
            var detailsRequest = new HttpRequestMessage(HttpMethod.Get, detailsUrl);
            AddWozHeaders(detailsRequest);

            using var detailsResponse = await _httpClient.SendAsync(detailsRequest, cancellationToken);
            detailsResponse.EnsureSuccessStatusCode();

            var detailsResult = await detailsResponse.Content.ReadFromJsonAsync<WozValuationResponse>(cancellationToken: cancellationToken);

            // Get the latest valuation
            var latestValuation = detailsResult?.WozWaarden
                ?.OrderByDescending(w => w.Peildatum)
                .FirstOrDefault();

            if (latestValuation == null)
            {
                _logger.LogWarning("No valuation found for WOZ object {TargetId}", targetId);
                return null;
            }

            var result = new WozValuationDto(
                Value: latestValuation.VastgesteldeWaarde,
                ReferenceDate: latestValuation.Peildatum,
                Source: "WOZ-waardeloket"
            );

            // Cache - be polite to the source
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CbsCacheMinutes));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scrape WOZ value for property (Hash: {CacheKey})", cacheKey);
            return null;
        }
    }

    private static void AddWozHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("Origin", "https://www.wozwaardeloket.nl");
        request.Headers.Add("Referer", "https://www.wozwaardeloket.nl/");
        request.Headers.Add("Accept", "application/json, text/plain, */*");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    // Internal DTOs for JSON deserialization
    private record WozSuggestResponse(WozDoc[]? Docs);
    private record WozDoc(long? WozObjectNummer, long? AdresseerbaarObjectId);
    private record WozValuationResponse(WozWaarde[]? WozWaarden);
    private record WozWaarde(DateTime Peildatum, int VastgesteldeWaarde);
}
