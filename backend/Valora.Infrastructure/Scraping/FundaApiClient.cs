using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

public partial class FundaApiClient : IFundaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FundaApiClient> _logger;

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
            var url = $"{FundaApiConstants.SummaryApiBaseUrl}{globalId}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();
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
            var url = $"{FundaApiConstants.ContactApiBaseUrl}{globalId}/contact-details?website=1";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();
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
            var url = $"{FundaApiConstants.FiberApiBaseUrl}{cleanPostalCode}";
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
            offeringType: FundaApiConstants.OfferingTypeBuy,
            aggregationType: FundaApiConstants.AggregationTypeListing,
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
        // API Quirk Explanation:
        // Even for rental listings ("offeringType": "rent"), the Funda Topposition API seems to expect
        // the price range type to be "SalePrice" or simply ignores the label as long as the bounds are numeric integers.
        // Sending "RentPrice" or similar often results in 0 matches or bad requests in this specific API version (v2).
        // Therefore, we reuse the generic SearchAsync which defaults to "SalePrice".
        return await SearchAsync(
            geoInfo, 
            offeringType: FundaApiConstants.OfferingTypeRent,
            aggregationType: FundaApiConstants.AggregationTypeListing,
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
            offeringType: FundaApiConstants.OfferingTypeBuy,
            aggregationType: FundaApiConstants.AggregationTypeProject,
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
            CultureInfo = FundaApiConstants.CultureNl,
            GeoInformation = geoInfo.ToLowerInvariant(),
            OfferingType = [offeringType],
            Page = page,
            Price = new FundaApiPriceFilter
            {
                LowerBound = minPrice ?? 0,
                // Empirical testing shows "SalePrice" works for Rent/Projects too in this API
                PriceRangeType = FundaApiConstants.PriceTypeSale,
                UpperBound = maxPrice
            },
            Zoning = [FundaApiConstants.ZoningResidential]
        };

        _logger.LogDebug("Fetching {Aggregation} ({Offering}) from Funda API for {GeoInfo}, page {Page}",
            aggregationType, offeringType, geoInfo, page);

        var response = await _httpClient.PostAsJsonAsync(FundaApiConstants.ToppositionApiUrl, request, cancellationToken);

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
    /// <para>
    /// <strong>Why parse Nuxt state instead of HTML scraping?</strong>
    /// Scraping raw HTML (e.g. finding divs with class 'listing-description') is brittle.
    /// Classes change frequently.
    /// Funda is a Nuxt.js app, which means it delivers the *entire* initial state as a JSON blob
    /// inside a specific `&lt;script&gt;` tag to "hydrate" the client-side Vue app.
    /// This JSON is a structured, typed source of truth. It's much more stable and contains
    /// data that isn't even rendered on screen (like hidden metadata).
    /// </para>
    /// </summary>
    public virtual async Task<FundaNuxtListingData?> GetListingDetailsAsync(string url, CancellationToken cancellationToken = default)
    {
        // 1. Fetch HTML
        var html = await GetListingDetailHtmlAsync(url, cancellationToken);
        if (string.IsNullOrEmpty(html)) return null;

        // 2. Extract JSON content from script
        // We look for the <script type="application/json"> tag that Nuxt uses for hydration.
        var jsonContent = FundaNuxtJsonParser.ExtractJsonFromHtml(html);
        if (string.IsNullOrEmpty(jsonContent))
        {
            _logger.LogWarning("Could not find Nuxt state JSON in page: {Url}", url);
            return null;
        }

        // 3. Parse and find the listing data node
        return FundaNuxtJsonParser.Parse(jsonContent, _logger);
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
}
