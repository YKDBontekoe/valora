using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class ListingService : IListingService
{
    private readonly IListingRepository _listingRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly ILogger<ListingService> _logger;

    public ListingService(
        IListingRepository listingRepository,
        IPriceHistoryRepository priceHistoryRepository,
        ILogger<ListingService> logger)
    {
        _listingRepository = listingRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _logger = logger;
    }

    public async Task CreateListingAsync(Listing listing, CancellationToken cancellationToken = default)
    {
        // Set default status if missing
        listing.Status ??= "Beschikbaar";

        await _listingRepository.AddAsync(listing, cancellationToken);
        _logger.LogInformation("Created new listing: {FundaId}", listing.FundaId);

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

    public async Task UpdateListingAsync(Listing existingListing, Listing newListing, CancellationToken cancellationToken = default)
    {
        // Check for price change
        var priceChanged = existingListing.Price != newListing.Price && newListing.Price.HasValue;

        if (priceChanged)
        {
            _logger.LogInformation(
                "Price changed for {FundaId}: {OldPrice} -> {NewPrice}",
                newListing.FundaId, existingListing.Price, newListing.Price);

            await _priceHistoryRepository.AddAsync(new PriceHistory
            {
                ListingId = existingListing.Id,
                Price = newListing.Price!.Value
            }, cancellationToken);
        }

        // Update properties
        existingListing.Price = newListing.Price;
        existingListing.ImageUrl = newListing.ImageUrl;

        MergeListingDetails(existingListing, newListing);

        await _listingRepository.UpdateAsync(existingListing, cancellationToken);
        _logger.LogDebug("Updated listing: {FundaId}", newListing.FundaId);
    }

    private void MergeListingDetails(Listing target, Listing source)
    {
        if (source.Bedrooms.HasValue) target.Bedrooms = source.Bedrooms;
        if (source.LivingAreaM2.HasValue) target.LivingAreaM2 = source.LivingAreaM2;
        if (source.PlotAreaM2.HasValue) target.PlotAreaM2 = source.PlotAreaM2;
        if (!string.IsNullOrEmpty(source.Status)) target.Status = source.Status;

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

        // Ensure Url is updated if available and different?
        if (!string.IsNullOrEmpty(source.Url)) target.Url = source.Url;

        // Also merge rich data fields if available in source (e.g. from Nuxt enrichment)
        if (!string.IsNullOrEmpty(source.Description)) target.Description = source.Description;
        if (source.Features != null && source.Features.Count > 0) target.Features = source.Features;

        // Images/Media
        if (source.ImageUrls != null && source.ImageUrls.Count > 0) target.ImageUrls = source.ImageUrls;
        if (!string.IsNullOrEmpty(source.VideoUrl)) target.VideoUrl = source.VideoUrl;
        if (!string.IsNullOrEmpty(source.VirtualTourUrl)) target.VirtualTourUrl = source.VirtualTourUrl;
        if (source.FloorPlanUrls != null && source.FloorPlanUrls.Count > 0) target.FloorPlanUrls = source.FloorPlanUrls;
        if (!string.IsNullOrEmpty(source.BrochureUrl)) target.BrochureUrl = source.BrochureUrl;

        // Construction/Details
        if (!string.IsNullOrEmpty(source.EnergyLabel)) target.EnergyLabel = source.EnergyLabel;
        if (source.YearBuilt.HasValue) target.YearBuilt = source.YearBuilt;
        if (!string.IsNullOrEmpty(source.RoofType)) target.RoofType = source.RoofType;
        if (source.NumberOfFloors.HasValue) target.NumberOfFloors = source.NumberOfFloors;
        if (!string.IsNullOrEmpty(source.ConstructionPeriod)) target.ConstructionPeriod = source.ConstructionPeriod;
        if (!string.IsNullOrEmpty(source.CVBoilerBrand)) target.CVBoilerBrand = source.CVBoilerBrand;
        if (source.CVBoilerYear.HasValue) target.CVBoilerYear = source.CVBoilerYear;

        // Geo
        if (source.Latitude.HasValue) target.Latitude = source.Latitude;
        if (source.Longitude.HasValue) target.Longitude = source.Longitude;

        // Insights
        if (source.ViewCount.HasValue) target.ViewCount = source.ViewCount;
        if (source.SaveCount.HasValue) target.SaveCount = source.SaveCount;
        if (source.NeighborhoodPopulation.HasValue) target.NeighborhoodPopulation = source.NeighborhoodPopulation;
        if (source.NeighborhoodAvgPriceM2.HasValue) target.NeighborhoodAvgPriceM2 = source.NeighborhoodAvgPriceM2;

        // Timestamps
        if (source.LastFundaFetchUtc.HasValue) target.LastFundaFetchUtc = source.LastFundaFetchUtc;
    }
}
