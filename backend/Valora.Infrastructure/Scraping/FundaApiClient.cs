using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

/// <summary>
/// Client for Funda's publicly accessible APIs.
/// Uses the Topposition API which is more reliable than HTML scraping
/// and doesn't require bypassing anti-bot measures.
/// </summary>
// <summary>
/// Client for Funda's publicly accessible APIs.
/// Uses the Topposition API to discover listings for:
/// - Residential Buy (Koop)
/// - Residential Rent (Huur)
/// - New Developments (Nieuwbouw)
/// </summary>
public class FundaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FundaApiClient> _logger;
    
    private const string ToppositionApiUrl = "https://search-topposition.funda.io/v2.0/search";
    private const string ListingSummaryApiUrlTemplate = "https://listing-detail-summary.funda.io/api/v1/listing/nl/{0}";
    private const string SimilarListingsApiUrlTemplate = "https://local-listings.funda.io/api/v1/similarlistings?globalid={0}";
    private const string ContactDetailsApiUrlTemplate = "https://contacts-bff.funda.io/api/v3/listings/{0}/contact-details?website=1";
    private const string FiberApiUrlTemplate = "https://kpnopticfiber.funda.io/api/v1/{0}";
    
    public FundaApiClient(HttpClient httpClient, ILogger<FundaApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        ConfigureHttpClient();
    }
    
    /// <summary>
    /// Fetches a detailed summary of a specific listing using its Global ID.
    /// Includes address, price, energy label, and broker info.
    /// </summary>
    public virtual async Task<FundaApiListingSummary?> GetListingSummaryAsync(int globalId, CancellationToken cancellationToken = default)
    {
        var url = string.Format(ListingSummaryApiUrlTemplate, globalId);
        // Exception handling is delegated to the caller/Polly to allow for retries and proper failure reporting.
        return await _httpClient.GetFromJsonAsync<FundaApiListingSummary>(url, cancellationToken);
    }

    /// <summary>
    /// Fetches similar listings (recommendations) for a given listing.
    /// Useful for graph-based crawling / discovery.
    /// </summary>
    public virtual async Task<FundaApiSimilarListingsResponse?> GetSimilarListingsAsync(int globalId, CancellationToken cancellationToken = default)
    {
        var url = string.Format(SimilarListingsApiUrlTemplate, globalId);
        var response = await _httpClient.GetAsync(url, cancellationToken);

        // Throw on error so Polly can handle retries
        response.EnsureSuccessStatusCode();

        // The API returns a JSON array of objects if multiple, but here it returns an object with arrays. verified via curl.
        return await response.Content.ReadFromJsonAsync<FundaApiSimilarListingsResponse>(cancellationToken);
    }

    /// <summary>
    /// Fetches contact details for a listing's broker/agent.
    /// Returns broker phone number, logo, and association code (NVM/VBO).
    /// </summary>
    public virtual async Task<FundaContactDetailsResponse?> GetContactDetailsAsync(int globalId, CancellationToken cancellationToken = default)
    {
        var url = string.Format(ContactDetailsApiUrlTemplate, globalId);
        try
        {
            return await _httpClient.GetFromJsonAsync<FundaContactDetailsResponse>(url, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Contact details not available for all listings
            _logger.LogDebug("Contact details not found for listing {GlobalId}", globalId);
            return null;
        }
    }

    /// <summary>
    /// Checks fiber optic availability at the given postal code.
    /// Requires FULL postal code (e.g., "1096DE") not just prefix.
    /// </summary>
    public virtual async Task<FundaFiberResponse?> GetFiberAvailabilityAsync(string postalCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postalCode) || postalCode.Length < 6)
            return null;
            
        // Remove spaces and ensure uppercase (e.g., "1096 DE" -> "1096DE")
        var cleanPostalCode = postalCode.Replace(" ", "").ToUpperInvariant();
        var url = string.Format(FiberApiUrlTemplate, cleanPostalCode);
        
        try
        {
            return await _httpClient.GetFromJsonAsync<FundaFiberResponse>(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "Failed to check fiber availability for postal code {PostalCode}", cleanPostalCode);
            return null;
        }
    }
    
    private void ConfigureHttpClient()
    {
        // Set browser-like headers for the API requests
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "nl-NL,nl;q=0.9,en;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Origin", "https://www.funda.nl");
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://www.funda.nl/");
    }
    
    /// <summary>
    /// Search for residential buy listings (Koop).
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
    /// Search for residential rent listings (Huur).
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
        var result = JsonSerializer.Deserialize<FundaApiResponse>(content);

        _logger.LogDebug("Funda API returned {Count} items", result?.Listings?.Count ?? 0);

        return result;
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

    private string? ExtractNuxtJson(string html)
    {
        // Simple regex to find the script content. 
        // We look for script type="application/json" and rely on the fact that the Nuxt blob is usually the largest one.
        // Or we can look for specific identifying text like "cachedListingData" inside the tag.
        var pattern = @"<script type=""application/json""[^>]*>(.*?cachedListingData.*?)</script>";
        var match = System.Text.RegularExpressions.Regex.Match(html, pattern, System.Text.RegularExpressions.RegexOptions.Singleline);
        
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        
        // Fallback: finding any application/json script and checking valid JSON
        var multiPattern = @"<script type=""application/json""[^>]*>(.*?)</script>";
        var matches = System.Text.RegularExpressions.Regex.Matches(html, multiPattern, System.Text.RegularExpressions.RegexOptions.Singleline);
        foreach (System.Text.RegularExpressions.Match m in matches)
        {
             var content = m.Groups[1].Value;
             if (content.Contains("cachedListingData") || content.Contains("features") && content.Contains("media"))
             {
                 return content;
             }
        }

        return null;
    }

    private FundaNuxtListingData? ParseNuxtState(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            // The root is usually an array (Nuxt 3 devalue)
            // We need to walk the tree to find an object that has "features", "media", "description".
            
            // Breadth-first search for the node
            var queue = new Queue<JsonElement>();
            queue.Enqueue(doc.RootElement);

            int safetyCounter = 0;
            while (queue.Count > 0 && safetyCounter++ < 10000)
            {
                var current = queue.Dequeue();

                if (current.ValueKind == JsonValueKind.Object)
                {
                    // Check if this is our candidate
                    if (current.TryGetProperty("features", out _) && 
                        current.TryGetProperty("media", out _) && 
                        current.TryGetProperty("description", out _))
                    {
                        return current.Deserialize<FundaNuxtListingData>();
                    }

                    foreach (var prop in current.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            queue.Enqueue(prop.Value);
                        }
                    }
                }
                else if (current.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in current.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object || item.ValueKind == JsonValueKind.Array)
                        {
                            queue.Enqueue(item);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Nuxt state JSON");
        }

        return null;
    }
}

/// <summary>
/// Request body for the Funda Topposition API.
/// </summary>
internal record FundaApiRequest
{
    public List<string> AggregationType { get; init; } = [];
    public string CultureInfo { get; init; } = "nl";
    public string GeoInformation { get; init; } = "";
    public List<string> OfferingType { get; init; } = [];
    public int Page { get; init; } = 1;
    public FundaApiPriceFilter? Price { get; init; }
    public List<string> Zoning { get; init; } = [];
}

/// <summary>
/// Price filter for the Funda API request.
/// </summary>
internal record FundaApiPriceFilter
{
    public int LowerBound { get; init; }
    public string PriceRangeType { get; init; } = "SalePrice";
    public int? UpperBound { get; init; }
}
