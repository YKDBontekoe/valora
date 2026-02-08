using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

public class FundaScraperService : IFundaScraperService
{
    private readonly IListingService _listingService;
    private readonly IFundaApiClient _apiClient;
    private readonly ScraperOptions _options;
    private readonly ILogger<FundaScraperService> _logger;
    private readonly IScraperNotificationService _notificationService;

    public FundaScraperService(
        IListingService listingService,
        IOptions<ScraperOptions> options,
        ILogger<FundaScraperService> logger,
        IScraperNotificationService notificationService,
        IFundaApiClient apiClient)
    {
        _listingService = listingService;
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

        // Optimization: Batch Fetching
        var fundaIds = apiListings.Select(l => l.GlobalId.ToString()).Distinct().ToList();
        var existingListings = await _listingService.GetListingsByFundaIdsAsync(fundaIds, cancellationToken);
        var existingListingsMap = existingListings.ToDictionary(l => l.FundaId, l => l);
        var processedIds = new HashSet<string>();

        foreach (var apiListing in apiListings)
        {
            var fundaId = apiListing.GlobalId.ToString();
            if (processedIds.Contains(fundaId)) continue;

            try
            {
                existingListingsMap.TryGetValue(fundaId, out var existingListing);
                await ProcessListingAsync(apiListing, existingListing, shouldNotify, cancellationToken);
                
                processedIds.Add(fundaId);

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

        await _listingService.SaveListingAsync(listing, existingListing, shouldNotify, cancellationToken);
    }

    /// <summary>
    /// Orchestrates the enrichment of a basic listing with detailed data from multiple sources.
    /// </summary>
    private async Task EnrichListingAsync(Listing listing, FundaApiListing apiListing, CancellationToken cancellationToken)
    {
        await EnrichWithSummaryApiAsync(listing, apiListing, cancellationToken);
        await EnrichWithNuxtDataAsync(listing, cancellationToken);
        await EnrichWithContactDetails(listing, apiListing, cancellationToken);
        await EnrichWithFiberAvailabilityAsync(listing, cancellationToken);
    }

    private async Task EnrichWithSummaryApiAsync(Listing listing, FundaApiListing apiListing, CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _apiClient.GetListingSummaryAsync(apiListing.GlobalId, cancellationToken);
            if (summary != null)
            {
                FundaMapper.EnrichListingWithSummary(listing, summary);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch summary for {FundaId}", listing.FundaId);
        }
    }

    private async Task EnrichWithNuxtDataAsync(Listing listing, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(listing.Url))
        {
            try 
            {
                var richData = await _apiClient.GetListingDetailsAsync(listing.Url, cancellationToken);
                if (richData != null)
                {
                    FundaMapper.EnrichListingWithNuxtData(listing, richData);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch rich details for {FundaId}", listing.FundaId);
            }
        }
    }

    private async Task EnrichWithContactDetails(Listing listing, FundaApiListing apiListing, CancellationToken cancellationToken)
    {
        try
        {
            var contacts = await _apiClient.GetContactDetailsAsync(apiListing.GlobalId, cancellationToken);
            if (contacts?.ContactDetails?.Count > 0)
            {
                var primary = contacts.ContactDetails[0];
                listing.BrokerOfficeId = primary.Id;
                listing.BrokerPhone = primary.PhoneNumber;
                listing.BrokerLogoUrl = primary.LogoUrl;
                listing.BrokerAssociationCode = primary.AssociationCode;
                // Update agent name if we have better info
                if (!string.IsNullOrEmpty(primary.DisplayName))
                {
                    listing.AgentName = primary.DisplayName;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch contact details for {FundaId}", listing.FundaId);
        }
    }

    private async Task EnrichWithFiberAvailabilityAsync(Listing listing, CancellationToken cancellationToken)
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
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to check fiber availability for {FundaId}", listing.FundaId);
            }
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
