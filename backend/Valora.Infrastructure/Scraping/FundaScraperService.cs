using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

public class FundaScraperService : IFundaScraperService
{
    private const string DefaultStatus = "Beschikbaar";

    private readonly IListingRepository _listingRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly FundaApiClient _apiClient;
    private readonly ScraperOptions _options;
    private readonly ILogger<FundaScraperService> _logger;
    private readonly IScraperNotificationService _notificationService;

    public FundaScraperService(
        IListingRepository listingRepository,
        IPriceHistoryRepository priceHistoryRepository,
        IOptions<ScraperOptions> options,
        ILogger<FundaScraperService> logger,
        IScraperNotificationService notificationService,
        FundaApiClient apiClient)
    {
        _listingRepository = listingRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _apiClient = apiClient;
        _options = options.Value;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task ScrapeAndStoreAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting funda.nl scrape job (API only)");

        foreach (var searchUrl in _options.SearchUrls)
        {
            try
            {
                await ScrapeSearchUrlAsync(searchUrl, null, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scrape search URL: {Url}", searchUrl);
            }
        }

        _logger.LogInformation("Funda.nl scrape job completed");
    }

    public async Task ScrapeLimitedAsync(string region, int limit, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting limited scrape for region: {Region}", region);
        await _notificationService.NotifyProgressAsync($"Starting search for {region}...");

        try
        {
            // Construct basic search URL for the region
            var searchUrl = $"https://www.funda.nl/koop/{region}/";
            await ScrapeSearchUrlAsync(searchUrl, limit, cancellationToken);

            await _notificationService.NotifyCompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed limited scrape for region: {Region}", region);
            await NotifyScrapingErrorAsync(ex.Message);
            throw;
        }
    }

    private async Task ScrapeSearchUrlAsync(string searchUrl, int? limit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scraping search URL: {Url}", searchUrl);
        bool shouldNotify = limit.HasValue;

        if (shouldNotify)
        {
            await _notificationService.NotifyProgressAsync("Fetching search results...");
        }

        // Try to extract region from URL for API-based search
        var region = FundaUrlParser.ExtractRegionFromUrl(searchUrl);
        
        if (string.IsNullOrEmpty(region))
        {
            _logger.LogWarning("Could not extract region from URL: {Url}", searchUrl);
            return;
        }

        _logger.LogDebug("Searching API for region: {Region}", region);
        var apiListings = await FetchFromApiAsync(region, limit, cancellationToken);

        if (apiListings.Count == 0)
        {
            _logger.LogWarning("No listings found from API for: {Region}", region);
            if (shouldNotify)
            {
                await NotifyScrapingErrorAsync("No results found.");
            }
            return;
        }

        _logger.LogInformation("Found {Count} listings via API", apiListings.Count);
        if (shouldNotify)
        {
            await _notificationService.NotifyProgressAsync($"Found {apiListings.Count} listings. Processing...");
        }

        // Optimization: Fetch all existing listings in one query to avoid N+1
        var existingListingsMap = await GetExistingListingsMapAsync(apiListings, cancellationToken);

        foreach (var apiListing in apiListings)
        {
            try
            {
                existingListingsMap.TryGetValue(apiListing.GlobalId.ToString(), out var existingListing);
                await ProcessListingAsync(apiListing, existingListing, shouldNotify, cancellationToken);
                
                // Rate limiting delay
                await Task.Delay(_options.DelayBetweenRequestsMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process listing: {GlobalId}", apiListing.GlobalId);
            }
        }
    }
    
    private async Task<Dictionary<string, Listing>> GetExistingListingsMapAsync(List<FundaApiListing> apiListings, CancellationToken cancellationToken)
    {
        var fundaIds = apiListings.Select(l => l.GlobalId.ToString()).ToList();
        var existingListings = await _listingRepository.GetByFundaIdsAsync(fundaIds, cancellationToken);

        // Handle duplicates by taking the first one found
        return existingListings
            .GroupBy(l => l.FundaId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private async Task<List<FundaApiListing>> FetchFromApiAsync(string region, int? limit, CancellationToken cancellationToken)
    {
        var maxPages = limit.HasValue ? Math.Max(1, limit.Value / 10) : 3;
        var apiListings = await _apiClient.SearchAllBuyPagesAsync(region, maxPages, cancellationToken: cancellationToken);

        // Filter valid listings
        var validListings = apiListings
            .Where(l => !string.IsNullOrEmpty(l.ListingUrl) && l.GlobalId > 0)
            .ToList();

        if (limit.HasValue)
        {
            validListings = validListings.Take(limit.Value).ToList();
        }

        return validListings;
    }

    private async Task ProcessListingAsync(FundaApiListing apiListing, Listing? existingListing, bool shouldNotify, CancellationToken cancellationToken)
    {
        var fundaId = apiListing.GlobalId.ToString();

        var listing = FundaMapper.MapApiListingToDomain(apiListing, fundaId);

        await EnrichListingAsync(listing, apiListing, cancellationToken);

        if (existingListing == null)
        {
            await AddNewListingAsync(listing, shouldNotify, cancellationToken);
        }
        else
        {
            await UpdateExistingListingAsync(existingListing, listing, shouldNotify, cancellationToken);
        }
    }

    /// <summary>
    /// Orchestrates the enrichment of a basic listing with detailed data from multiple sources.
    /// <para>
    /// <strong>Why multiple steps?</strong>
    /// Funda's data is fragmented across different endpoints and HTML structures:
    /// 1. <strong>Summary API</strong>: Provides status (sold/rented), publication date, and accurate postal code.
    /// 2. <strong>Nuxt/HTML</strong>: Provides rich descriptions, media URLs, and extended features (energy label, year built).
    /// 3. <strong>Contact API</strong>: Provides broker details (phone, logo) which are dynamic.
    /// 4. <strong>Fiber API</strong>: Provides internet availability (requires full postal code from step 1).
    /// </para>
    /// <para>
    /// Each step is wrapped in a try-catch block to ensure that a failure in one auxiliary data source
    /// does not prevent the listing from being saved (Graceful Degradation).
    /// </para>
    /// </summary>
    private async Task EnrichListingAsync(Listing listing, FundaApiListing apiListing, CancellationToken cancellationToken)
    {
        await EnrichWithSummaryAsync(listing, apiListing.GlobalId, cancellationToken);
        await EnrichWithNuxtDataAsync(listing, cancellationToken);
        await EnrichWithContactDetailsAsync(listing, apiListing.GlobalId, cancellationToken);
        await EnrichWithFiberAsync(listing, cancellationToken);
    }

    private async Task EnrichWithSummaryAsync(Listing listing, int globalId, CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _apiClient.GetListingSummaryAsync(globalId, cancellationToken);
            if (summary != null)
            {
                FundaMapper.EnrichListingWithSummary(listing, summary);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch summary for {FundaId}", listing.FundaId);
        }
    }

    private async Task EnrichWithNuxtDataAsync(Listing listing, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(listing.Url)) return;

        try
        {
            var richData = await _apiClient.GetListingDetailsAsync(listing.Url, cancellationToken);
            if (richData != null)
            {
                FundaMapper.EnrichListingWithNuxtData(listing, richData);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch rich details for {FundaId}", listing.FundaId);
        }
    }

    private async Task EnrichWithContactDetailsAsync(Listing listing, int globalId, CancellationToken cancellationToken)
    {
        try
        {
            var contacts = await _apiClient.GetContactDetailsAsync(globalId, cancellationToken);
            if (contacts != null)
            {
                FundaMapper.EnrichListingWithContactDetails(listing, contacts);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch contact details for {FundaId}", listing.FundaId);
        }
    }

    private async Task EnrichWithFiberAsync(Listing listing, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(listing.PostalCode) && listing.PostalCode.Length >= 6)
        {
            try
            {
                var fiber = await _apiClient.GetFiberAvailabilityAsync(listing.PostalCode, cancellationToken);
                if (fiber != null)
                {
                    listing.FiberAvailable = fiber.Availability;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to check fiber availability for {FundaId}", listing.FundaId);
            }
        }
    }

    private async Task AddNewListingAsync(Listing listing, bool shouldNotify, CancellationToken cancellationToken)
    {
        // Set default status for new listings if not present
        listing.Status ??= DefaultStatus;

        // New listing - add it
        await _listingRepository.AddAsync(listing, cancellationToken);
        _logger.LogInformation("Added new listing: {FundaId} - {Address}", listing.FundaId, listing.Address);

        if (shouldNotify)
        {
            await NotifyMatchFoundAsync(listing.Address);
        }

        // Record initial price
        if (listing.Price.HasValue)
        {
            await _priceHistoryRepository.AddAsync(new PriceHistory
            {
                ListingId = listing.Id,
                Price = listing.Price.Value
            }, cancellationToken);
        }
    }

    private async Task UpdateExistingListingAsync(Listing existingListing, Listing listing, bool shouldNotify, CancellationToken cancellationToken)
    {
        // Existing listing - check for price changes
        var priceChanged = existingListing.Price != listing.Price && listing.Price.HasValue;

        if (priceChanged)
        {
            _logger.LogInformation(
                "Price changed for {FundaId}: {OldPrice} -> {NewPrice}",
                listing.FundaId, existingListing.Price, listing.Price);

            await _priceHistoryRepository.AddAsync(new PriceHistory
            {
                ListingId = existingListing.Id,
                Price = listing.Price!.Value
            }, cancellationToken);
        }

        // Update listing properties
        existingListing.Price = listing.Price;
        existingListing.ImageUrl = listing.ImageUrl;
        
        FundaMapper.MergeListingDetails(existingListing, listing);

        await _listingRepository.UpdateAsync(existingListing, cancellationToken);
        _logger.LogDebug("Updated listing: {FundaId}", listing.FundaId);

        if (shouldNotify)
        {
            await NotifyMatchFoundAsync($"{listing.Address} (Updated)");
        }
    }

    private async Task NotifyMatchFoundAsync(string address)
    {
        try
        {
            await _notificationService.NotifyListingFoundAsync(address);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send notification for listing: {Address}", address);
        }
    }

    private async Task NotifyScrapingErrorAsync(string message)
    {
        try
        {
            await _notificationService.NotifyErrorAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send error notification: {Message}", message);
        }
    }
}
