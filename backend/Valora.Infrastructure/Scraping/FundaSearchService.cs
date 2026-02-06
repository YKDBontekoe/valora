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
    private readonly ILogger<FundaSearchService> _logger;
    private readonly TimeSpan _cacheFreshness;
    private readonly TimeSpan _searchCacheFreshness;
    
    // In-memory cache for tracking when regions were last searched
    // Key: normalized region name, Value: last search time
    private static readonly ConcurrentDictionary<string, DateTime> _regionSearchCache = new();

    public FundaSearchService(
        IFundaApiClient apiClient,
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
        var globalId = ExtractGlobalIdFromUrl(fundaUrl);
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
            UpdateListingFromSummary(listing, summary);
            
            // Enrich with rich data if available
            if (richData != null)
            {
                EnrichListingWithNuxtData(listing, richData);
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

        var listing = existing ?? new Listing
        {
            FundaId = fundaId,
            Address = apiListing.Address?.ListingAddress ?? apiListing.Address?.City ?? "Unknown Address",
        };

        // Basic data from API response
        listing.City = apiListing.Address?.City;
        listing.AgentName = apiListing.AgentName;
        listing.ImageUrl = apiListing.Image?.Default;
        listing.Price = ParsePrice(apiListing.Price);
        listing.PropertyType = apiListing.IsProject ? "Nieuwbouwproject" : "Woonhuis";

        var fullUrl = apiListing.ListingUrl!.StartsWith("http")
            ? apiListing.ListingUrl
            : $"https://www.funda.nl{apiListing.ListingUrl}";
        listing.Url = fullUrl;

        // Try to get rich details (but don't fail if we can't)
        try
        {
            var richData = await _apiClient.GetListingDetailsAsync(fullUrl, ct);
            if (richData != null)
            {
                EnrichListingWithNuxtData(listing, richData);
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

    private static int? ExtractGlobalIdFromUrl(string url)
    {
        // URL format: https://www.funda.nl/detail/koop/amsterdam/appartement-.../43224373/
        // The GlobalId is typically the last numeric segment in the URL path
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

    // This method mirrors the one in FundaScraperService to maintain consistency
    private static void EnrichListingWithNuxtData(Listing listing, FundaNuxtListingData data)
    {
        // Description
        listing.Description = data.Description?.Content;

        // Features
        if (data.Features != null)
        {
            if (data.ObjectType?.PropertySpecification != null)
            {
                listing.LivingAreaM2 = data.ObjectType.PropertySpecification.SelectedArea;
                listing.PlotAreaM2 = data.ObjectType.PropertySpecification.SelectedPlotArea;
            }

            var featureMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (data.Features.Indeling != null) FlattenFeatures(data.Features.Indeling.KenmerkenList, featureMap);
            if (data.Features.Afmetingen != null) FlattenFeatures(data.Features.Afmetingen.KenmerkenList, featureMap);
            if (data.Features.Energie != null) FlattenFeatures(data.Features.Energie.KenmerkenList, featureMap);
            if (data.Features.Bouw != null) FlattenFeatures(data.Features.Bouw.KenmerkenList, featureMap);

            listing.Features = featureMap;

            // Extract specific fields from feature map
            if (!listing.LivingAreaM2.HasValue && featureMap.TryGetValue("Wonen", out var livingArea))
                listing.LivingAreaM2 = ParseFirstNumber(livingArea);
            
            if (!listing.PlotAreaM2.HasValue && featureMap.TryGetValue("Perceel", out var plotArea)) 
                listing.PlotAreaM2 = ParseFirstNumber(plotArea);

            if (featureMap.TryGetValue("Aantal kamers", out var rooms))
            {
                var bedroomMatch = BedroomRegex().Match(rooms);
                if (bedroomMatch.Success && int.TryParse(bedroomMatch.Groups[1].Value, out var bedrooms))
                {
                    listing.Bedrooms = bedrooms;
                }
                else
                {
                    listing.Bedrooms = ParseFirstNumber(rooms);
                }
            }

            if (featureMap.TryGetValue("Aantal badkamers", out var bathrooms))
                listing.Bathrooms = ParseFirstNumber(bathrooms);
            
            if (featureMap.TryGetValue("Energielabel", out var label)) listing.EnergyLabel = label.Trim();
            if (featureMap.TryGetValue("Bouwjaar", out var year)) listing.YearBuilt = ParseFirstNumber(year);
        }

        // Images
        if (data.Media?.Items != null)
        {
            listing.ImageUrls = data.Media.Items
                .Where(x => !string.IsNullOrEmpty(x.Id))
                .Select(x => $"https://cloud.funda.nl/valentina_media/{x.Id}_720.jpg")
                .ToList();
            
            if (listing.ImageUrls.Count > 0)
            {
                listing.ImageUrl = listing.ImageUrls[0];
            }
        }

        // Coordinates
        if (data.Coordinates != null)
        {
            listing.Latitude = data.Coordinates.Lat;
            listing.Longitude = data.Coordinates.Lng;
        }
    }

    private static void FlattenFeatures(List<FundaNuxtFeatureItem>? items, Dictionary<string, string> map)
    {
        if (items == null) return;

        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.Label))
            {
                if (!string.IsNullOrEmpty(item.Value))
                {
                    map.TryAdd(item.Label.Trim(), item.Value.Trim());
                }
                
                if (item.KenmerkenList != null && item.KenmerkenList.Count > 0)
                {
                    FlattenFeatures(item.KenmerkenList, map);
                }
            }
            else if (item.KenmerkenList != null)
            {
                FlattenFeatures(item.KenmerkenList, map);
            }
        }
    }

    private static int? ParseFirstNumber(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var match = NumberRegex().Match(text);
        if (match.Success && int.TryParse(match.Value, out var num))
        {
            return num;
        }
        return null;
    }

    [GeneratedRegex(@"/(\d{6,})", RegexOptions.IgnoreCase)]
    private static partial Regex GlobalIdRegex();

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex PriceCleanupRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"(\d+)\s*slaapkamer", RegexOptions.IgnoreCase)]
    private static partial Regex BedroomRegex();
}
