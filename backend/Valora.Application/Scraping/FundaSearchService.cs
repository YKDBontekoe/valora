using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping.Interfaces;
using Valora.Application.Scraping.Utils;
using Valora.Domain.Entities;

namespace Valora.Application.Scraping;

/// <summary>
/// Service for on-demand Funda searches with cache-through pattern.
/// Fetches from Funda API when cache is stale, stores results in database.
/// </summary>
public class FundaSearchService : IFundaSearchService
{
    private readonly IFundaApiClient _apiClient;
    private readonly IListingRepository _listingRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FundaSearchService> _logger;
    private readonly TimeSpan _cacheFreshness;
    private readonly TimeSpan _searchCacheFreshness;

    public FundaSearchService(
        IFundaApiClient apiClient,
        IListingRepository listingRepository,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<FundaSearchService> logger)
    {
        _apiClient = apiClient;
        _listingRepository = listingRepository;
        _cache = cache;
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
            _cache.Set(GetRegionCacheKey(normalizedRegion), DateTime.UtcNow, _searchCacheFreshness);
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
            var summaryListing = await _apiClient.GetListingSummaryAsync(globalId.Value, ct);
            if (summaryListing == null)
            {
                _logger.LogWarning("Listing {FundaId} not found on Funda", fundaIdStr);
                return existing; // Return stale data if we have it
            }

            // Get rich details
            Listing? richListing = null;
            if (!string.IsNullOrEmpty(fundaUrl))
            {
                try
                {
                    richListing = await _apiClient.GetListingDetailsAsync(fundaUrl, ct);
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
                Address = summaryListing.Address ?? "Unknown Address",
            };

            // Update from summary
            listing.UpdateFrom(summaryListing);

            // Enrich with rich data if available
            if (richListing != null)
            {
                listing.UpdateFrom(richListing);
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
        if (!_cache.TryGetValue(GetRegionCacheKey(region), out DateTime lastSearch))
        {
            return true;
        }

        return DateTime.UtcNow - lastSearch > _searchCacheFreshness;
    }

    private static string GetRegionCacheKey(string region) => $"Search_{region}";

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

        var offeringType = query.OfferingType?.ToLowerInvariant() ?? "buy";

        var apiListings = offeringType switch
        {
            "rent" => await _apiClient.SearchRentAsync(region, query.Page, query.MinPrice, query.MaxPrice, ct),
            "project" => await _apiClient.SearchProjectsAsync(region, query.Page, query.MinPrice, query.MaxPrice, ct),
            _ => await _apiClient.SearchBuyAsync(region, query.Page, query.MinPrice, query.MaxPrice, ct)
        };

        if (apiListings == null || apiListings.Count == 0)
        {
            _logger.LogWarning("No listings found from Funda API for region: {Region}", region);
            return;
        }

        _logger.LogInformation("Found {Count} listings from Funda API", apiListings.Count);

        foreach (var apiListing in apiListings)
        {
            if (string.IsNullOrEmpty(apiListing.FundaId) || string.IsNullOrEmpty(apiListing.Url))
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
                _logger.LogError(ex, "Failed to process listing {FundaId}", apiListing.FundaId);
            }
        }
    }

    private async Task ProcessApiListingAsync(Listing apiListing, CancellationToken ct)
    {
        var fundaId = apiListing.FundaId;
        var existing = await _listingRepository.GetByFundaIdAsync(fundaId, ct);

        // Skip if we have fresh data
        if (existing != null && IsFresh(existing))
        {
            return;
        }

        var listing = existing ?? new Listing
        {
            FundaId = fundaId,
            Address = apiListing.Address ?? "Unknown Address",
        };

        // Update properties from the API listing
        // Note: apiListing here comes from Search*Async which returns mapped Listings
        listing.UpdateFrom(apiListing);
        listing.Url = apiListing.Url; // Ensure URL is set

        // Try to get rich details (but don't fail if we can't)
        try
        {
            var richListing = await _apiClient.GetListingDetailsAsync(listing.Url!, ct);
            if (richListing != null)
            {
                listing.UpdateFrom(richListing);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not fetch rich details for {FundaId}", fundaId);
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
