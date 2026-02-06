using System.Collections.Concurrent;
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
    private readonly FundaApiClient _apiClient;
    private readonly IListingRepository _listingRepository;
    private readonly ILogger<FundaSearchService> _logger;
    private readonly TimeSpan _cacheFreshness;
    private readonly TimeSpan _searchCacheFreshness;
    
    // In-memory cache for tracking when regions were last searched
    // Key: normalized region name, Value: last search time
    private static readonly ConcurrentDictionary<string, DateTime> _regionSearchCache = new();

    public FundaSearchService(
        FundaApiClient apiClient,
        IListingRepository listingRepository,
        IConfiguration configuration,
        ILogger<FundaSearchService> logger)
    {
        _apiClient = apiClient;
        _listingRepository = listingRepository;
        _logger = logger;
        
        // Read cache settings from environment variables with defaults
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
        
        // Check if we need to refresh data from Funda
        if (ShouldRefreshSearch(normalizedRegion))
        {
            fromCache = false;
            await FetchAndStoreListingsAsync(normalizedRegion, query, ct);
            _regionSearchCache[normalizedRegion] = DateTime.UtcNow;
        }

        // Query from database with filters
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
        // Extract GlobalId from URL
        var globalId = FundaUrlParser.ExtractGlobalIdFromUrl(fundaUrl);
        if (globalId == null)
        {
            _logger.LogWarning("Could not extract GlobalId from URL: {Url}", fundaUrl);
            return null;
        }

        var fundaIdStr = globalId.Value.ToString();
        
        // Check if we have a fresh cached version
        var existing = await _listingRepository.GetByFundaIdAsync(fundaIdStr, ct);
        
        if (existing != null && IsFresh(existing))
        {
            _logger.LogDebug("Returning cached listing for {FundaId}", fundaIdStr);
            return existing;
        }

        // Fetch fresh data from Funda
        _logger.LogInformation("Fetching fresh data for listing {FundaId}", fundaIdStr);
        
        try
        {
            var summary = await _apiClient.GetListingSummaryAsync(globalId.Value, ct);
            if (summary == null)
            {
                _logger.LogWarning("Listing {FundaId} not found on Funda", fundaIdStr);
                return existing; // Return stale data if we have it
            }

            // Get rich details
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

            // Create or update listing
            var listing = existing ?? new Listing
            {
                FundaId = fundaIdStr,
                Address = summary.Address?.Street ?? "Unknown Address",
            };

            // Update from summary
            FundaMapper.EnrichListingWithSummary(listing, summary);
            
            // Enrich with rich data if available
            if (richData != null)
            {
                FundaMapper.EnrichListingWithNuxtData(listing, richData);
            }

            listing.LastFundaFetchUtc = DateTime.UtcNow;
            listing.Url = fundaUrl;

            if (existing == null)
            {
                await _listingRepository.AddAsync(listing, ct);
            }
            else
            {
                await _listingRepository.UpdateAsync(listing, ct);
            }

            return listing;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch listing {FundaId} from Funda", fundaIdStr);
            return existing; // Return stale data if available
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
                
                // Small delay to be respectful to Funda
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

        // Skip if we have fresh data
        if (existing != null && IsFresh(existing))
        {
            return;
        }

        var newListingData = FundaMapper.MapApiListingToDomain(apiListing, fundaId);
        Listing listing;

        if (existing == null)
        {
            listing = newListingData;
        }
        else
        {
            listing = existing;
            // Update basic fields from API
            listing.Price = newListingData.Price;
            listing.ImageUrl = newListingData.ImageUrl;
            listing.Url = newListingData.Url;
            FundaMapper.MergeListingDetails(listing, newListingData);
        }

        // Try to get rich details (but don't fail if we can't)
        if (!string.IsNullOrEmpty(listing.Url))
        {
            try
            {
                var richData = await _apiClient.GetListingDetailsAsync(listing.Url, ct);
                if (richData != null)
                {
                    FundaMapper.EnrichListingWithNuxtData(listing, richData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not fetch rich details for {FundaId}", fundaId);
            }
        }

        listing.LastFundaFetchUtc = DateTime.UtcNow;

        if (existing == null)
        {
            await _listingRepository.AddAsync(listing, ct);
            _logger.LogDebug("Added new listing: {FundaId}", fundaId);
        }
        else
        {
            await _listingRepository.UpdateAsync(listing, ct);
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
}
