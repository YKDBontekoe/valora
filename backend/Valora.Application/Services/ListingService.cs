using System.ComponentModel.DataAnnotations;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Mappings;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using ValidationException = Valora.Application.Common.Exceptions.ValidationException;

namespace Valora.Application.Services;

public class ListingService : IListingService
{
    private readonly IListingRepository _repository;
    private readonly IPdokListingService _pdokService;
    private readonly IContextReportService _contextReportService;

    public ListingService(
        IListingRepository repository,
        IPdokListingService pdokService,
        IContextReportService contextReportService)
    {
        _repository = repository;
        _pdokService = pdokService;
        _contextReportService = contextReportService;
    }

    public async Task<PaginatedList<ListingSummaryDto>> GetListingsAsync(ListingFilterDto filter, CancellationToken cancellationToken = default)
    {
        var validationContext = new ValidationContext(filter);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(filter, validationContext, validationResults, true))
        {
            var errors = validationResults
                .GroupBy(x => x.MemberNames.FirstOrDefault() ?? "General")
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage ?? "Unknown error").ToArray());

            throw new ValidationException(errors);
        }

        return await _repository.GetSummariesAsync(filter, cancellationToken);
    }

    public async Task<ListingDto?> GetListingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await _repository.GetByIdAsync(id, cancellationToken);
        if (listing == null)
        {
             return null;
        }
        return ListingMapper.ToDto(listing);
    }

    public async Task<ListingDto?> GetPdokListingAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
             throw new ValidationException(new[] { "ID is required" });
        }

        var listingDto = await _pdokService.GetListingDetailsAsync(id, cancellationToken);
        if (listingDto is null)
        {
            return null;
        }

        var existing = await _repository.GetByFundaIdAsync(listingDto.FundaId, cancellationToken);
        if (existing is null)
        {
            await _repository.AddAsync(MapToListingEntity(listingDto), cancellationToken);
        }
        else
        {
            UpdateListingEntity(existing, listingDto);
            await _repository.UpdateAsync(existing, cancellationToken);
        }

        return listingDto;
    }

    public async Task<double> EnrichListingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await _repository.GetByIdAsync(id, cancellationToken);
        if (listing is null)
        {
            throw new NotFoundException(nameof(Valora.Domain.Entities.Listing), id);
        }

        // 1. Generate Report
        ContextReportRequestDto request = new(Input: listing.Address); // Default 1km radius

        // We use the application DTO for the service call...
        var reportDto = await _contextReportService.BuildAsync(request, cancellationToken);

        // ...and map it to the Domain model for storage
        var contextReportModel = ListingMapper.MapToDomain(reportDto);

        // 2. Update Entity
        listing.ContextReport = contextReportModel;
        listing.ContextCompositeScore = reportDto.CompositeScore;

        if (reportDto.CategoryScores.TryGetValue("Social", out var social)) listing.ContextSocialScore = social;
        if (reportDto.CategoryScores.TryGetValue("Safety", out var crime)) listing.ContextSafetyScore = crime;
        if (reportDto.CategoryScores.TryGetValue("Amenities", out var amenities)) listing.ContextAmenitiesScore = amenities;
        if (reportDto.CategoryScores.TryGetValue("Environment", out var env)) listing.ContextEnvironmentScore = env;

        // 3. Save
        await _repository.UpdateAsync(listing, cancellationToken);

        return reportDto.CompositeScore;
    }

    private static Valora.Domain.Entities.Listing MapToListingEntity(ListingDto dto)
    {
        return new Valora.Domain.Entities.Listing
        {
            Id = dto.Id,
            FundaId = dto.FundaId,
            Address = dto.Address,
            City = dto.City,
            PostalCode = dto.PostalCode,
            Price = dto.Price,
            Bedrooms = dto.Bedrooms,
            Bathrooms = dto.Bathrooms,
            LivingAreaM2 = dto.LivingAreaM2,
            PlotAreaM2 = dto.PlotAreaM2,
            PropertyType = dto.PropertyType,
            Status = dto.Status,
            Url = dto.Url,
            ImageUrl = dto.ImageUrl,
            ListedDate = dto.ListedDate,
            Description = dto.Description,
            EnergyLabel = dto.EnergyLabel,
            YearBuilt = dto.YearBuilt,
            ImageUrls = dto.ImageUrls,
            OwnershipType = dto.OwnershipType,
            CadastralDesignation = dto.CadastralDesignation,
            VVEContribution = dto.VVEContribution,
            HeatingType = dto.HeatingType,
            InsulationType = dto.InsulationType,
            GardenOrientation = dto.GardenOrientation,
            HasGarage = dto.HasGarage,
            ParkingType = dto.ParkingType,
            AgentName = dto.AgentName,
            VolumeM3 = dto.VolumeM3,
            BalconyM2 = dto.BalconyM2,
            GardenM2 = dto.GardenM2,
            ExternalStorageM2 = dto.ExternalStorageM2,
            Features = dto.Features,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            VideoUrl = dto.VideoUrl,
            VirtualTourUrl = dto.VirtualTourUrl,
            FloorPlanUrls = dto.FloorPlanUrls,
            BrochureUrl = dto.BrochureUrl,
            RoofType = dto.RoofType,
            NumberOfFloors = dto.NumberOfFloors,
            ConstructionPeriod = dto.ConstructionPeriod,
            CVBoilerBrand = dto.CVBoilerBrand,
            CVBoilerYear = dto.CVBoilerYear,
            BrokerPhone = dto.BrokerPhone,
            BrokerLogoUrl = dto.BrokerLogoUrl,
            FiberAvailable = dto.FiberAvailable,
            PublicationDate = dto.PublicationDate,
            IsSoldOrRented = dto.IsSoldOrRented,
            Labels = dto.Labels,
            ContextCompositeScore = dto.ContextCompositeScore,
            ContextSafetyScore = dto.ContextSafetyScore,
            ContextReport = dto.ContextReport,
            LastFundaFetchUtc = DateTime.UtcNow
        };
    }

    private static void UpdateListingEntity(Valora.Domain.Entities.Listing listing, ListingDto dto)
    {
        listing.Address = dto.Address;
        listing.City = dto.City;
        listing.PostalCode = dto.PostalCode;
        listing.Price = dto.Price;
        listing.Bedrooms = dto.Bedrooms;
        listing.Bathrooms = dto.Bathrooms;
        listing.LivingAreaM2 = dto.LivingAreaM2;
        listing.PlotAreaM2 = dto.PlotAreaM2;
        listing.PropertyType = dto.PropertyType;
        listing.Status = dto.Status;
        listing.Url = dto.Url;
        listing.ImageUrl = dto.ImageUrl;
        listing.ListedDate = dto.ListedDate;
        listing.Description = dto.Description;
        listing.EnergyLabel = dto.EnergyLabel;
        listing.YearBuilt = dto.YearBuilt;
        listing.ImageUrls = dto.ImageUrls;
        listing.OwnershipType = dto.OwnershipType;
        listing.CadastralDesignation = dto.CadastralDesignation;
        listing.VVEContribution = dto.VVEContribution;
        listing.HeatingType = dto.HeatingType;
        listing.InsulationType = dto.InsulationType;
        listing.GardenOrientation = dto.GardenOrientation;
        listing.HasGarage = dto.HasGarage;
        listing.ParkingType = dto.ParkingType;
        listing.AgentName = dto.AgentName;
        listing.VolumeM3 = dto.VolumeM3;
        listing.BalconyM2 = dto.BalconyM2;
        listing.GardenM2 = dto.GardenM2;
        listing.ExternalStorageM2 = dto.ExternalStorageM2;
        listing.Features = dto.Features;
        listing.Latitude = dto.Latitude;
        listing.Longitude = dto.Longitude;
        listing.VideoUrl = dto.VideoUrl;
        listing.VirtualTourUrl = dto.VirtualTourUrl;
        listing.FloorPlanUrls = dto.FloorPlanUrls;
        listing.BrochureUrl = dto.BrochureUrl;
        listing.RoofType = dto.RoofType;
        listing.NumberOfFloors = dto.NumberOfFloors;
        listing.ConstructionPeriod = dto.ConstructionPeriod;
        listing.CVBoilerBrand = dto.CVBoilerBrand;
        listing.CVBoilerYear = dto.CVBoilerYear;
        listing.BrokerPhone = dto.BrokerPhone;
        listing.BrokerLogoUrl = dto.BrokerLogoUrl;
        listing.FiberAvailable = dto.FiberAvailable;
        listing.PublicationDate = dto.PublicationDate;
        listing.IsSoldOrRented = dto.IsSoldOrRented;
        listing.Labels = dto.Labels;
        listing.ContextCompositeScore = dto.ContextCompositeScore;
        listing.ContextSafetyScore = dto.ContextSafetyScore;
        listing.ContextReport = dto.ContextReport;
        listing.LastFundaFetchUtc = DateTime.UtcNow;
    }
}
