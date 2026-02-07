using System.Text.RegularExpressions;
using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

internal static partial class FundaMapper
{
    private const string ProjectType = "Nieuwbouwproject";
    private const string HouseType = "Woonhuis";

    public static Listing MapApiListingToDomain(FundaApiListing apiListing, string fundaId)
    {
        var listingUrl = apiListing.ListingUrl ?? "";
        var fullUrl = listingUrl.StartsWith("http")
            ? apiListing.ListingUrl
            : $"https://www.funda.nl{listingUrl}";

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

    public static void EnrichListingWithSummary(Listing listing, FundaApiListingSummary summary)
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

    public static void EnrichListingWithNuxtData(Listing listing, FundaNuxtListingData data)
    {
        listing.Description = data.Description?.Content;

        EnrichFeatures(listing, data);
        EnrichMedia(listing, data);
        EnrichCoordinates(listing, data);
        EnrichInsights(listing, data);
        EnrichMiscellaneous(listing, data);

        // Additional enrichment if features are present
        if (listing.Features != null)
        {
            EnrichConstructionDetails(listing, listing.Features);
        }
    }

    private static void EnrichFeatures(Listing listing, FundaNuxtListingData data)
    {
        if (data.Features == null) return;

        // Living Area & Plot Area from ObjectType
        if (data.ObjectType?.PropertySpecification != null)
        {
            listing.LivingAreaM2 = data.ObjectType.PropertySpecification.SelectedArea;
            listing.PlotAreaM2 = data.ObjectType.PropertySpecification.SelectedPlotArea;
        }

        var featureMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Populate map from various feature groups
        if (data.Features.Indeling != null) PopulateFeatureMap(data.Features.Indeling.KenmerkenList, featureMap);
        if (data.Features.Afmetingen != null) PopulateFeatureMap(data.Features.Afmetingen.KenmerkenList, featureMap);
        if (data.Features.Energie != null) PopulateFeatureMap(data.Features.Energie.KenmerkenList, featureMap);
        if (data.Features.Bouw != null) PopulateFeatureMap(data.Features.Bouw.KenmerkenList, featureMap);

        listing.Features = featureMap;

        // Extract specific attributes from the flat map
        SetAreasFromFeatures(listing, featureMap);
        SetRoomsAndBathrooms(listing, featureMap);
        SetEnergyAndConstruction(listing, featureMap);
        SetGardenAndParking(listing, featureMap);
        SetCadastralDesignation(listing, featureMap);
    }

    private static void SetAreasFromFeatures(Listing listing, Dictionary<string, string> featureMap)
    {
        if (!listing.LivingAreaM2.HasValue && featureMap.TryGetValue("Wonen", out var livingArea))
            listing.LivingAreaM2 = ParseFirstNumber(livingArea);

        if (!listing.PlotAreaM2.HasValue && featureMap.TryGetValue("Perceel", out var plotArea))
            listing.PlotAreaM2 = ParseFirstNumber(plotArea);

        if (featureMap.TryGetValue("Gebouwgebonden buitenruimte", out var balcony))
            listing.BalconyM2 = ParseFirstNumber(balcony);

        if (featureMap.TryGetValue("Externe bergruimte", out var storage))
            listing.ExternalStorageM2 = ParseFirstNumber(storage);

        if (featureMap.TryGetValue("Inhoud", out var volume))
            listing.VolumeM3 = ParseFirstNumber(volume);

        // Garden Area Logic: Find largest area mentioned in "Tuin" related keys
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
    }

    private static void SetRoomsAndBathrooms(Listing listing, Dictionary<string, string> featureMap)
    {
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
    }

    private static void SetEnergyAndConstruction(Listing listing, Dictionary<string, string> featureMap)
    {
        if (featureMap.TryGetValue("Energielabel", out var label)) listing.EnergyLabel = label.Trim();
        if (featureMap.TryGetValue("Isolatie", out var insulation)) listing.InsulationType = insulation;
        if (featureMap.TryGetValue("Verwarming", out var heating)) listing.HeatingType = heating;
        if (featureMap.TryGetValue("Bouwjaar", out var year)) listing.YearBuilt = ParseFirstNumber(year);
        if (featureMap.TryGetValue("Eigendomssituatie", out var ownership)) listing.OwnershipType = ownership;

        if (featureMap.TryGetValue("Bijdrage VvE", out var vveRaw))
        {
             var vveClean = PriceCleanupRegex().Replace(vveRaw, "");
             if (decimal.TryParse(vveClean, out var vveCost)) listing.VVEContribution = vveCost;
        }
    }

    private static void SetGardenAndParking(Listing listing, Dictionary<string, string> featureMap)
    {
        foreach (var kvp in featureMap)
        {
            if (kvp.Key.Contains("Tuin", StringComparison.OrdinalIgnoreCase) || kvp.Key.Contains("Buitenruimte", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value)) listing.GardenOrientation = kvp.Value;
            }

            if (kvp.Key.Equals("Ligging", StringComparison.OrdinalIgnoreCase) && featureMap.ContainsKey("Tuin"))
            {
                 listing.GardenOrientation = kvp.Value;
            }

            if (kvp.Key.Contains("Garage", StringComparison.OrdinalIgnoreCase)) listing.HasGarage = true;

            if (kvp.Key.Contains("Parkeerfaciliteiten", StringComparison.OrdinalIgnoreCase)) listing.ParkingType = kvp.Value;
        }
    }

