using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

public class FundaScraperService : IFundaScraperService
{
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
        var region = ExtractRegionFromUrl(searchUrl);
        
        if (string.IsNullOrEmpty(region))
        {
            _logger.LogWarning("Could not extract region from URL: {Url}", searchUrl);
            return;
        }

        _logger.LogDebug("Searching API for region: {Region}", region);
        var apiListings = await TryFetchFromApiAsync(region, limit, cancellationToken);

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

        foreach (var apiListing in apiListings)
        {
            try
            {
                await ProcessListingAsync(apiListing, shouldNotify, cancellationToken);
                
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
    
    private async Task<List<FundaApiListing>> TryFetchFromApiAsync(string region, int? limit, CancellationToken cancellationToken)
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
    
    private static string? ExtractRegionFromUrl(string url)
    {
        // URL format: https://www.funda.nl/koop/amsterdam/ or https://www.funda.nl/zoeken/koop?selected_area=...
        var match = Regex.Match(url, @"funda\.nl/(?:koop|huur)/([^/]+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        
        // Try to extract from query string
        match = Regex.Match(url, @"selected_area=.*?""([^""]+)""", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        
        return null;
    }
    
    private static decimal? ParsePriceFromApi(string? priceText)
    {
        if (string.IsNullOrEmpty(priceText)) return null;
        
        // Remove currency symbol, periods (thousands separator), and suffixes like "k.k."
        var cleaned = Regex.Replace(priceText, @"[^\d]", "");
        if (decimal.TryParse(cleaned, out var price) && price > 0)
        {
            return price;
        }
        return null;
    }

    private async Task ProcessListingAsync(FundaApiListing apiListing, bool shouldNotify, CancellationToken cancellationToken)
    {
        var fundaId = apiListing.GlobalId.ToString();
        var fullUrl = apiListing.ListingUrl!.StartsWith("http") ? apiListing.ListingUrl : $"https://www.funda.nl{apiListing.ListingUrl}";

        // Check if listing already exists
        var existingListing = await _listingRepository.GetByFundaIdAsync(fundaId, cancellationToken);

        // Map API model to Domain entity
        // Note: API provides limited details compared to HTML scraping.
        // Missing: Bedrooms, LivingAreaM2, PlotAreaM2, PropertyType, Status (exact)
        var price = ParsePriceFromApi(apiListing.Price);
        
        var listing = new Listing
        {
            FundaId = fundaId,
            Address = apiListing.Address?.ListingAddress ?? apiListing.Address?.City ?? "Unknown Address",
            City = apiListing.Address?.City,
            PostalCode = null, // Not provided by API
            Price = price,
            Bedrooms = null, // Not provided by API
            Bathrooms = null,
            LivingAreaM2 = null, // Not provided by API
            PlotAreaM2 = null, // Not provided by API
            PropertyType = apiListing.IsProject ? "Nieuwbouwproject" : "Woonhuis", // Best guess
            Status = "Beschikbaar", // API usually returns available listings
            Url = fullUrl,
            ImageUrl = apiListing.Image?.Default
        };

        if (existingListing == null)
        {
            await AddNewListingAsync(listing, shouldNotify, cancellationToken);
        }
        else
        {
            await UpdateExistingListingAsync(existingListing, listing, shouldNotify, cancellationToken);
        }
    }

    private async Task AddNewListingAsync(Listing listing, bool shouldNotify, CancellationToken cancellationToken)
    {
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
        
        // We do NOT overwrite fields that might have been enriched manually or by previous scraper if they are null in the new source
        if (listing.Bedrooms.HasValue) existingListing.Bedrooms = listing.Bedrooms;
        if (listing.LivingAreaM2.HasValue) existingListing.LivingAreaM2 = listing.LivingAreaM2;
        if (listing.PlotAreaM2.HasValue) existingListing.PlotAreaM2 = listing.PlotAreaM2;
        if (!string.IsNullOrEmpty(listing.Status)) existingListing.Status = listing.Status;

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