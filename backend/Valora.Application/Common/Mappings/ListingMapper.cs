using Valora.Application.DTOs;
using Valora.Domain.Common;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Mappings;

public static class ListingMapper
{
    public static ListingDto ToDto(Listing listing)
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
            listing.PublicationDate, listing.IsSoldOrRented, listing.Labels,
            // Phase 6: WOZ
            null, null, null,
            // Phase 5: Context
            listing.ContextCompositeScore, listing.ContextSafetyScore, listing.ContextReport
        );
    }

    public static Valora.Domain.Models.ContextReportModel MapToDomain(ContextReportDto reportDto)
    {
        return new Valora.Domain.Models.ContextReportModel(
            new Valora.Domain.Models.ResolvedLocationModel(
                reportDto.Location.Query, reportDto.Location.DisplayAddress,
                reportDto.Location.Latitude, reportDto.Location.Longitude,
                reportDto.Location.RdX, reportDto.Location.RdY,
                reportDto.Location.MunicipalityCode, reportDto.Location.MunicipalityName,
                reportDto.Location.DistrictCode, reportDto.Location.DistrictName,
                reportDto.Location.NeighborhoodCode, reportDto.Location.NeighborhoodName,
                reportDto.Location.PostalCode),
            reportDto.SocialMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
            reportDto.CrimeMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
            reportDto.DemographicsMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
            reportDto.HousingMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
            reportDto.MobilityMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
            reportDto.AmenityMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
            reportDto.EnvironmentMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
            reportDto.CompositeScore,
            reportDto.CategoryScores.ToDictionary(k => k.Key, k => k.Value),
            reportDto.Sources.Select(s => new Valora.Domain.Models.SourceAttributionModel(s.Source, s.Url, s.License, s.RetrievedAtUtc)).ToList(),
            reportDto.Warnings.ToList()
        );
    }

    public static Listing ToEntity(ListingDto dto)
    {
        // Address is required, but we must ensure it fits
        var address = dto.Address.Truncate(ValidationConstants.Listing.AddressMaxLength)
                      ?? throw new Valora.Application.Common.Exceptions.ValidationException(new Dictionary<string, string[]> { { "Address", new[] { "Address cannot be null or empty." } } });

        // FundaId must be exact and valid. No silent truncation.
        if (dto.FundaId.Length > ValidationConstants.Listing.FundaIdMaxLength)
        {
             throw new Valora.Application.Common.Exceptions.ValidationException(new Dictionary<string, string[]>
             {
                 { "FundaId", new[] { $"FundaId exceeds maximum length of {ValidationConstants.Listing.FundaIdMaxLength}." } }
             });
        }

        var listing = new Listing
        {
            Id = dto.Id,
            FundaId = dto.FundaId,
            Address = address
        };
        UpdateEntity(listing, dto);
        return listing;
    }

    public static void UpdateEntity(Listing listing, ListingDto dto)
    {
        // Address is required
        listing.Address = dto.Address.Truncate(ValidationConstants.Listing.AddressMaxLength)
                          ?? throw new Valora.Application.Common.Exceptions.ValidationException(new Dictionary<string, string[]> { { "Address", new[] { "Address cannot be null or empty." } } });

        // URL validation (Prevent truncation which breaks links)
        if (dto.Url?.Length > ValidationConstants.Listing.UrlMaxLength)
        {
             throw new Valora.Application.Common.Exceptions.ValidationException(new Dictionary<string, string[]>
             {
                 { "Url", new[] { $"Url exceeds maximum length of {ValidationConstants.Listing.UrlMaxLength}." } }
             });
        }
        listing.Url = dto.Url;

        // ImageUrl validation
        if (dto.ImageUrl?.Length > ValidationConstants.Listing.ImageUrlMaxLength)
        {
             throw new Valora.Application.Common.Exceptions.ValidationException(new Dictionary<string, string[]>
             {
                 { "ImageUrl", new[] { $"ImageUrl exceeds maximum length of {ValidationConstants.Listing.ImageUrlMaxLength}." } }
             });
        }
        listing.ImageUrl = dto.ImageUrl;

        listing.City = dto.City.Truncate(ValidationConstants.Listing.CityMaxLength);
        listing.PostalCode = dto.PostalCode.Truncate(ValidationConstants.Listing.PostalCodeMaxLength);
        listing.Price = dto.Price;
        listing.Bedrooms = dto.Bedrooms;
        listing.Bathrooms = dto.Bathrooms;
        listing.LivingAreaM2 = dto.LivingAreaM2;
        listing.PlotAreaM2 = dto.PlotAreaM2;
        listing.PropertyType = dto.PropertyType.Truncate(ValidationConstants.Listing.PropertyTypeMaxLength);
        listing.Status = dto.Status.Truncate(ValidationConstants.Listing.StatusMaxLength);
        listing.ListedDate = dto.ListedDate;
        listing.Description = dto.Description; // No max length on Description? Usually unbounded or large.
        listing.EnergyLabel = dto.EnergyLabel.Truncate(ValidationConstants.Listing.EnergyLabelMaxLength);
        listing.YearBuilt = dto.YearBuilt;
        listing.ImageUrls = dto.ImageUrls;
        listing.OwnershipType = dto.OwnershipType.Truncate(ValidationConstants.Listing.OwnershipTypeMaxLength);
        listing.CadastralDesignation = dto.CadastralDesignation.Truncate(ValidationConstants.Listing.CadastralDesignationMaxLength);
        listing.VVEContribution = dto.VVEContribution;
        listing.HeatingType = dto.HeatingType.Truncate(ValidationConstants.Listing.HeatingTypeMaxLength);
        listing.InsulationType = dto.InsulationType.Truncate(ValidationConstants.Listing.InsulationTypeMaxLength);
        listing.GardenOrientation = dto.GardenOrientation.Truncate(ValidationConstants.Listing.GardenOrientationMaxLength);
        listing.HasGarage = dto.HasGarage;
        listing.ParkingType = dto.ParkingType.Truncate(ValidationConstants.Listing.ParkingTypeMaxLength);
        listing.AgentName = dto.AgentName.Truncate(ValidationConstants.Listing.AgentNameMaxLength);
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
        listing.RoofType = dto.RoofType.Truncate(ValidationConstants.Listing.RoofTypeMaxLength);
        listing.NumberOfFloors = dto.NumberOfFloors;
        listing.ConstructionPeriod = dto.ConstructionPeriod.Truncate(ValidationConstants.Listing.ConstructionPeriodMaxLength);
        listing.CVBoilerBrand = dto.CVBoilerBrand.Truncate(ValidationConstants.Listing.CVBoilerBrandMaxLength);
        listing.CVBoilerYear = dto.CVBoilerYear;
        listing.BrokerPhone = dto.BrokerPhone.Truncate(ValidationConstants.Listing.BrokerPhoneMaxLength);
        listing.BrokerLogoUrl = dto.BrokerLogoUrl; // No explicit max length in config, assumingly handled or large enough
        // BrokerAssociationCode not present in DTO
        listing.FiberAvailable = dto.FiberAvailable;
        listing.PublicationDate = dto.PublicationDate;
        listing.IsSoldOrRented = dto.IsSoldOrRented;
        listing.Labels = dto.Labels;
        listing.ContextCompositeScore = dto.ContextCompositeScore;
        listing.ContextSafetyScore = dto.ContextSafetyScore;
        listing.ContextReport = dto.ContextReport;
    }
}