    /// <summary>
    /// Attempts to extract the cadastral designation (e.g., "A 1234") from feature keys.
    /// <para>
    /// <strong>Heuristic:</strong>
    /// Cadastral data often appears as a key in the feature list where the key itself contains the data
    /// (e.g., "Section A 1234") and the value is empty or generic (e.g., "Title").
    /// We look for keys that:
    /// - Contain uppercase letters and digits.
    /// - Are longer than 5 chars (to avoid noise).
    /// - Do NOT contain common words like "kamers" or "bouw".
    /// </para>
    /// </summary>
    private static void SetCadastralDesignation(Listing listing, Dictionary<string, string> featureMap)
    {
        foreach (var key in featureMap.Keys)
        {
            // Heuristic check for cadastral string pattern
            if (key.Any(char.IsUpper) &&
                key.Any(char.IsDigit) &&
                key.Length > 5 &&
                !key.Contains("kamers", StringComparison.OrdinalIgnoreCase) &&
                !key.Contains("bouw", StringComparison.OrdinalIgnoreCase))
            {
                 // Check if the value suggests the key holds the information
                 if (featureMap.TryGetValue(key, out var val) && (string.IsNullOrEmpty(val) || val == "Title"))
                 {
                     listing.CadastralDesignation = key;
                     break;
                 }
            }
        }
    }

    private static void EnrichMedia(Listing listing, FundaNuxtListingData data)
    {
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

        if (data.Videos != null && data.Videos.Count > 0)
        {
            listing.VideoUrl = data.Videos[0].Url;
        }

        if (data.Photos360 != null && data.Photos360.Count > 0)
        {
            listing.VirtualTourUrl = data.Photos360[0].Url;
        }

        if (data.FloorPlans != null)
        {
            listing.FloorPlanUrls = data.FloorPlans
                .Where(fp => !string.IsNullOrEmpty(fp.Url) || !string.IsNullOrEmpty(fp.Id))
                .Select(fp => fp.Url ?? $"https://cloud.funda.nl/valentina_media/{fp.Id}_720.jpg")
                .ToList();
        }

        listing.BrochureUrl = data.BrochureUrl;
    }

    private static void EnrichCoordinates(Listing listing, FundaNuxtListingData data)
    {
        if (data.Coordinates != null)
        {
            listing.Latitude = data.Coordinates.Lat;
            listing.Longitude = data.Coordinates.Lng;
        }
    }

    private static void EnrichInsights(Listing listing, FundaNuxtListingData data)
    {
        if (data.ObjectInsights != null)
        {
            listing.ViewCount = data.ObjectInsights.Views;
            listing.SaveCount = data.ObjectInsights.Saves;
        }

        if (data.LocalInsights != null)
        {
            listing.NeighborhoodPopulation = data.LocalInsights.Inhabitants;
            listing.NeighborhoodAvgPriceM2 = data.LocalInsights.AvgPricePerM2;
        }
    }

