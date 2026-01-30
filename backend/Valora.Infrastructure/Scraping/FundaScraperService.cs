using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Scraping;

public class FundaScraperService : IFundaScraperService
{
    private readonly HttpClient _httpClient;
    private readonly IListingRepository _listingRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly FundaHtmlParser _parser;
    private readonly ScraperOptions _options;
    private readonly ILogger<FundaScraperService> _logger;
    private readonly IScraperNotificationService _notificationService;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public FundaScraperService(
        HttpClient httpClient,
        IListingRepository listingRepository,
        IPriceHistoryRepository priceHistoryRepository,
        IOptions<ScraperOptions> options,
        ILogger<FundaScraperService> logger,
        IScraperNotificationService notificationService)
    {
        _httpClient = httpClient;
        _listingRepository = listingRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _parser = new FundaHtmlParser();
        _options = options.Value;
        _logger = logger;
        _notificationService = notificationService;

        // Configure retry policy with exponential backoff
        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                _options.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (outcome, timespan, retryCount, _) =>
                {
                    _logger.LogWarning(
                        "Request failed. Waiting {Delay}s before retry {RetryCount}/{MaxRetries}",
                        timespan.TotalSeconds, retryCount, _options.MaxRetries);
                });

        // Set headers to appear as a regular browser
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", 
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "nl-NL,nl;q=0.9,en;q=0.8");
    }

    public async Task ScrapeAndStoreAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting funda.nl scrape job");

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
            await _notificationService.NotifyErrorAsync(ex.Message);
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

        // Fetch search results page
        var searchHtml = await FetchPageAsync(searchUrl, cancellationToken);
        if (string.IsNullOrEmpty(searchHtml))
        {
            _logger.LogWarning("Empty response from search URL: {Url}", searchUrl);
            if (shouldNotify)
            {
                await _notificationService.NotifyErrorAsync("No results found or failed to fetch page.");
            }
            return;
        }

        // Parse listing cards
        var listingCards = _parser.ParseSearchResults(searchHtml).ToList();

        if (limit.HasValue)
        {
            listingCards = listingCards.Take(limit.Value).ToList();
        }

        _logger.LogInformation("Found {Count} listings on search page", listingCards.Count);
        if (shouldNotify)
        {
            await _notificationService.NotifyProgressAsync($"Found {listingCards.Count} listings. Processing...");
        }

        foreach (var card in listingCards)
        {
            try
            {
                await ProcessListingAsync(card, shouldNotify, cancellationToken);
                
                // Rate limiting delay
                await Task.Delay(_options.DelayBetweenRequestsMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process listing: {FundaId}", card.FundaId);
            }
        }
    }

    private async Task ProcessListingAsync(FundaListingCard card, bool shouldNotify, CancellationToken cancellationToken)
    {
        // Check if listing already exists
        var existingListing = await _listingRepository.GetByFundaIdAsync(card.FundaId, cancellationToken);

        // Fetch detail page
        var detailHtml = await FetchPageAsync(card.Url, cancellationToken);
        if (string.IsNullOrEmpty(detailHtml))
        {
            _logger.LogWarning("Empty response from detail page: {Url}", card.Url);
            return;
        }

        // Parse detail page
        var listing = _parser.ParseDetailPage(detailHtml, card.FundaId, card.Url);
        if (listing == null)
        {
            _logger.LogWarning("Failed to parse listing: {FundaId}", card.FundaId);
            return;
        }

        if (existingListing == null)
        {
            // New listing - add it
            await _listingRepository.AddAsync(listing, cancellationToken);
            _logger.LogInformation("Added new listing: {FundaId} - {Address}", listing.FundaId, listing.Address);
            if (shouldNotify)
            {
                try { await _notificationService.NotifyListingFoundAsync(listing.Address); } catch { /* Ignore notification failures */ }
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
        else
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

            // Update listing
            existingListing.Price = listing.Price;
            existingListing.Bedrooms = listing.Bedrooms;
            existingListing.LivingAreaM2 = listing.LivingAreaM2;
            existingListing.PlotAreaM2 = listing.PlotAreaM2;
            existingListing.Status = listing.Status;
            existingListing.ImageUrl = listing.ImageUrl;

            await _listingRepository.UpdateAsync(existingListing, cancellationToken);
            _logger.LogDebug("Updated listing: {FundaId}", listing.FundaId);

            if (shouldNotify)
            {
                try { await _notificationService.NotifyListingFoundAsync($"{listing.Address} (Updated)"); } catch { /* Ignore notification failures */ }
            }
        }
    }

    private async Task<string?> FetchPageAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _retryPolicy.ExecuteAsync(async () =>
                await _httpClient.GetAsync(url, cancellationToken));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch {Url}: {StatusCode}", url, response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {Url}", url);
            return null;
        }
    }
}
