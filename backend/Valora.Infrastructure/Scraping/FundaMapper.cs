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

        var price = ParsePrice(apiListing.Price);

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
        if (summary.Address != null)
        {
            listing.Address = summary.Address.Street ?? listing.Address;
            listing.City = summary.Address.City ?? listing.City;
            listing.PostalCode = summary.Address.PostalCode ?? listing.PostalCode;
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

            if (!string.IsNullOrEmpty(summary.FastView.NumberOfBedrooms))
            {
                listing.Bedrooms = ParseFirstNumber(summary.FastView.NumberOfBedrooms);
            }

            listing.EnergyLabel = summary.FastView.EnergyLabel;
        }

        if (summary.Brokers != null && summary.Brokers.Count > 0)
        {
            listing.AgentName = summary.Brokers[0].Name;
        }

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

            // Cadastral
            foreach (var key in featureMap.Keys)
            {
                if (key.Any(char.IsUpper) && key.Any(char.IsDigit) && key.Length > 5 && !key.Contains("kamers") && !key.Contains("bouw"))
                {
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
    }

    public static void MergeListingDetails(Listing target, Listing source)
    {
        // Basic fields - overwrite if present in source (latest crawl)
        if (!string.IsNullOrEmpty(source.Address)) target.Address = source.Address;
        if (!string.IsNullOrEmpty(source.City)) target.City = source.City;
        if (source.Price.HasValue) target.Price = source.Price;
        if (!string.IsNullOrEmpty(source.ImageUrl)) target.ImageUrl = source.ImageUrl;
        if (!string.IsNullOrEmpty(source.Url)) target.Url = source.Url;

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

    public static decimal? ParsePrice(string? priceText)
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

    [GeneratedRegex(@"(\d+)\s*slaapkamer", RegexOptions.IgnoreCase)]
    private static partial Regex BedroomRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"(.+?)\s*\((\d{4})\)")]
    private static partial Regex CVBoilerRegex();

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex PriceCleanupRegex();
}
