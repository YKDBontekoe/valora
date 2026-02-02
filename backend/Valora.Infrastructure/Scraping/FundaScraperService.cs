using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Scraping;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

public class FundaScraperService : IFundaScraperService
{
    private readonly FundaApiClient _apiClient;
    private readonly IListingService _listingService;
    private readonly ScraperOptions _options;
    private readonly ILogger<FundaScraperService> _logger;
    private readonly IScraperNotificationService _notificationService;

    public FundaScraperService(
        FundaApiClient apiClient,
        IListingService listingService,
        IOptions<ScraperOptions> options,
        ILogger<FundaScraperService> logger,
        IScraperNotificationService notificationService)
    {
        _apiClient = apiClient;
        _listingService = listingService;
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

        // Map to DTOs
        var dtos = new List<ScrapedListingDto>();
        foreach (var apiListing in apiListings)
        {
            try
            {
                var dto = MapToDto(apiListing);
                dtos.Add(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to map listing to DTO: {GlobalId}", apiListing.GlobalId);
            }
        }

        // Delegate to Application Service
        if (dtos.Count > 0)
        {
            await _listingService.ProcessListingsAsync(dtos, shouldNotify, cancellationToken);
        }
    }
    
    private async Task<List<FundaApiListing>> TryFetchFromApiAsync(string region, int? limit, CancellationToken cancellationToken)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch from Funda API");
            return [];
        }
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

    private static ScrapedListingDto MapToDto(FundaApiListing apiListing)
    {
        var price = ParsePriceFromApi(apiListing.Price);
        var fullUrl = apiListing.ListingUrl!.StartsWith("http") ? apiListing.ListingUrl : $"https://www.funda.nl{apiListing.ListingUrl}";

        return new ScrapedListingDto
        {
            FundaId = apiListing.GlobalId.ToString(),
            Address = apiListing.Address?.ListingAddress ?? apiListing.Address?.City ?? "Unknown Address",
            City = apiListing.Address?.City,
            PostalCode = null, // Not provided by API
            Price = price,
            Bedrooms = null, // Not provided by API
            LivingAreaM2 = null, // Not provided by API
            PlotAreaM2 = null, // Not provided by API
            PropertyType = apiListing.IsProject ? "Nieuwbouwproject" : "Woonhuis", // Best guess
            Status = "Beschikbaar", // API usually returns available listings
            Url = fullUrl,
            ImageUrl = apiListing.Image?.Default
        };
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
