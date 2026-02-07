using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

/// <summary>
/// Playwright-based implementation of IFundaApiClient that uses browser automation
/// to bypass Funda's bot protection (Cloudflare/Akamai).
/// 
/// <para>
/// <strong>Why Playwright?</strong>
/// Funda blocks direct HTTP requests to their Topposition API with 403 Forbidden.
/// The protection requires JavaScript execution for validation. Playwright launches
/// a real browser that can pass these checks.
/// </para>
/// </summary>
public partial class PlaywrightFundaClient : IFundaApiClient, IAsyncDisposable
{
    private readonly ILogger<PlaywrightFundaClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SemaphoreSlim _browserLock = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _disposed;
    private const int ChallengePollIntervalMs = 1_000;
    private const int ChallengeMaxWaitMs = 30_000;

    // Reuse JSON options for parsing
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PlaywrightFundaClient(ILogger<PlaywrightFundaClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Ensures browser is initialized, creating it if needed.
    /// Uses lazy initialization to avoid startup cost when not used.
    /// Tries to use system Chrome first, falls back to bundled Chromium.
    /// </summary>
    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser != null) return _browser;

        await _browserLock.WaitAsync();
        IPlaywright? playwright = null;
        try
        {
            if (_browser != null) return _browser;

            playwright = await Playwright.CreateAsync();
            IBrowser? browser = null;

            try
            {
                // Try using Chrome channel (system-installed Chrome) first.
                browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Channel = "chrome", // Use system Chrome
                    Args = new[]
                    {
                        "--disable-blink-features=AutomationControlled",
                        "--no-sandbox"
                    }
                });
                _logger.LogInformation("Playwright initialized with system Chrome");
            }
            catch (PlaywrightException firstLaunchException)
            {
                try
                {
                    // Fall back to bundled Chromium if system Chrome is not available.
                    browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                    {
                        Headless = true,
                        Args = new[]
                        {
                            "--disable-blink-features=AutomationControlled",
                            "--no-sandbox"
                        }
                    });
                    _logger.LogInformation("Playwright initialized with bundled Chromium");
                }
                catch (Exception secondLaunchException)
                {
                    _logger.LogError(
                        secondLaunchException,
                        "Playwright browser launch failed for both Chrome channel and bundled Chromium");
                    throw new InvalidOperationException(
                        "Unable to launch Playwright browser.",
                        new AggregateException(firstLaunchException, secondLaunchException));
                }
            }

            if (browser == null)
            {
                throw new InvalidOperationException("Unable to initialize Playwright browser.");
            }

            _playwright = playwright;
            _browser = browser;
            return browser;
        }
        catch
        {
            if (_browser == null && playwright != null)
            {
                playwright.Dispose();
            }

            _playwright = null;
            throw;
        }
        finally
        {
            _browserLock.Release();
        }
    }


    /// <summary>
    /// Creates a new browser context with realistic browser fingerprint.
    /// </summary>
    private async Task<IBrowserContext> CreateContextAsync()
    {
        var browser = await GetBrowserAsync();
        return await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
            Locale = "nl-NL",
            TimezoneId = "Europe/Amsterdam",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });
    }

    public async Task<FundaApiResponse?> SearchBuyAsync(
        string geoInfo,
        int page = 1,
        int? minPrice = null,
        int? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        return await SearchWithPlaywrightAsync(geoInfo, "koop", page, minPrice, maxPrice, cancellationToken);
    }

    public async Task<FundaApiResponse?> SearchRentAsync(
        string geoInfo,
        int page = 1,
        int? minPrice = null,
        int? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        return await SearchWithPlaywrightAsync(geoInfo, "huur", page, minPrice, maxPrice, cancellationToken);
    }

    public async Task<FundaApiResponse?> SearchProjectsAsync(
        string geoInfo,
        int page = 1,
        int? minPrice = null,
        int? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        return await SearchWithPlaywrightAsync(geoInfo, "nieuwbouw", page, minPrice, maxPrice, cancellationToken);
    }

    /// <summary>
    /// Uses Playwright to navigate to Funda search page and extract listing data.
    /// </summary>
    private async Task<FundaApiResponse?> SearchWithPlaywrightAsync(
        string geoInfo,
        string searchType,
        int page,
        int? minPrice,
        int? maxPrice,
        CancellationToken cancellationToken)
    {
        var url = $"https://www.funda.nl/{searchType}/{geoInfo.ToLowerInvariant()}/";
        if (page > 1)
        {
            url += $"p{page}/";
        }

        var queryParams = new List<string>();
        if (minPrice.HasValue || maxPrice.HasValue)
        {
            var min = minPrice?.ToString() ?? "0";
            var max = maxPrice?.ToString() ?? string.Empty;
            queryParams.Add($"price={min}-{max}");
        }

        if (queryParams.Count > 0)
        {
            url += $"?{string.Join("&", queryParams)}";
        }

        _logger.LogDebug("Playwright navigating to {Url}", url);

        await using var context = await CreateContextAsync();
        var pageObj = await context.NewPageAsync();

        try
        {
            // Navigate with retry
            await ExecuteWithRetryAsync(async () =>
            {
                await pageObj.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000
                });
            }, "Search Page Navigation", cancellationToken);

            // Wait a bit for dynamic content
            await Task.Delay(2000, cancellationToken);

            // Debug: log the actual URL and page title (detect challenge pages)
            var actualUrl = pageObj.Url;
            var title = await pageObj.TitleAsync();
            _logger.LogDebug("Page loaded - URL: {Url}, Title: {Title}", actualUrl, title);

            // Check if we landed on a challenge page
            if (title.Contains("bijna op de pagina", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("challenge", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Detected bot challenge page, waiting for resolution...");
                var resolved = await WaitForChallengeResolutionAsync(pageObj, cancellationToken);
                if (!resolved)
                {
                    _logger.LogWarning("Challenge page did not resolve within timeout for {Url}", url);
                    return null;
                }
            }

            // Extract listing data from the page
            var listings = await ExtractListingsFromPageAsync(pageObj, cancellationToken);
            listings = ApplyPriceFilter(listings, minPrice, maxPrice);

            return new FundaApiResponse
            {
                FriendlyUrl = url,
                Listings = listings
            };
        }
        catch (PlaywrightException ex)
        {
            _logger.LogError(ex, "Playwright navigation failed for {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Executes an async action with exponential backoff retry logic.
    /// </summary>
    private async Task ExecuteWithRetryAsync(Func<Task> action, string operationName, CancellationToken cancellationToken, int maxRetries = 3)
    {
        int attempt = 0;
        while (true)
        {
            try
            {
                attempt++;
                await action();
                return; // Success
            }
            catch (Exception ex) when (attempt <= maxRetries && !cancellationToken.IsCancellationRequested && (ex is PlaywrightException || ex is TimeoutException))
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2s, 4s, 8s
                _logger.LogWarning(ex, "Retry {Attempt}/{MaxRetries} for {Operation}. Waiting {Delay}s.", attempt, maxRetries, operationName, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                if (attempt > maxRetries)
                {
                    _logger.LogError(ex, "Max retries exceeded for {Operation}.", operationName);
                }
                throw;
            }
        }
    }

    private static List<FundaApiListing> ApplyPriceFilter(List<FundaApiListing> listings, int? minPrice, int? maxPrice)
    {
        if (!minPrice.HasValue && !maxPrice.HasValue)
        {
            return listings;
        }

        return listings
            .Where(listing =>
            {
                var parsed = ParsePriceValue(listing.Price);
                if (!parsed.HasValue)
                {
                    // Keep listings with unparseable price to avoid dropping valid objects.
                    return true;
                }

                if (minPrice.HasValue && parsed.Value < minPrice.Value)
                {
                    return false;
                }

                if (maxPrice.HasValue && parsed.Value > maxPrice.Value)
                {
                    return false;
                }

                return true;
            })
            .ToList();
    }

    private static decimal? ParsePriceValue(string? price)
    {
        if (string.IsNullOrWhiteSpace(price))
        {
            return null;
        }

        var digitsOnly = new string(price.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digitsOnly))
        {
            return null;
        }

        return decimal.TryParse(digitsOnly, out var parsed) ? parsed : null;
    }

    private static bool IsChallengeTitle(string title) =>
        title.Contains("bijna op de pagina", StringComparison.OrdinalIgnoreCase) ||
        title.Contains("challenge", StringComparison.OrdinalIgnoreCase);

    private async Task<bool> WaitForChallengeResolutionAsync(IPage page, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(ChallengeMaxWaitMs);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentTitle = await page.TitleAsync();
            if (!IsChallengeTitle(currentTitle))
            {
                return true;
            }

            var listingLinks = await page.QuerySelectorAllAsync("[data-testid='listingDetailsAddress']");
            if (listingLinks.Count > 0)
            {
                return true;
            }

            await Task.Delay(ChallengePollIntervalMs, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Extracts listing data from a Funda search results page.
    /// Tries DOM scraping first (most reliable), then falls back to JSON parsing.
    /// </summary>
    private async Task<List<FundaApiListing>> ExtractListingsFromPageAsync(
        IPage page,
        CancellationToken cancellationToken)
    {
        var listings = new List<FundaApiListing>();

        // Primary: DOM-based extraction (most reliable)
        try
        {
            _logger.LogDebug("Attempting DOM-based extraction");
            listings = await ExtractListingsFromDomAsync(page);
            _logger.LogDebug("DOM extraction found {Count} listings", listings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DOM-based extraction failed");
        }

        // Fallback: Try Nuxt JSON if DOM extraction failed
        if (listings.Count == 0)
        {
            try
            {
                var scripts = await page.QuerySelectorAllAsync("script[type='application/json']");
                _logger.LogDebug("Found {Count} JSON script elements, trying JSON fallback", scripts.Count);
                
                foreach (var script in scripts)
                {
                    var content = await script.TextContentAsync();
                    if (string.IsNullOrEmpty(content)) continue;

                    if (content.Contains("searchResults") || content.Contains("listings"))
                    {
                        var parsed = ParseSearchResultsJson(content);
                        if (parsed.Count > 0)
                        {
                            listings.AddRange(parsed);
                            _logger.LogDebug("Parsed {Count} listings from JSON", parsed.Count);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "JSON parsing fallback also failed");
            }
        }

        _logger.LogDebug("Extracted {Count} listings from page", listings.Count);
        return listings;
    }


    /// <summary>
    /// Parses search results JSON from Nuxt hydration state.
    /// </summary>
    private List<FundaApiListing> ParseSearchResultsJson(string json)
    {
        var listings = new List<FundaApiListing>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Navigate the JSON structure to find listings array
            // The structure varies, so we try multiple paths
            if (TryFindListingsArray(root, out var listingsArray))
            {
                foreach (var item in listingsArray.EnumerateArray())
                {
                    var listing = ParseListingElement(item);
                    if (listing != null)
                    {
                        listings.Add(listing);
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse search results JSON");
        }

        return listings;
    }

    private static bool TryFindListingsArray(JsonElement element, out JsonElement result)
    {
        result = default;

        // Try common paths
        if (element.TryGetProperty("searchResults", out var sr) &&
            sr.TryGetProperty("listings", out result))
        {
            return true;
        }

        if (element.TryGetProperty("listings", out result))
        {
            return true;
        }

        // Deep search
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array &&
                    prop.Name.Contains("listing", StringComparison.OrdinalIgnoreCase))
                {
                    result = prop.Value;
                    return true;
                }

                if (prop.Value.ValueKind == JsonValueKind.Object &&
                    TryFindListingsArray(prop.Value, out result))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static FundaApiListing? ParseListingElement(JsonElement element)
    {
        try
        {
            var globalId = element.TryGetProperty("globalId", out var gid) ? gid.GetInt32() : 0;
            if (globalId == 0 && element.TryGetProperty("id", out var id))
            {
                globalId = id.ValueKind == JsonValueKind.Number ? id.GetInt32() : 0;
            }

            if (globalId == 0) return null;

            var price = element.TryGetProperty("price", out var p) ? p.GetString() : null;
            var listingUrl = element.TryGetProperty("listingUrl", out var lu) ? lu.GetString() : null;
            if (listingUrl == null && element.TryGetProperty("url", out var u))
            {
                listingUrl = u.GetString();
            }

            string? address = null;
            string? city = null;

            if (element.TryGetProperty("address", out var addr))
            {
                address = addr.TryGetProperty("listingAddress", out var la) ? la.GetString() : null;
                if (address == null && addr.TryGetProperty("street", out var st))
                {
                    address = st.GetString();
                }
                city = addr.TryGetProperty("city", out var c) ? c.GetString() : null;
            }

            string? imageUrl = null;
            if (element.TryGetProperty("image", out var img))
            {
                imageUrl = img.TryGetProperty("default", out var def) ? def.GetString() : null;
            }

            return new FundaApiListing
            {
                GlobalId = globalId,
                Price = price,
                ListingUrl = listingUrl,
                Address = new FundaApiAddress
                {
                    ListingAddress = address,
                    City = city
                },
                Image = new FundaApiImage
                {
                    Default = imageUrl
                }
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Fallback DOM-based extraction using CSS selectors.
    /// Updated 2026-02: Uses data-testid='listingDetailsAddress' anchor selector
    /// </summary>
    private async Task<List<FundaApiListing>> ExtractListingsFromDomAsync(IPage page)
    {
        var listings = new List<FundaApiListing>();

        try
        {
            // Find all listing address links using the stable data-testid selector
            var addressLinks = await page.QuerySelectorAllAsync("[data-testid='listingDetailsAddress']");
            
            _logger.LogDebug("Found {Count} listing address links", addressLinks.Count);

            foreach (var addressLink in addressLinks)
            {
                try
                {
                    // Extract link URL
                    var href = await addressLink.GetAttributeAsync("href");
                    if (string.IsNullOrEmpty(href)) continue;

                    // Log first few URLs for debugging
                    if (listings.Count < 3)
                    {
                        _logger.LogDebug("Found listing URL: {Url}", href);
                    }

                    // Extract global ID from URL
                    var globalId = ExtractGlobalIdFromUrl(href);
                    // Don't skip if globalId is 0 - we can still use the URL as identifier

                    // Extract address text from the link
                    var address = await addressLink.TextContentAsync();

                    // Navigate up to find the listing container and extract price
                    // The price is typically in a parent container with flex layout
                    string? price = null;
                    try
                    {
                        // Use JavaScript to get the price from nearby elements
                        price = await page.EvaluateAsync<string?>(@"(el) => {
                            let container = el;
                            // Go up a few levels to find the container with price
                            for (let i = 0; i < 6 && container; i++) {
                                const priceEl = container.querySelector('div.truncate');
                                if (priceEl && priceEl.innerText.includes('€')) {
                                    return priceEl.innerText.trim();
                                }
                                container = container.parentElement;
                            }
                            // Alternative: look for any element with € symbol
                            for (let i = 0; i < 6 && container; i++) {
                                const text = container.innerText;
                                const match = text.match(/€\s*[\d.,]+(?:\s*(?:k\.k\.|v\.o\.n\.))?/);
                                if (match) return match[0];
                                container = container.parentElement;
                            }
                            return null;
                        }", addressLink);
                    }
                    catch
                    {
                        // Price extraction is optional
                    }

                    listings.Add(new FundaApiListing
                    {
                        GlobalId = globalId,
                        Price = price?.Trim(),
                        ListingUrl = href,
                        Address = new FundaApiAddress
                        {
                            ListingAddress = address?.Trim()
                        }
                    });
                }
                catch
                {
                    // Skip items that fail to parse
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DOM-based extraction failed");
        }

        return listings;
    }

    private static int ExtractGlobalIdFromUrl(string url)
    {
        // Updated URL format: /detail/koop/amsterdam/appartement-name/43239385/
        // Global ID is now found as the last numeric segment before trailing slash
        
        // Try new format first: /detail/.../ID/
        var newMatch = Regex.Match(url, @"/(\d{6,})/?$");
        if (newMatch.Success && int.TryParse(newMatch.Groups[1].Value, out var newId))
        {
            return newId;
        }
        
        // Fallback to old format: /koop/amsterdam/huis-12345678-address/
        var oldMatch = Regex.Match(url, @"-(\d{6,})");
        return oldMatch.Success && int.TryParse(oldMatch.Groups[1].Value, out var oldId) ? oldId : 0;
    }


    public async Task<List<FundaApiListing>> SearchAllBuyPagesAsync(
        string geoInfo,
        int maxPages = 5,
        CancellationToken cancellationToken = default)
    {
        var allListings = new List<FundaApiListing>();

        for (var page = 1; page <= maxPages; page++)
        {
            try
            {
                var result = await SearchBuyAsync(geoInfo, page, cancellationToken: cancellationToken);

                if (result?.Listings == null || result.Listings.Count == 0)
                {
                    break;
                }

                allListings.AddRange(result.Listings);
                _logger.LogDebug("Page {Page}: found {Count} listings", page, result.Listings.Count);

                // Rate limiting - be respectful
                await Task.Delay(2000, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch page {Page}", page);
            }
        }

        return allListings;
    }

    /// <summary>
    /// Extracts rich listing details from a Funda detail page using DOM scraping.
    /// Uses stable data-testid selectors discovered 2026-02.
    /// </summary>
    public async Task<FundaNuxtListingData?> GetListingDetailsAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(url)) return null;

        // Ensure absolute URL
        if (!url.StartsWith("http"))
        {
            url = "https://www.funda.nl" + url;
        }

        _logger.LogDebug("Playwright fetching listing details: {Url}", url);

        await using var context = await CreateContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            // Navigate with retry
            await ExecuteWithRetryAsync(async () =>
            {
                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000
                });
            }, "Detail Page Navigation", cancellationToken);

            await Task.Delay(1000, cancellationToken);

            // Dismiss cookie consent if present
            try
            {
                var acceptButton = await page.QuerySelectorAsync("button[id*='accept'], button[class*='accept']");
                if (acceptButton != null)
                {
                    await acceptButton.ClickAsync();
                    await Task.Delay(500, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Cookie consent dismissal failed for {Url}", url);
            }

            // Log page title for debugging
            var title = await page.TitleAsync();
            _logger.LogDebug("Detail page title: {Title}", title);

            // Wait for category elements to appear (they contain the characteristics)
            try
            {
                await page.WaitForSelectorAsync("[data-testid^='category-']", new PageWaitForSelectorOptions
                {
                    Timeout = 5000,
                    State = WaitForSelectorState.Attached
                });
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Category elements not found - page may be blocked or different structure");
                // Try to get some diagnostic info
                var bodyLength = await page.EvaluateAsync<int>("document.body?.innerHTML?.length || 0");
                var hasNuxt = await page.EvaluateAsync<bool>("!!document.getElementById('__NUXT_DATA__')");
                _logger.LogDebug("Body length: {Length}, Has NUXT: {HasNuxt}", bodyLength, hasNuxt);
            }

            // Scroll down to load all content (lazy loading)
            try
            {
                await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight / 2)");
                await Task.Delay(500, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Scroll step failed for {Url}", url);
            }

            // Extract characteristics using data-testid categories
            var characteristics = await ExtractCharacteristicsAsync(page);
            _logger.LogDebug("Extracted {Count} raw characteristics from page", characteristics.Count);



            // Build feature groups from extracted characteristics
            var indelingItems = new List<FundaNuxtFeatureItem>();
            var afmetingenItems = new List<FundaNuxtFeatureItem>();
            var energieItems = new List<FundaNuxtFeatureItem>();
            var bouwItems = new List<FundaNuxtFeatureItem>();

            // Map characteristics to appropriate feature groups
            foreach (var (key, value) in characteristics)
            {
                var item = new FundaNuxtFeatureItem { Label = key, Value = value };

                // Categorize based on key
                if (key.Contains("kamer", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("badkamer", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("verdiep", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("garage", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("balkon", StringComparison.OrdinalIgnoreCase) ||
                    key.Contains("tuin", StringComparison.OrdinalIgnoreCase))
                {
                    indelingItems.Add(item);
                }
                else if (key.Contains("wonen", StringComparison.OrdinalIgnoreCase) ||
                         key.Contains("perceel", StringComparison.OrdinalIgnoreCase) ||
                         key.Contains("inhoud", StringComparison.OrdinalIgnoreCase) ||
                         key.Contains("m²", StringComparison.OrdinalIgnoreCase))
                {
                    afmetingenItems.Add(item);
                }
                else if (key.Contains("energie", StringComparison.OrdinalIgnoreCase) ||
                         key.Contains("isolatie", StringComparison.OrdinalIgnoreCase) ||
                         key.Contains("verwarming", StringComparison.OrdinalIgnoreCase) ||
                         key.Contains("cv", StringComparison.OrdinalIgnoreCase))
                {
                    energieItems.Add(item);
                }
                else if (key.Contains("bouwjaar", StringComparison.OrdinalIgnoreCase) ||
                         key.Contains("dak", StringComparison.OrdinalIgnoreCase) ||
                         key.Contains("eigendom", StringComparison.OrdinalIgnoreCase) ||
                         key.Contains("vve", StringComparison.OrdinalIgnoreCase) ||
                         key.Contains("postcode", StringComparison.OrdinalIgnoreCase) ||
                         key.Contains("kadastr", StringComparison.OrdinalIgnoreCase))
                {
                    bouwItems.Add(item);
                }
                else
                {
                    // Default to bouw for other properties
                    bouwItems.Add(item);
                }
            }

            // Extract description
            string? descriptionText = null;
            try
            {
                descriptionText = await page.EvaluateAsync<string?>(@"() => {
                    const h2 = Array.from(document.querySelectorAll('h2')).find(h => h.textContent.includes('Omschrijving'));
                    if (!h2) return null;
                    const container = h2.parentElement?.querySelector('div');
                    return container?.textContent?.trim() || null;
                }");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Description extraction failed for {Url}", url);
            }

            // Extract photo URLs - Try to get ALL photos from __NUXT_DATA__ script first
            var mediaItems = new List<FundaNuxtMediaItem>();
            try 
            {
                // Method 1: Extraction from __NUXT_DATA__ (contains all photos, not just visible ones)
                var allPhotoIds = await page.EvaluateAsync<string[]?>(@"() => {
                    const script = document.querySelector('script[id=""__NUXT_DATA__""]');
                    if (!script) return null;
                    
                    const text = script.textContent;
                    // Look for patterns like ""224/286/929"" which are photo IDs
                    // We look for 3 groups of 3 digits separated by slashes
                    const matches = text.match(/""\d{3}\/\d{3}\/\d{3}""/g);
                    
                    if (!matches) return null;
                    
                    // Clean up quotes and return unique IDs
                    return [...new Set(matches.map(m => m.replace(/""/g, '')))];
                }");

                if (allPhotoIds?.Length > 0)
                {
                    // Construct full URLs for all found IDs
                    // Use larger resolution based on browser findings (2048x1536 is usually available, but 1440 is safe default)
                    mediaItems = allPhotoIds.Select(id => new FundaNuxtMediaItem 
                    { 
                        Id = $"https://cloud.funda.nl/valentina_media/{id}.jpg?options=width=1440", 
                        Type = 1 
                    }).ToList();
                    
                    _logger.LogDebug("Extracted {Count} photos from NUXT_DATA", mediaItems.Count);
                }
                else
                {
                    // Method 2: Fallback to DOM extraction (only gets visible photos)
                    _logger.LogDebug("NUXT_DATA photo extraction failed, falling back to DOM");
                    
                    var visiblePhotoUrls = await page.EvaluateAsync<string[]?>(@"() => {
                        const imgs = Array.from(document.querySelectorAll('img[src*=""cloud.funda""]'));
                        return imgs.map(img => img.src).filter(src => src && !src.includes('120x') && !src.includes('80x'));
                    }");
                    
                    if (visiblePhotoUrls?.Length > 0)
                    {
                        mediaItems = visiblePhotoUrls.Distinct().Select(photoUrl => new FundaNuxtMediaItem { Id = photoUrl, Type = 1 }).ToList();
                        _logger.LogDebug("Extracted {Count} photos from DOM fallback", mediaItems.Count);
                    }
                }
            }
            catch (Exception ex) 
            { 
                _logger.LogWarning(ex, "Failed to extract photos");
            }

            var result = new FundaNuxtListingData
            {
                Features = new FundaNuxtFeatures
                {
                    Indeling = indelingItems.Count > 0 ? new FundaNuxtFeatureGroup { Title = "Indeling", KenmerkenList = indelingItems } : null,
                    Afmetingen = afmetingenItems.Count > 0 ? new FundaNuxtFeatureGroup { Title = "Afmetingen", KenmerkenList = afmetingenItems } : null,
                    Energie = energieItems.Count > 0 ? new FundaNuxtFeatureGroup { Title = "Energie", KenmerkenList = energieItems } : null,
                    Bouw = bouwItems.Count > 0 ? new FundaNuxtFeatureGroup { Title = "Bouw", KenmerkenList = bouwItems } : null
                },
                Description = !string.IsNullOrEmpty(descriptionText) ? new FundaNuxtDescription { Content = descriptionText } : null,
                Media = mediaItems.Count > 0 ? new FundaNuxtMedia { Items = mediaItems } : null
            };

            var totalFeatures = indelingItems.Count + afmetingenItems.Count + energieItems.Count + bouwItems.Count;
            _logger.LogDebug("Extracted {Count} features from detail page", totalFeatures);

            return totalFeatures > 0 || mediaItems.Count > 0 || !string.IsNullOrEmpty(descriptionText)
                ? result
                : null;
        }
        catch (PlaywrightException ex)
        {
            _logger.LogError(ex, "Playwright failed to fetch listing details: {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Extracts characteristics from the detail page using dt/dd pairs within data-testid categories.
    /// </summary>
    private async Task<Dictionary<string, string>> ExtractCharacteristicsAsync(IPage page)
    {
        var result = new Dictionary<string, string>();

        try
        {
            // Use JSON.stringify to ensure proper serialization
            var jsonString = await page.EvaluateAsync<string>(@"() => {
                const result = {};
                
                // Find all category sections with data-testid
                const categories = document.querySelectorAll('[data-testid^=""category-""]');
                categories.forEach(cat => {
                    // Find all dt/dd pairs within each category
                    const dts = cat.querySelectorAll('dt');
                    dts.forEach(dt => {
                        const dd = dt.nextElementSibling;
                        if (dd && dd.tagName === 'DD') {
                            const key = dt.textContent.trim();
                            const value = dd.textContent.trim();
                            if (key && value) {
                                result[key] = value;
                            }
                        }
                    });
                });
                
                // Also check for dl outside of categories
                const allDts = document.querySelectorAll('dl dt');
                allDts.forEach(dt => {
                    const dd = dt.nextElementSibling;
                    if (dd && dd.tagName === 'DD') {
                        const key = dt.textContent.trim();
                        const value = dd.textContent.trim();
                        if (key && value && !result[key]) {
                            result[key] = value;
                        }
                    }
                });
                
                return JSON.stringify(result);
            }");

            if (!string.IsNullOrEmpty(jsonString))
            {
                result = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString) ?? result;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract characteristics");
            return result;
        }
    }


    // These APIs work without bot protection, so we delegate to HTTP client

    public async Task<FundaApiListingSummary?> GetListingSummaryAsync(
        int globalId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://www.funda.nl/api/detail-summary/v2/getsummary/{globalId}";
            using var client = _httpClientFactory.CreateClient("FundaHttpClient");
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch summary for {GlobalId}. Status: {StatusCode}", globalId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FundaApiListingSummary>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch summary for {GlobalId}", globalId);
            return null;
        }
    }

    public async Task<FundaContactDetailsResponse?> GetContactDetailsAsync(
        int globalId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://contacts-bff.funda.io/api/v3/listings/{globalId}/contact-details?website=1";
            using var client = _httpClientFactory.CreateClient("FundaHttpClient");
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch contact details for {GlobalId}. Status: {StatusCode}", globalId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FundaContactDetailsResponse>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch contact details for {GlobalId}", globalId);
            return null;
        }
    }

    public async Task<FundaFiberResponse?> GetFiberAvailabilityAsync(
        string postalCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postalCode)) return null;

        var cleanPostalCode = postalCode.Replace(" ", "").ToUpperInvariant();

        try
        {
            var url = $"https://kpnopticfiber.funda.io/api/v1/{cleanPostalCode}";
            using var client = _httpClientFactory.CreateClient("FundaHttpClient");
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to check fiber for postal code {PostalCodeHash}. Status: {StatusCode}", cleanPostalCode.GetHashCode(), response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<FundaFiberResponse>(content, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to check fiber for postal code hash {PostalCodeHash}", cleanPostalCode.GetHashCode());
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_browser != null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;
        _browserLock.Dispose();

        GC.SuppressFinalize(this);
    }
}
