using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class ListingService : IListingService
{
    private readonly IListingRepository _repository;

    public ListingService(IListingRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedList<ListingDto>> GetAllAsync(ListingFilterDto filter, CancellationToken cancellationToken)
    {
        return await _repository.GetAllAsync(filter, cancellationToken);
    }

    public async Task<ListingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var listing = await _repository.GetByIdAsync(id, cancellationToken);
        if (listing is null) return null;
        return MapToDto(listing);
    }

    private static ListingDto MapToDto(Listing listing)
    {
        return new ListingDto(
            listing.Id, listing.FundaId, listing.Address, listing.City, listing.PostalCode, listing.Price,
            listing.Bedrooms, listing.Bathrooms, listing.LivingAreaM2, listing.PlotAreaM2,
            listing.PropertyType, listing.Status, listing.Url, listing.ImageUrl, listing.ListedDate, listing.CreatedAt,
            // Rich Data
            listing.Description, listing.EnergyLabel, listing.YearBuilt, listing.ImageUrls,
            // Phase 2
            listing.OwnershipType, listing.CadastralDesignation, listing.VVEContribution, listing.HeatingType,
            listing.InsulationType, listing.GardenOrientation, listing.HasGarage, listing.ParkingType,
            // Phase 3
            listing.AgentName, listing.VolumeM3, listing.BalconyM2, listing.GardenM2, listing.ExternalStorageM2,
            listing.Features,
            // Geo & Media
            listing.Latitude, listing.Longitude, listing.VideoUrl, listing.VirtualTourUrl, listing.FloorPlanUrls, listing.BrochureUrl,
            // Construction
            listing.RoofType, listing.NumberOfFloors, listing.ConstructionPeriod, listing.CVBoilerBrand, listing.CVBoilerYear,
            // Broker
            listing.BrokerPhone, listing.BrokerLogoUrl,
            // Infra
            listing.FiberAvailable,
            // Status
            listing.PublicationDate, listing.IsSoldOrRented, listing.Labels
        );
    }
}
