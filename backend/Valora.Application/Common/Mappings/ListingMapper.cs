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
            reportDto.AmenityMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
            reportDto.EnvironmentMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
            reportDto.CompositeScore,
            reportDto.CategoryScores.ToDictionary(k => k.Key, k => k.Value),
            reportDto.Sources.Select(s => new Valora.Domain.Models.SourceAttributionModel(s.Source, s.Url, s.License, s.RetrievedAtUtc)).ToList(),
            reportDto.Warnings.ToList()
        );
    }
}
