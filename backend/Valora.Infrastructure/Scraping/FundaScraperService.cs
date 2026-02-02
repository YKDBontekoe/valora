using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

public partial class FundaScraperService : IFundaScraperService
{
    private const string DefaultStatus = "Beschikbaar";
    private const string ProjectType = "Nieuwbouwproject";
    private const string HouseType = "Woonhuis";

    private readonly IListingRepository _listingRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IRegionScrapeCursorRepository _cursorRepository;
    private readonly FundaApiClient _apiClient;
    private readonly ScraperOptions _options;
    private readonly ILogger<FundaScraperService> _logger;
    private readonly IScraperNotificationService _notificationService;

    public FundaScraperService(
        IListingRepository listingRepository,
        IPriceHistoryRepository priceHistoryRepository,
        IRegionScrapeCursorRepository cursorRepository,
        IOptions<ScraperOptions> options,
        ILogger<FundaScraperService> logger,
        IScraperNotificationService notificationService,
        FundaApiClient apiClient)
    {
        _listingRepository = listingRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _cursorRepository = cursorRepository;
        _apiClient = apiClient;
        _options = options.Value;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task ScrapeAndStoreAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting funda.nl scrape job (API only)");

        var regions = _options.SearchUrls
            .Select(ExtractRegionFromUrl)
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (regions.Count == 0)
        {
            _logger.LogWarning("No valid regions found in scraper search URLs.");
            return;
        }

        var remainingCalls = Math.Max(0, _options.MaxApiCallsPerRun);

        foreach (var region in regions)
        {
            if (remainingCalls <= 0)
            {
                break;
            }

            try
            {
                remainingCalls = await ScrapeRecentPagesAsync(region, remainingCalls, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed recent scrape for region: {Region}", region);
            }
        }

        foreach (var region in regions)
        {
            if (remainingCalls <= 0)
            {
                break;
            }

            try
            {
                remainingCalls = await ScrapeBackfillPagesAsync(region, remainingCalls, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed backfill scrape for region: {Region}", region);
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
            await _notificationService.NotifyProgressAsync("Fetching search results...");
            var remainingCalls = Math.Max(1, _options.MaxApiCallsPerRun);
            var collected = new List<FundaApiListing>();
            var maxPages = Math.Max(1, (int)Math.Ceiling(limit / 10d));

            for (var page = 1; page <= maxPages && remainingCalls > 0 && collected.Count < limit; page++)
            {
                var pageListings = await FetchListingsPageAsync(region, page, cancellationToken);
                remainingCalls--;

                if (pageListings.Count == 0)
                {
                    break;
                }

                collected.AddRange(pageListings);
            }

            var limitedListings = collected.Take(limit).ToList();
            await ProcessListingsAsync(limitedListings, shouldNotify: true, cancellationToken);

            await _notificationService.NotifyCompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed limited scrape for region: {Region}", region);
            await NotifyScrapingErrorAsync(ex.Message);
            throw;
        }
    }

    private async Task<List<FundaApiListing>> FetchListingsPageAsync(string region, int page, CancellationToken cancellationToken)
    {
        try
        {
            var response = _options.FocusOnNewConstruction
                ? await _apiClient.SearchProjectsAsync(region, page, cancellationToken: cancellationToken)
                : await _apiClient.SearchBuyAsync(region, page, cancellationToken: cancellationToken);

            var listings = response?.Listings ?? [];

            var filtered = listings
                .Where(l => !string.IsNullOrEmpty(l.ListingUrl) && l.GlobalId > 0)
                .ToList();

            if (_options.FocusOnNewConstruction)
            {
                filtered = filtered.Where(l => l.IsProject).ToList();
            }

            return filtered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch from Funda API");
            return [];
        }
    }
    
    private static string? ExtractRegionFromUrl(string url)
    {
        var decodedUrl = Uri.UnescapeDataString(url);

        // URL format: https://www.funda.nl/koop/amsterdam/ or https://www.funda.nl/zoeken/koop?selected_area=...
        var regionMatch = UrlRegionRegex().Match(decodedUrl);
        if (regionMatch.Success)
        {
            return regionMatch.Groups[1].Value;
        }
        
        // Try to extract from query string
        var queryMatch = QueryRegionRegex().Match(decodedUrl);
        if (queryMatch.Success)
        {
            return queryMatch.Groups[1].Value;
        }
        
        return null;
    }
    
    private static decimal? ParsePriceFromApi(string? priceText)
    {
        if (string.IsNullOrEmpty(priceText)) return null;
        
        // Remove currency symbol, periods (thousands separator), and suffixes like "k.k."
        var cleaned = PriceCleanupRegex().Replace(priceText, "");
        if (decimal.TryParse(cleaned, out var price) && price > 0)
        {
            return price;
        }
        return null;
    }

    private async Task<bool> ProcessListingAsync(FundaApiListing apiListing, bool shouldNotify, CancellationToken cancellationToken)
    {
        var fundaId = apiListing.GlobalId.ToString();
        var existingListing = await _listingRepository.GetByFundaIdAsync(fundaId, cancellationToken);

        var listing = MapApiListingToDomain(apiListing, fundaId);

        if (existingListing == null)
        {
            await AddNewListingAsync(listing, shouldNotify, cancellationToken);
            return true;
        }

        await UpdateExistingListingAsync(existingListing, listing, shouldNotify, cancellationToken);
        return false;
    }

    private static Listing MapApiListingToDomain(FundaApiListing apiListing, string fundaId)
    {
        var fullUrl = apiListing.ListingUrl!.StartsWith("http")
            ? apiListing.ListingUrl
            : $"https://www.funda.nl{apiListing.ListingUrl}";

        var price = ParsePriceFromApi(apiListing.Price);

        // Map API model to Domain entity
        // Note: API provides limited details compared to HTML scraping.
        // Missing: Bedrooms, LivingAreaM2, PlotAreaM2, PropertyType, Status (exact)
        return new Listing
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
            PropertyType = apiListing.IsProject ? ProjectType : HouseType, // Best guess
            Status = null, // Unknown from API; don't overwrite enriched status
            Url = fullUrl,
            ImageUrl = apiListing.Image?.Default
        };
    }

    [GeneratedRegex(@"funda\.nl/(?:koop|huur)/([^/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegionRegex();

    [GeneratedRegex(@"selected_area=.*?""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex QueryRegionRegex();

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex PriceCleanupRegex();

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

    private async Task<int> ScrapeRecentPagesAsync(string region, int remainingCalls, CancellationToken cancellationToken)
    {
        var pages = Math.Max(1, _options.RecentPagesPerRegion);
        var cursor = await _cursorRepository.GetOrCreateAsync(region, cancellationToken);

        for (var page = 1; page <= pages && remainingCalls > 0; page++)
        {
            var listings = await FetchListingsPageAsync(region, page, cancellationToken);
            remainingCalls--;

            if (listings.Count == 0)
            {
                break;
            }

            await ProcessListingsAsync(listings, shouldNotify: false, cancellationToken);
        }

        cursor.LastRecentScrapeUtc = DateTime.UtcNow;
        await _cursorRepository.UpdateAsync(cursor, cancellationToken);

        return remainingCalls;
    }

    private async Task<int> ScrapeBackfillPagesAsync(string region, int remainingCalls, CancellationToken cancellationToken)
    {
        if (_options.MaxBackfillPagesPerRun <= 0)
        {
            return remainingCalls;
        }

        var cursor = await _cursorRepository.GetOrCreateAsync(region, cancellationToken);
        var pagesToFetch = _options.MaxBackfillPagesPerRun;

        for (var i = 0; i < pagesToFetch && remainingCalls > 0; i++)
        {
            var page = Math.Max(1, cursor.NextBackfillPage);
            var listings = await FetchListingsPageAsync(region, page, cancellationToken);
            remainingCalls--;

            if (listings.Count == 0)
            {
                cursor.NextBackfillPage = 1;
                break;
            }

            await ProcessListingsAsync(listings, shouldNotify: false, cancellationToken);
            cursor.NextBackfillPage = page + 1;
        }

        cursor.LastBackfillScrapeUtc = DateTime.UtcNow;
        await _cursorRepository.UpdateAsync(cursor, cancellationToken);

        return remainingCalls;
    }

    private async Task ProcessListingsAsync(
        IReadOnlyCollection<FundaApiListing> listings,
        bool shouldNotify,
        CancellationToken cancellationToken)
    {
        if (listings.Count == 0)
        {
            if (shouldNotify)
            {
                await NotifyScrapingErrorAsync("No results found.");
            }
            return;
        }

        if (shouldNotify)
        {
            await _notificationService.NotifyProgressAsync($"Found {listings.Count} listings. Processing...");
        }

        foreach (var apiListing in listings)
        {
            try
            {
                await ProcessListingAsync(apiListing, shouldNotify, cancellationToken);
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
}