    private static void EnrichMiscellaneous(Listing listing, FundaNuxtListingData data)
    {
        if (data.OpenHouseDates != null)
        {
            listing.OpenHouseDates = data.OpenHouseDates
                .Where(oh => oh.Date.HasValue)
                .Select(oh => oh.Date!.Value)
                .ToList();
        }
    }

    private static void EnrichConstructionDetails(Listing listing, Dictionary<string, string> featureMap)
    {
        if (featureMap.TryGetValue("Daktype", out var roofType)) listing.RoofType = roofType;
        if (featureMap.TryGetValue("Dak", out var roof)) listing.RoofType ??= roof;
        if (featureMap.TryGetValue("Aantal woonlagen", out var floors)) listing.NumberOfFloors = ParseFirstNumber(floors);
        if (featureMap.TryGetValue("Bouwperiode", out var period)) listing.ConstructionPeriod = period;
        if (featureMap.TryGetValue("CV-ketel", out var cvKetel))
        {
            var cvMatch = CVBoilerRegex().Match(cvKetel);
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

    public static void MergeListingDetails(Listing target, Listing source)
    {
        // We do NOT overwrite fields that might have been enriched manually or by previous scraper if they are null in the new source
        if (source.Bedrooms.HasValue) target.Bedrooms = source.Bedrooms;
        if (source.LivingAreaM2.HasValue) target.LivingAreaM2 = source.LivingAreaM2;
        if (source.PlotAreaM2.HasValue) target.PlotAreaM2 = source.PlotAreaM2;
        if (!string.IsNullOrEmpty(source.Status)) target.Status = source.Status;

        // New fields from extended APIs
        if (source.BrokerOfficeId.HasValue) target.BrokerOfficeId = source.BrokerOfficeId;
        if (!string.IsNullOrEmpty(source.BrokerPhone)) target.BrokerPhone = source.BrokerPhone;
        if (!string.IsNullOrEmpty(source.BrokerLogoUrl)) target.BrokerLogoUrl = source.BrokerLogoUrl;
        if (!string.IsNullOrEmpty(source.BrokerAssociationCode)) target.BrokerAssociationCode = source.BrokerAssociationCode;
        if (source.FiberAvailable.HasValue) target.FiberAvailable = source.FiberAvailable;
        if (source.PublicationDate.HasValue) target.PublicationDate = source.PublicationDate;

        target.IsSoldOrRented = source.IsSoldOrRented;

        if (source.Labels != null && source.Labels.Count > 0) target.Labels = source.Labels;
        if (!string.IsNullOrEmpty(source.PostalCode)) target.PostalCode = source.PostalCode;
        if (!string.IsNullOrEmpty(source.AgentName)) target.AgentName = source.AgentName;
    }

    private static void PopulateFeatureMap(List<FundaNuxtFeatureItem>? items, Dictionary<string, string> map)
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
                    map.TryAdd(item.Label.Trim(), item.Value.Trim());
                }

                // Recurse (e.g., specific dimensions under a room, or cadastral details)
                if (item.KenmerkenList != null && item.KenmerkenList.Count > 0)
                {
                    PopulateFeatureMap(item.KenmerkenList, map);
                }
            }
            else if (item.KenmerkenList != null)
            {
                // Sometimes label is on the group (Title) but here we process items.
                // If checking FeatureGroup.Title is needed, it must be passed down.
                PopulateFeatureMap(item.KenmerkenList, map);
            }
        }
    }

    public static int? ParseFirstNumber(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var match = NumberRegex().Match(text);
        if (match.Success && int.TryParse(match.Value, out var num))
        {
            return num;
        }
        return null;
    }

    private static decimal? ParsePriceFromApi(string? priceText)
    {
        if (string.IsNullOrEmpty(priceText)) return null;

        // Remove currency symbol, periods (thousands separator), and suffixes like "k.k."
        var cleaned = PriceCleanupRegex().Replace(priceText, "");
        if (decimal.TryParse(cleaned, out var price))
        {
            return price;
        }
        return null;
    }

    // --- Regex Patterns ---

    [GeneratedRegex(@"(\d+)\s*slaapkamer", RegexOptions.IgnoreCase)]
    private static partial Regex BedroomRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"(.+?)\s*\((\d{4})\)")]
    private static partial Regex CVBoilerRegex();

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex PriceCleanupRegex();
}
