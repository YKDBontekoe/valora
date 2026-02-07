using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

/// <summary>
/// Service for on-demand Funda searches with cache-through pattern.
/// Fetches from Funda API when cache is stale, stores results in database.
/// </summary>
public partial class FundaSearchService : IFundaSearchService
{
    private readonly IFundaApiClient _apiClient;
    private readonly IListingRepository _listingRepository;
    private readonly IListingService _listingService;
    private readonly ILogger<FundaSearchService> _logger;
    private readonly TimeSpan _cacheFreshness;
    private readonly TimeSpan _searchCacheFreshness;
    
    // In-memory cache for tracking when regions were last searched
    private static readonly ConcurrentDictionary<string, DateTime> _regionSearchCache = new();

    public FundaSearchService(
        IFundaApiClient apiClient,
        IListingRepository listingRepository,
        IListingService listingService,
        IConfiguration configuration,
        ILogger<FundaSearchService> logger)
    {
        _apiClient = apiClient;
        _listingRepository = listingRepository;
        _listingService = listingService;
        _logger = logger;
        
        _cacheFreshness = TimeSpan.FromMinutes(
            configuration.GetValue<int>("CACHE_FRESHNESS_MINUTES", 60));
        _searchCacheFreshness = TimeSpan.FromMinutes(
            configuration.GetValue<int>("SEARCH_CACHE_MINUTES", 15));
    }

    public async Task<FundaSearchResult> SearchAsync(FundaSearchQuery query, CancellationToken ct = default)
    {
        var normalizedRegion = query.Region.ToLowerInvariant().Trim();
        _logger.LogInformation("Dynamic search for region: {Region}, offering: {Type}", 
            normalizedRegion, query.OfferingType);

        var fromCache = true;
        
        if (ShouldRefreshSearch(normalizedRegion))
        {
            fromCache = false;
            await FetchAndStoreListingsAsync(normalizedRegion, query, ct);
            _regionSearchCache[normalizedRegion] = DateTime.UtcNow;
        }

        var listings = await QueryDatabaseAsync(query, ct);
        
        return new FundaSearchResult
        {
            Items = listings,
            TotalCount = listings.Count,
            Page = query.Page,
            PageSize = query.PageSize,
            FromCache = fromCache
        };
    }

