using Valora.Application.DTOs;
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
        var listing = new Listing
        {
            Id = dto.Id,
            FundaId = dto.FundaId,
            Address = dto.Address
        };
        UpdateEntity(listing, dto);
        return listing;
    }

    public static void UpdateEntity(Listing listing, ListingDto dto)
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
