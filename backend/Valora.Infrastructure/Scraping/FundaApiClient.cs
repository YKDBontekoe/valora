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
        try
        {
            return await _httpClient.GetFromJsonAsync<FundaApiListingSummary>(url, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch listing summary for {GlobalId}", globalId);
            return null;
        }
    }

    /// <summary>
    /// Fetches similar listings (recommendations) for a given listing.
    /// Useful for graph-based crawling / discovery.
    /// </summary>
    public virtual async Task<FundaApiSimilarListingsResponse?> GetSimilarListingsAsync(int globalId, CancellationToken cancellationToken = default)
    {
        var url = string.Format(SimilarListingsApiUrlTemplate, globalId);
        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            
            // The API returns a JSON array of objects if multiple, but here it returns an object with arrays. verified via curl.
            return await response.Content.ReadFromJsonAsync<FundaApiSimilarListingsResponse>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch similar listings for {GlobalId}", globalId);
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
        try
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
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Funda API request failed with status {StatusCode}. Body: {Body}", 
                    response.StatusCode, errorContent);
                return null;
            }
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<FundaApiResponse>(content);
            
            _logger.LogDebug("Funda API returned {Count} items", result?.Listings?.Count ?? 0);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from Funda API for {GeoInfo}", geoInfo);
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
            var result = await searchAction(page);
            
            if (result?.Listings == null || result.Listings.Count == 0)
            {
                break;
            }
            
            allListings.AddRange(result.Listings);
            
            // Small delay to be respectful
            await Task.Delay(500);
        }
        
        return allListings;
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