    public async Task<Listing?> GetByFundaUrlAsync(string fundaUrl, CancellationToken ct = default)
    {
        var globalId = ExtractGlobalIdFromUrl(fundaUrl);
        if (globalId == null)
        {
            _logger.LogWarning("Could not extract GlobalId from URL: {Url}", fundaUrl);
            return null;
        }

        var fundaIdStr = globalId.Value.ToString();
        
        var existing = await _listingRepository.GetByFundaIdAsync(fundaIdStr, ct);
        
        if (existing != null && IsFresh(existing))
        {
            _logger.LogDebug("Returning cached listing for {FundaId}", fundaIdStr);
            return existing;
        }

        _logger.LogInformation("Fetching fresh data for listing {FundaId}", fundaIdStr);
        
        try
        {
            var summary = await _apiClient.GetListingSummaryAsync(globalId.Value, ct);
            if (summary == null)
            {
                _logger.LogWarning("Listing {FundaId} not found on Funda", fundaIdStr);
                return existing;
            }

            FundaNuxtListingData? richData = null;
            if (!string.IsNullOrEmpty(fundaUrl))
            {
                try
                {
                    richData = await _apiClient.GetListingDetailsAsync(fundaUrl, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch rich details for {FundaId}", fundaIdStr);
                }
            }

            // Create a fresh transient listing object from API data
            var newListing = new Listing
            {
                FundaId = fundaIdStr,
                Address = summary.Address?.Street ?? "Unknown Address",
                Url = fundaUrl,
                LastFundaFetchUtc = DateTime.UtcNow
            };

            UpdateListingFromSummary(newListing, summary);
            
            if (richData != null)
            {
                FundaMapper.EnrichListingWithNuxtData(newListing, richData);
            }

            if (existing == null)
            {
                await _listingService.CreateListingAsync(newListing, ct);
                return newListing;
            }
            else
            {
                await _listingService.UpdateListingAsync(existing, newListing, ct);
                return existing;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch listing {FundaId} from Funda", fundaIdStr);
            return existing;
        }
    }

    private bool ShouldRefreshSearch(string region)
    {
        if (!_regionSearchCache.TryGetValue(region, out var lastSearch))
        {
            return true;
        }
        
        return DateTime.UtcNow - lastSearch > _searchCacheFreshness;
    }

    private bool IsFresh(Listing listing)
    {
        if (!listing.LastFundaFetchUtc.HasValue)
        {
            return false;
        }
        
        return DateTime.UtcNow - listing.LastFundaFetchUtc.Value < _cacheFreshness;
    }

    private async Task FetchAndStoreListingsAsync(string region, FundaSearchQuery query, CancellationToken ct)
    {
        _logger.LogInformation("Fetching fresh listings from Funda for region: {Region}", region);

        var apiListings = query.OfferingType.ToLowerInvariant() switch
        {
            "rent" => await _apiClient.SearchRentAsync(region, query.Page, query.MinPrice, query.MaxPrice, ct),
            "project" => await _apiClient.SearchProjectsAsync(region, query.Page, query.MinPrice, query.MaxPrice, ct),
            _ => await _apiClient.SearchBuyAsync(region, query.Page, query.MinPrice, query.MaxPrice, ct)
        };

        if (apiListings?.Listings == null || apiListings.Listings.Count == 0)
        {
            _logger.LogWarning("No listings found from Funda API for region: {Region}", region);
            return;
        }

        _logger.LogInformation("Found {Count} listings from Funda API", apiListings.Listings.Count);

        foreach (var apiListing in apiListings.Listings)
        {
            if (apiListing.GlobalId <= 0 || string.IsNullOrEmpty(apiListing.ListingUrl))
            {
                continue;
            }

            try
            {
                await ProcessApiListingAsync(apiListing, ct);
                
                await Task.Delay(100, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process listing {GlobalId}", apiListing.GlobalId);
            }
        }
    }

    private async Task ProcessApiListingAsync(FundaApiListing apiListing, CancellationToken ct)
    {
        var fundaId = apiListing.GlobalId.ToString();
        var existing = await _listingRepository.GetByFundaIdAsync(fundaId, ct);

        if (existing != null && IsFresh(existing))
        {
            return;
        }

        var fullUrl = apiListing.ListingUrl!.StartsWith("http")
            ? apiListing.ListingUrl
            : $"https://www.funda.nl{apiListing.ListingUrl}";

        // Create fresh listing object
        var newListing = new Listing
        {
            FundaId = fundaId,
            Address = apiListing.Address?.ListingAddress ?? apiListing.Address?.City ?? "Unknown Address",
            City = apiListing.Address?.City,
            AgentName = apiListing.AgentName,
            ImageUrl = apiListing.Image?.Default,
            Price = ParsePrice(apiListing.Price),
            PropertyType = apiListing.IsProject ? "Nieuwbouwproject" : "Woonhuis",
            Url = fullUrl,
            LastFundaFetchUtc = DateTime.UtcNow
        };

        try
        {
            var richData = await _apiClient.GetListingDetailsAsync(fullUrl, ct);
            if (richData != null)
            {
                FundaMapper.EnrichListingWithNuxtData(newListing, richData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not fetch rich details for {FundaId}", fundaId);
        }

        if (existing == null)
        {
            await _listingService.CreateListingAsync(newListing, ct);
            _logger.LogDebug("Added new listing: {FundaId}", fundaId);
        }
        else
        {
            await _listingService.UpdateListingAsync(existing, newListing, ct);
            _logger.LogDebug("Updated listing: {FundaId}", fundaId);
        }
    }

    private async Task<List<Listing>> QueryDatabaseAsync(FundaSearchQuery query, CancellationToken ct)
    {
        return await _listingRepository.GetByCityAsync(
            query.Region,
            query.MinPrice,
            query.MaxPrice,
            query.MinBedrooms,
            query.PageSize,
            query.Page,
            ct);
    }

    private static int? ExtractGlobalIdFromUrl(string url)
    {
        var match = GlobalIdRegex().Match(url);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var id))
        {
            return id;
        }
        return null;
    }

    private static decimal? ParsePrice(string? priceText)
    {
        if (string.IsNullOrEmpty(priceText)) return null;
        var cleaned = PriceCleanupRegex().Replace(priceText, "");
        if (decimal.TryParse(cleaned, out var price) && price > 0)
        {
            return price;
        }
        return null;
    }

    private static void UpdateListingFromSummary(Listing listing, FundaApiListingSummary summary)
    {
        if (summary.Address != null)
        {
            listing.Address = summary.Address.Street ?? listing.Address;
            listing.City = summary.Address.City;
            listing.PostalCode = summary.Address.PostalCode;
        }

        if (summary.Price != null)
        {
            listing.Price = ParsePrice(summary.Price.SellingPrice);
        }

        if (summary.FastView != null)
        {
            if (!string.IsNullOrEmpty(summary.FastView.LivingArea))
            {
                var match = NumberRegex().Match(summary.FastView.LivingArea);
                if (match.Success && int.TryParse(match.Value, out var area))
                {
                    listing.LivingAreaM2 = area;
                }
            }

            if (!string.IsNullOrEmpty(summary.FastView.NumberOfBedrooms) && 
                int.TryParse(summary.FastView.NumberOfBedrooms, out var bedrooms))
            {
                listing.Bedrooms = bedrooms;
            }

            listing.EnergyLabel = summary.FastView.EnergyLabel;
        }

        if (summary.Brokers.Count > 0)
        {
            listing.AgentName = summary.Brokers[0].Name;
        }

        listing.PublicationDate = summary.PublicationDate;
        listing.IsSoldOrRented = summary.IsSoldOrRented;
        
        if (summary.Labels.Count > 0)
        {
            listing.Labels = summary.Labels.Select(l => l.Text ?? "").Where(t => !string.IsNullOrEmpty(t)).ToList();
        }
    }

    [GeneratedRegex(@"/(\d{6,})", RegexOptions.IgnoreCase)]
    private static partial Regex GlobalIdRegex();

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex PriceCleanupRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();
}
