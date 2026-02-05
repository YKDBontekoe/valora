using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Valora.Application.Scraping.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

public partial class FundaApiClient : IFundaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FundaApiClient> _logger;
    private const string ToppositionApiUrl = "https://www.funda.nl/api/topposition/v2/search";

    public FundaApiClient(HttpClient httpClient, ILogger<FundaApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // --- IFundaApiClient Implementation ---

    public async Task<List<Listing>> SearchBuyAsync(string region, int page = 1, int? minPrice = null, int? maxPrice = null, CancellationToken cancellationToken = default)
    {
        var response = await FetchSearchBuyAsync(region, page, minPrice, maxPrice, cancellationToken);
        return MapResponseToListings(response);
    }

    public async Task<List<Listing>> SearchRentAsync(string region, int page = 1, int? minPrice = null, int? maxPrice = null, CancellationToken cancellationToken = default)
    {
        var response = await FetchSearchRentAsync(region, page, minPrice, maxPrice, cancellationToken);
        return MapResponseToListings(response);
    }

    public async Task<List<Listing>> SearchProjectsAsync(string region, int page = 1, int? minPrice = null, int? maxPrice = null, CancellationToken cancellationToken = default)
    {
        var response = await FetchSearchProjectsAsync(region, page, minPrice, maxPrice, cancellationToken);
        return MapResponseToListings(response);
    }

    public async Task<List<Listing>> SearchAllBuyPagesAsync(string region, int maxPages = 5, CancellationToken cancellationToken = default)
    {
        var apiListings = await FetchAllBuyPagesAsync(region, maxPages, cancellationToken);
        return apiListings.Select(l => FundaMapper.MapApiListingToDomain(l, l.GlobalId.ToString())).ToList();
    }

    public async Task<Listing?> GetListingSummaryAsync(int globalId, CancellationToken cancellationToken = default)
    {
        var summary = await FetchListingSummaryAsync(globalId, cancellationToken);
        if (summary == null) return null;

        // Create a partial listing with the summary data
        var listing = new Listing
        {
            FundaId = globalId.ToString(),
            Address = "Unknown" // Placeholder, will be populated by enricher
        };
        FundaMapper.EnrichListingWithSummary(listing, summary);
        return listing;
    }

    public async Task<Listing?> GetListingDetailsAsync(string url, CancellationToken cancellationToken = default)
    {
        var richData = await FetchListingDetailsAsync(url, cancellationToken);
        if (richData == null) return null;

        var listing = new Listing
        {
            FundaId = "0", // Unknown context here
            Address = "Unknown"
        };
        FundaMapper.EnrichListingWithNuxtData(listing, richData);
        return listing;
    }

    public async Task<Listing?> GetContactDetailsAsync(int globalId, CancellationToken cancellationToken = default)
    {
        var contacts = await FetchContactDetailsAsync(globalId, cancellationToken);
        if (contacts?.ContactDetails == null || contacts.ContactDetails.Count == 0) return null;

        var primary = contacts.ContactDetails[0];
        return new Listing
        {
            FundaId = globalId.ToString(),
            Address = "Unknown",
            BrokerOfficeId = primary.Id,
            BrokerPhone = primary.PhoneNumber,
            BrokerLogoUrl = primary.LogoUrl,
            BrokerAssociationCode = primary.AssociationCode,
            AgentName = primary.DisplayName
        };
    }

    public async Task<bool?> CheckFiberAvailabilityAsync(string postalCode, CancellationToken cancellationToken = default)
    {
        var fiber = await FetchFiberAvailabilityAsync(postalCode, cancellationToken);
        return fiber?.Availability;
    }

    private List<Listing> MapResponseToListings(FundaApiResponse? response)
    {
        if (response?.Listings == null) return new List<Listing>();

        return response.Listings
            .Where(l => !string.IsNullOrEmpty(l.ListingUrl) && l.GlobalId > 0)
            .Select(l => FundaMapper.MapApiListingToDomain(l, l.GlobalId.ToString()))
            .ToList();
    }

    // --- Internal Infrastructure Methods (Fetch...) ---

    protected virtual async Task<FundaApiListingSummary?> FetchListingSummaryAsync(int globalId, CancellationToken cancellationToken = default)
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

    protected virtual async Task<FundaContactDetailsResponse?> FetchContactDetailsAsync(int globalId, CancellationToken cancellationToken = default)
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

    protected virtual async Task<FundaFiberResponse?> FetchFiberAvailabilityAsync(string postalCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postalCode)) return null;
        
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

    protected virtual async Task<FundaApiResponse?> FetchSearchBuyAsync(
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

    protected virtual async Task<FundaApiResponse?> FetchSearchRentAsync(
        string geoInfo,
        int page = 1,
        int? minPrice = null,
        int? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        return await SearchAsync(
            geoInfo, 
            offeringType: "rent", 
            aggregationType: "listing", 
            page: page, 
            minPrice: minPrice, 
            maxPrice: maxPrice, 
            cancellationToken: cancellationToken);
    }

    protected virtual async Task<FundaApiResponse?> FetchSearchProjectsAsync(
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
                PriceRangeType = "SalePrice",
                UpperBound = maxPrice
            },
            Zoning = ["residential"]
        };

        _logger.LogDebug("Fetching {Aggregation} ({Offering}) from Funda API for {GeoInfo}, page {Page}",
            aggregationType, offeringType, geoInfo, page);

        var response = await _httpClient.PostAsJsonAsync(ToppositionApiUrl, request, cancellationToken);
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
    
    protected virtual async Task<List<FundaApiListing>> FetchAllBuyPagesAsync(
        string geoInfo,
        int maxPages = 5,
        CancellationToken cancellationToken = default)
    {
        return await AggregatePagesAsync(
            page => FetchSearchBuyAsync(geoInfo, page, cancellationToken: cancellationToken),
            maxPages);
    }

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
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch page {Page}", page);
            }
        }
        
        return allListings;
    }

    protected virtual async Task<FundaNuxtListingData?> FetchListingDetailsAsync(string url, CancellationToken cancellationToken = default)
    {
        var html = await GetListingDetailHtmlAsync(url, cancellationToken);
        if (string.IsNullOrEmpty(html)) return null;

        var jsonContent = ExtractNuxtJson(html);
        if (string.IsNullOrEmpty(jsonContent))
        {
            _logger.LogWarning("Could not find Nuxt state JSON in page: {Url}", url);
            return null;
        }

        return ParseNuxtState(jsonContent);
    }
    
    private async Task<string> GetListingDetailHtmlAsync(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
             if (url.StartsWith("/")) url = "https://www.funda.nl" + url;
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private string? ExtractNuxtJson(string html)
    {
        var matches = NuxtScriptRegex().Matches(html);
        foreach (System.Text.RegularExpressions.Match m in matches)
        {
             var content = m.Groups[1].Value;
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
