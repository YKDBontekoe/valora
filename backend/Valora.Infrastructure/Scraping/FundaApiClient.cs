using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

public partial class FundaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FundaApiClient> _logger;
    private const string ToppositionApiUrl = "https://www.funda.nl/api/topposition/v2/search";

    public FundaApiClient(HttpClient httpClient, ILogger<FundaApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Fetches the summary (including correct address/globalId) for a listing ID.
    /// Uses the internal API endpoint: https://www.funda.nl/api/detail-summary/v2/getsummary/{globalId}
    /// </summary>
    public virtual async Task<FundaApiListingSummary?> GetListingSummaryAsync(int globalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://www.funda.nl/api/detail-summary/v2/getsummary/{globalId}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch summary for listing {GlobalId}: {StatusCode}", globalId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FundaApiListingSummary>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching summary for listing {GlobalId}", globalId);
            return null;
        }
    }

    /// <summary>
    /// Fetches contact details (broker info) for a listing.
    /// Uses endpoint: https://contacts-bff.funda.io/api/v3/listings/{GlobalId}/contact-details?website=1
    /// </summary>
    public virtual async Task<FundaContactDetailsResponse?> GetContactDetailsAsync(int globalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://contacts-bff.funda.io/api/v3/listings/{globalId}/contact-details?website=1";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Contact details not found for listing {GlobalId}", globalId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FundaContactDetailsResponse>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching contact details for listing {GlobalId}", globalId);
            return null;
        }
    }

    /// <summary>
    /// Checks fiber internet availability.
    /// Endpoint: https://kpnopticfiber.funda.io/api/v1/{postalCode}
    /// </summary>
    public virtual async Task<FundaFiberResponse?> GetFiberAvailabilityAsync(string postalCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postalCode)) return null;
        
        // Format postal code: 1234AB (remove space)
        var cleanPostalCode = postalCode.Replace(" ", "").ToUpperInvariant();

        try
        {
            var url = $"https://kpnopticfiber.funda.io/api/v1/{cleanPostalCode}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FundaFiberResponse>(content);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check fiber availability for postal code {PostalCode}", cleanPostalCode);
            return null;
        }
    }

    /// <summary>
    /// Search for residential listings (Koop).
    /// </summary>
    public virtual async Task<FundaApiResponse?> SearchBuyAsync(
        string geoInfo,
        int page = 1,
        int? minPrice = null,
        int? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        return await SearchAsync(
            geoInfo, 
            offeringType: "buy", 
            aggregationType: "listing", 
            page: page, 
            minPrice: minPrice, 
            maxPrice: maxPrice, 
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Search for rental listings (Huur).
    /// </summary>
    public virtual async Task<FundaApiResponse?> SearchRentAsync(
        string geoInfo,
        int page = 1,
        int? minPrice = null,
        int? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        // Note: Rent requests also seem to accept "SalePrice" as the range type in this specific API,
        // or effectively ignore the type mismatch if the bounds are numeric.
        // We use the generic SearchAsync which defaults to SalePrice for now as it's proven to work.
        return await SearchAsync(
            geoInfo, 
            offeringType: "rent", 
            aggregationType: "listing", 
            page: page, 
            minPrice: minPrice, 
            maxPrice: maxPrice, 
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Search for new construction projects (Nieuwbouw).
    /// </summary>
    public virtual async Task<FundaApiResponse?> SearchProjectsAsync(
        string geoInfo,
        int page = 1,
        int? minPrice = null,
        int? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        return await SearchAsync(
            geoInfo, 
            offeringType: "buy", 
            aggregationType: "project", 
            page: page, 
            minPrice: minPrice, 
            maxPrice: maxPrice, 
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Generic search method for the Funda Topposition API.
    /// </summary>
    private async Task<FundaApiResponse?> SearchAsync(
        string geoInfo,
        string offeringType,
        string aggregationType,
        int page,
        int? minPrice,
        int? maxPrice,
        CancellationToken cancellationToken)
    {
        var request = new FundaApiRequest
        {
            AggregationType = [aggregationType],
            CultureInfo = "nl",
            GeoInformation = geoInfo.ToLowerInvariant(),
            OfferingType = [offeringType],
            Page = page,
            Price = new FundaApiPriceFilter
            {
                LowerBound = minPrice ?? 0,
                // Empirical testing shows "SalePrice" works for Rent/Projects too in this API
                PriceRangeType = "SalePrice",
                UpperBound = maxPrice
            },
            Zoning = ["residential"]
        };

        _logger.LogDebug("Fetching {Aggregation} ({Offering}) from Funda API for {GeoInfo}, page {Page}",
            aggregationType, offeringType, geoInfo, page);

        var response = await _httpClient.PostAsJsonAsync(ToppositionApiUrl, request, cancellationToken);

        // Throw on error so Polly can handle retries
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            var result = JsonSerializer.Deserialize<FundaApiResponse>(content);
            _logger.LogDebug("Funda API returned {Count} items", result?.Listings?.Count ?? 0);
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Funda API response");
            return null;
        }
    }
    
    /// <summary>
    /// Search multiple pages and aggregate results for residential buy listings.
    /// </summary>
    public virtual async Task<List<FundaApiListing>> SearchAllBuyPagesAsync(
        string geoInfo,
        int maxPages = 5,
        CancellationToken cancellationToken = default)
    {
        return await AggregatePagesAsync(
            page => SearchBuyAsync(geoInfo, page, cancellationToken: cancellationToken),
            maxPages);
    }

    /// <summary>
    /// Helper to aggregate multiple pages of results.
    /// </summary>
    private async Task<List<FundaApiListing>> AggregatePagesAsync(
        Func<int, Task<FundaApiResponse?>> searchAction,
        int maxPages)
    {
        var allListings = new List<FundaApiListing>();
        
        for (var page = 1; page <= maxPages; page++)
        {
            try
            {
                var result = await searchAction(page);

                if (result?.Listings == null || result.Listings.Count == 0)
                {
                    break;
                }

                allListings.AddRange(result.Listings);

                // Small delay to be respectful
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch page {Page}", page);
            }
        }
        
        return allListings;
    }
    /// <summary>
    /// Fetches the details page HTML and extracts the Nuxt hydration state to get rich data.
    /// This includes full description, multiple photos, bathrooms, etc.
    /// </summary>
    public virtual async Task<FundaNuxtListingData?> GetListingDetailsAsync(string url, CancellationToken cancellationToken = default)
    {
        // 1. Fetch HTML
        var html = await GetListingDetailHtmlAsync(url, cancellationToken);
        if (string.IsNullOrEmpty(html)) return null;

        // 2. Extract JSON content from script
        // Look for <script type="application/json" ...> that *looks* like it has the data
        // The Nuxt script typically starts with [ followed by some meta keys
        var jsonContent = ExtractNuxtJson(html);
        if (string.IsNullOrEmpty(jsonContent))
        {
            _logger.LogWarning("Could not find Nuxt state JSON in page: {Url}", url);
            return null;
        }

        // 3. Parse and find the listing data node
        return ParseNuxtState(jsonContent);
    }
    
    private async Task<string> GetListingDetailHtmlAsync(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        // Ensure we have a valid Absolute URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
             // Try to construct if it's relative? Funda usually gives us full URLs.
             if (url.StartsWith("/")) url = "https://www.funda.nl" + url;
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// Extracts the JSON content from the Nuxt hydration script tag.
    /// <para>
    /// <strong>Strategy:</strong>
    /// Funda uses Nuxt.js, which hydrates the client-side state via a `<script type="application/json">` tag.
    /// Instead of using a greedy regex (which is prone to catastrophic backtracking on large HTML),
    /// we iterate over all matching script tags and inspect their content for known keywords
    /// like "cachedListingData" or "features" + "media".
    /// </para>
    /// </summary>
    private string? ExtractNuxtJson(string html)
    {
        // Simple regex to find the script content. 
        // We look for script type="application/json" and iterate over them to find the one with the data.
        // This is safer than a greedy regex which might capture multiple script tags.

        var matches = NuxtScriptRegex().Matches(html);
        foreach (System.Text.RegularExpressions.Match m in matches)
        {
             var content = m.Groups[1].Value;
             // Check for key identifiers of the Nuxt hydration state
             if (content.Contains("cachedListingData") || (content.Contains("features") && content.Contains("media")))
             {
                 return content;
             }
        }

        return null;
    }

    private FundaNuxtListingData? ParseNuxtState(string json)
    {
        return FundaNuxtJsonParser.Parse(json, _logger);
    }

    [GeneratedRegex(@"<script type=""application/json""[^>]*>(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex NuxtScriptRegex();
}
