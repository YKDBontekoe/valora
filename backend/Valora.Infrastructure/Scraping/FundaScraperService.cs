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

    private async Task ProcessListingAsync(FundaApiListing apiListing, bool shouldNotify, CancellationToken cancellationToken)
    {
        var fundaId = apiListing.GlobalId.ToString();
        var existingListing = await _listingRepository.GetByFundaIdAsync(fundaId, cancellationToken);

        var listing = MapApiListingToDomain(apiListing, fundaId);

        // 1. Enrich with Summary API (includes publicationDate, sold status, labels, postal code)
        try
        {
            var summary = await _apiClient.GetListingSummaryAsync(apiListing.GlobalId, cancellationToken);
            if (summary != null)
            {
                EnrichListingWithSummary(listing, summary);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch summary for {FundaId}", fundaId);
        }

        // 2. Enrich with HTML/Nuxt data (rich features, description, photos)
        if (!string.IsNullOrEmpty(listing.Url))
        {
            try 
            {
                var richData = await _apiClient.GetListingDetailsAsync(listing.Url, cancellationToken);
                if (richData != null)
                {
                    EnrichListingWithNuxtData(listing, richData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch rich details for {FundaId}", fundaId);
            }
        }

        // 3. Enrich with Contact Details API (broker phone, logo, association)
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
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch contact details for {FundaId}", fundaId);
        }

        // 4. Check Fiber Availability (requires full postal code)
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
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to check fiber availability for {FundaId}", fundaId);
            }
        }

        if (existingListing == null)
        {
            await AddNewListingAsync(listing, shouldNotify, cancellationToken);
        }
        else
        {
            await UpdateExistingListingAsync(existingListing, listing, shouldNotify, cancellationToken);
        }
    }

    private static void EnrichListingWithSummary(Listing listing, FundaApiListingSummary summary)
    {
        // Publication date
        listing.PublicationDate = summary.PublicationDate;
        
        // Sold/Rented status flag
        listing.IsSoldOrRented = summary.IsSoldOrRented;
        
        // Labels (e.g., "Nieuw", "Open huis")
        if (summary.Labels?.Count > 0)
        {
            listing.Labels = summary.Labels
                .Where(l => !string.IsNullOrEmpty(l.Text))
                .Select(l => l.Text!)
                .ToList();
        }
        
        // Extract postal code from address if available
        if (!string.IsNullOrEmpty(summary.Address?.PostalCode))
        {
            listing.PostalCode = summary.Address.PostalCode;
        }
        
        // City from address
        if (!string.IsNullOrEmpty(summary.Address?.City))
        {
            listing.City = summary.Address.City;
        }
        
        // Status inference from tracking (most reliable source)
        if (!string.IsNullOrEmpty(summary.Tracking?.Values?.Status))
        {
            listing.Status = MapFundaStatus(summary.Tracking.Values.Status);
        }
        else if (summary.IsSoldOrRented)
        {
            listing.Status = "Verkocht/Verhuurd";
        }
    }

    private static string MapFundaStatus(string fundaStatus)
    {
        return fundaStatus.ToLowerInvariant() switch
        {
            "beschikbaar" => "Beschikbaar",
            "verkocht" or "sold" => "Verkocht",
            "verhuurd" or "rented" => "Verhuurd",
            "onder bod" => "Onder bod",
            "onder optie" => "Onder optie",
            _ => fundaStatus // Return as-is if unknown
        };
    }

    private static void EnrichListingWithNuxtData(Listing listing, FundaNuxtListingData data)
    {
        // Description
        listing.Description = data.Description?.Content;

        // Features
        if (data.Features != null)
        {
            // Living Area & Plot Area from ObjectType (Most reliable if available)
            if (data.ObjectType?.PropertySpecification != null)
            {
                listing.LivingAreaM2 = data.ObjectType.PropertySpecification.SelectedArea;
                listing.PlotAreaM2 = data.ObjectType.PropertySpecification.SelectedPlotArea;
            }

            // Flatten the recursive feature tree for easier access
            var featureMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (data.Features.Indeling != null) FlattenFeatures(data.Features.Indeling.KenmerkenList, featureMap);
            if (data.Features.Afmetingen != null) FlattenFeatures(data.Features.Afmetingen.KenmerkenList, featureMap);
            if (data.Features.Energie != null) FlattenFeatures(data.Features.Energie.KenmerkenList, featureMap);
            if (data.Features.Bouw != null) FlattenFeatures(data.Features.Bouw.KenmerkenList, featureMap);

            // -- P3: Store ALL features --
            listing.Features = featureMap;

            // -- Extract Data Points from Map --

            // Areas (Fallback)
            if (!listing.LivingAreaM2.HasValue && featureMap.TryGetValue("Wonen", out var livingArea))
                listing.LivingAreaM2 = ParseFirstNumber(livingArea);
            
            if (!listing.PlotAreaM2.HasValue && featureMap.TryGetValue("Perceel", out var plotArea)) 
                listing.PlotAreaM2 = ParseFirstNumber(plotArea);

            // Phase 3: New Specific Areas
            if (featureMap.TryGetValue("Gebouwgebonden buitenruimte", out var balcony)) listing.BalconyM2 = ParseFirstNumber(balcony);
            if (featureMap.TryGetValue("Externe bergruimte", out var storage)) listing.ExternalStorageM2 = ParseFirstNumber(storage);
            if (featureMap.TryGetValue("Inhoud", out var volume)) listing.VolumeM3 = ParseFirstNumber(volume);
            
            // Garden Area logic
            // Sometimes it's explicit: "Achtertuin (43 m²)" or just "Achtertuin" with value "43 m²"
            foreach(var kvp in featureMap) 
            {
               if (kvp.Key.Contains("tuin", StringComparison.OrdinalIgnoreCase) && kvp.Value.Contains("m²"))
               {
                   var area = ParseFirstNumber(kvp.Value);
                   if (area.HasValue && area > (listing.GardenM2 ?? 0))
                   {
                       listing.GardenM2 = area;
                   }
               }
            }


            // Rooms
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

            // Bathrooms
            if (featureMap.TryGetValue("Aantal badkamers", out var bathrooms))
                listing.Bathrooms = ParseFirstNumber(bathrooms);
            
            // Energy
            if (featureMap.TryGetValue("Energielabel", out var label)) listing.EnergyLabel = label.Trim();
            if (featureMap.TryGetValue("Isolatie", out var insulation)) listing.InsulationType = insulation;
            if (featureMap.TryGetValue("Verwarming", out var heating)) listing.HeatingType = heating;

            // Year Built
            if (featureMap.TryGetValue("Bouwjaar", out var year)) listing.YearBuilt = ParseFirstNumber(year);

            // Ownership
            if (featureMap.TryGetValue("Eigendomssituatie", out var ownership)) listing.OwnershipType = ownership;

            // VVE
            if (featureMap.TryGetValue("Bijdrage VvE", out var vveRaw))
            {
                 // Parsing "€ 150,00 per maand"
                 var vveClean = PriceCleanupRegex().Replace(vveRaw, "");
                 if (decimal.TryParse(vveClean, out var vveCost)) listing.VVEContribution = vveCost;
            }

            // Garden / Garage / Parking
            // Searching map keys for containment since they might be "Tuin", "Achtertuin", etc.
            foreach (var kvp in featureMap)
            {
                if (kvp.Key.Contains("Tuin", StringComparison.OrdinalIgnoreCase) || kvp.Key.Contains("Buitenruimte", StringComparison.OrdinalIgnoreCase))
                {
                    // Basic check: if meaningful value
                    if (!string.IsNullOrWhiteSpace(kvp.Value)) listing.GardenOrientation = kvp.Value; // Ideally specific key like "Ligging tuin"
                }

                if (kvp.Key.Equals("Ligging", StringComparison.OrdinalIgnoreCase) && featureMap.ContainsKey("Tuin")) // Common for Garden Orientation
                {
                     listing.GardenOrientation = kvp.Value;
                }
                
                if (kvp.Key.Contains("Garage", StringComparison.OrdinalIgnoreCase)) listing.HasGarage = true;
                
                if (kvp.Key.Contains("Parkeerfaciliteiten", StringComparison.OrdinalIgnoreCase)) listing.ParkingType = kvp.Value;
            }
            
            // Cadastral
            // We can look for patterns in the keys now that they are flattened
            // Typically keys are the Section IDs like "AMSTERDAM L 123"
            foreach (var key in featureMap.Keys)
            {
                if (key.Any(char.IsUpper) && key.Any(char.IsDigit) && key.Length > 5 && !key.Contains("kamers") && !key.Contains("bouw"))
                {
                     // Heuristic for Cadastral ID
                     if (featureMap.TryGetValue(key, out var val) && (string.IsNullOrEmpty(val) || val == "Title"))
                     {
                         listing.CadastralDesignation = key;
                         break; 
                     }
                }
            }
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

        // Phase 4: Complete Data Capture
        
        // Coordinates
        if (data.Coordinates != null)
        {
            listing.Latitude = data.Coordinates.Lat;
            listing.Longitude = data.Coordinates.Lng;
        }
        
        // Videos
        if (data.Videos != null && data.Videos.Count > 0)
        {
            listing.VideoUrl = data.Videos[0].Url;
        }
        
        // 360 Photos / Virtual Tour
        if (data.Photos360 != null && data.Photos360.Count > 0)
        {
            listing.VirtualTourUrl = data.Photos360[0].Url;
        }
        
        // Floor Plans
        if (data.FloorPlans != null)
        {
            listing.FloorPlanUrls = data.FloorPlans
                .Where(fp => !string.IsNullOrEmpty(fp.Url) || !string.IsNullOrEmpty(fp.Id))
                .Select(fp => fp.Url ?? $"https://cloud.funda.nl/valentina_media/{fp.Id}_720.jpg")
                .ToList();
        }
        
        // Brochure
        listing.BrochureUrl = data.BrochureUrl;
        
        // Engagement Insights
        if (data.ObjectInsights != null)
        {
            listing.ViewCount = data.ObjectInsights.Views;
            listing.SaveCount = data.ObjectInsights.Saves;
        }
        
        // Local / Neighborhood Insights
        if (data.LocalInsights != null)
        {
            listing.NeighborhoodPopulation = data.LocalInsights.Inhabitants;
            listing.NeighborhoodAvgPriceM2 = data.LocalInsights.AvgPricePerM2;
        }
        
        // Open House Dates
        if (data.OpenHouseDates != null)
        {
            listing.OpenHouseDates = data.OpenHouseDates
                .Where(oh => oh.Date.HasValue)
                .Select(oh => oh.Date!.Value)
                .ToList();
        }
        
        // Construction Details from Features map
        if (listing.Features != null)
        {
            if (listing.Features.TryGetValue("Daktype", out var roofType)) listing.RoofType = roofType;
            if (listing.Features.TryGetValue("Dak", out var roof)) listing.RoofType ??= roof;
            if (listing.Features.TryGetValue("Aantal woonlagen", out var floors)) listing.NumberOfFloors = ParseFirstNumber(floors);
            if (listing.Features.TryGetValue("Bouwperiode", out var period)) listing.ConstructionPeriod = period;
            if (listing.Features.TryGetValue("CV-ketel", out var cvKetel))
            {
                // Parse "Vaillant (2019)"
                var cvMatch = System.Text.RegularExpressions.Regex.Match(cvKetel, @"(.+?)\s*\((\d{4})\)");
                if (cvMatch.Success)
                {
                    listing.CVBoilerBrand = cvMatch.Groups[1].Value.Trim();
                    if (int.TryParse(cvMatch.Groups[2].Value, out var cvYear)) listing.CVBoilerYear = cvYear;
                }
                else
                {
                    listing.CVBoilerBrand = cvKetel;
                }
            }
        }
    }

    private static void FlattenFeatures(List<FundaNuxtFeatureItem>? items, Dictionary<string, string> map)
    {
        if (items == null) return;

        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.Label))
            {
                // If it has a value, store it
                if (!string.IsNullOrEmpty(item.Value))
                {
                    // Clean up label to be more standard key if needed, or just use as is
                    // Funda labels: "Aantal kamers", "Wonen", "Perceel", "Energielabel"
                    map.TryAdd(item.Label.Trim(), item.Value.Trim());
                }
                
                // Recurse (e.g., specific dimensions under a room, or cadastral details)
                if (item.KenmerkenList != null && item.KenmerkenList.Count > 0)
                {
                    FlattenFeatures(item.KenmerkenList, map);
                }
            }
            else if (item.KenmerkenList != null)
            {
                // Sometimes label is on the group (Title) but here we process items.
                // If checking FeatureGroup.Title is needed, it must be passed down.
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

    [GeneratedRegex(@"(\d+)\s*slaapkamer", RegexOptions.IgnoreCase)]
    private static partial Regex BedroomRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();

    private static Listing MapApiListingToDomain(FundaApiListing apiListing, string fundaId)
    {
        var fullUrl = apiListing.ListingUrl!.StartsWith("http")
            ? apiListing.ListingUrl
            : $"https://www.funda.nl{apiListing.ListingUrl}";

        var price = ParsePriceFromApi(apiListing.Price);

        // Note: API provides limited details compared to HTML scraping.
        // We initialize with nulls where data is missing in API, and fill it later.
        return new Listing
        {
            FundaId = fundaId,
            AgentName = apiListing.AgentName,
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
        
        // New fields from extended APIs
        if (listing.BrokerOfficeId.HasValue) existingListing.BrokerOfficeId = listing.BrokerOfficeId;
        if (!string.IsNullOrEmpty(listing.BrokerPhone)) existingListing.BrokerPhone = listing.BrokerPhone;
        if (!string.IsNullOrEmpty(listing.BrokerLogoUrl)) existingListing.BrokerLogoUrl = listing.BrokerLogoUrl;
        if (!string.IsNullOrEmpty(listing.BrokerAssociationCode)) existingListing.BrokerAssociationCode = listing.BrokerAssociationCode;
        if (listing.FiberAvailable.HasValue) existingListing.FiberAvailable = listing.FiberAvailable;
        if (listing.PublicationDate.HasValue) existingListing.PublicationDate = listing.PublicationDate;
        existingListing.IsSoldOrRented = listing.IsSoldOrRented;
        if (listing.Labels.Count > 0) existingListing.Labels = listing.Labels;
        if (!string.IsNullOrEmpty(listing.PostalCode)) existingListing.PostalCode = listing.PostalCode;
        if (!string.IsNullOrEmpty(listing.AgentName)) existingListing.AgentName = listing.AgentName;

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