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
            await InitializeSessionAsync(cancellationToken);

            var targetId = await GetTargetIdAsync(street, number, suffix, city, nummeraanduidingId, cancellationToken);

            if (targetId == null)
            {
                _logger.LogWarning("No WOZ object ID found for requested property (Hash: {CacheKey})", cacheKey);
                return null;
            }

            var result = await FetchWozValuationByTargetIdAsync(targetId.Value, cancellationToken);

            if (result != null)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CbsCacheMinutes));
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "WOZ value lookup network error for property (Hash: {CacheKey})", cacheKey);
            return null;
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogWarning(ex, "WOZ value lookup returned invalid JSON for property (Hash: {CacheKey})", cacheKey);
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scrape WOZ value for property (Hash: {CacheKey})", cacheKey);
            return null;
        }
    }

    private async Task InitializeSessionAsync(CancellationToken cancellationToken)
    {
        var homeRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.wozwaardeloket.nl/");
        AddWozHeaders(homeRequest);

        using var homeResponse = await _httpClient.SendAsync(homeRequest, cancellationToken);
        if (!homeResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to initialize WOZ session. Status: {Status}, Reason: {Reason}",
                homeResponse.StatusCode, homeResponse.ReasonPhrase);
        }
    }

    private async Task<long?> GetTargetIdAsync(string street, int number, string? suffix, string city, string? nummeraanduidingId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(nummeraanduidingId) && long.TryParse(nummeraanduidingId, out var parsedId))
        {
            return parsedId;
        }

        var query = $"{city} {street} {number}{suffix}".Trim();
        var suggestUrl = $"https://api.kadaster.nl/lvwoz/wozwaardeloket-api/v1/suggest?q={Uri.EscapeDataString(query)}";

        var suggestRequest = new HttpRequestMessage(HttpMethod.Get, suggestUrl);
        AddWozHeaders(suggestRequest);

        using var suggestResponse = await _httpClient.SendAsync(suggestRequest, cancellationToken);
        suggestResponse.EnsureSuccessStatusCode();

        var suggestResult = await suggestResponse.Content.ReadFromJsonAsync<WozSuggestResponse>(cancellationToken: cancellationToken);
        var match = suggestResult?.Docs?.FirstOrDefault();

        return match?.AdresseerbaarObjectId;
    }

    private async Task<WozValuationDto?> FetchWozValuationByTargetIdAsync(long targetId, CancellationToken cancellationToken)
    {
        var detailsUrl = $"https://api.kadaster.nl/lvwoz/wozwaardeloket-api/v1/wozwaarde/nummeraanduiding/{targetId}";
        var detailsRequest = new HttpRequestMessage(HttpMethod.Get, detailsUrl);
        AddWozHeaders(detailsRequest);

        using var detailsResponse = await _httpClient.SendAsync(detailsRequest, cancellationToken);
        detailsResponse.EnsureSuccessStatusCode();

        var detailsResult = await detailsResponse.Content.ReadFromJsonAsync<WozValuationResponse>(cancellationToken: cancellationToken);

        var latestValuation = detailsResult?.WozWaarden?.MaxBy(w => w.Peildatum);

        if (latestValuation == null)
        {
            _logger.LogWarning("No valuation found for WOZ object {TargetId}", targetId);
            return null;
        }

        return new WozValuationDto(
            Value: latestValuation.VastgesteldeWaarde,
            ReferenceDate: latestValuation.Peildatum,
            Source: "WOZ-waardeloket"
        );
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
