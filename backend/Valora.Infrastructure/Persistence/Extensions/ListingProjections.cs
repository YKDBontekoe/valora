using System.Linq.Expressions;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Extensions;

public static class ListingProjections
{
    public static readonly Expression<Func<Listing, ListingDto>> ToDto = l => new ListingDto(
        l.Id,
        l.FundaId,
        l.Address,
        l.City,
        l.PostalCode,
        l.Price,
        l.Bedrooms,
        l.Bathrooms,
        l.LivingAreaM2,
        l.PlotAreaM2,
        l.PropertyType,
        l.Status,
        l.Url,
        l.ImageUrl,
        l.ListedDate,
        l.CreatedAt,
        // Rich Data
        l.Description, l.EnergyLabel, l.YearBuilt, l.ImageUrls,
        // Phase 2
        l.OwnershipType, l.CadastralDesignation, l.VVEContribution, l.HeatingType,
        l.InsulationType, l.GardenOrientation, l.HasGarage, l.ParkingType,
        // Phase 3
        l.AgentName, l.VolumeM3, l.BalconyM2, l.GardenM2, l.ExternalStorageM2,
        l.Features,
        // Geo & Media
        l.Latitude, l.Longitude, l.VideoUrl, l.VirtualTourUrl, l.FloorPlanUrls, l.BrochureUrl,
        // Construction
        l.RoofType, l.NumberOfFloors, l.ConstructionPeriod, l.CVBoilerBrand, l.CVBoilerYear,
        // Broker
        l.BrokerPhone, l.BrokerLogoUrl,
        // Infra
        l.FiberAvailable,
        // Status
        l.PublicationDate, l.IsSoldOrRented, l.Labels,
        // Phase 6: WOZ
        null, null, null,
        // Context
        l.ContextCompositeScore, l.ContextSafetyScore, l.ContextReport
    );

    public static readonly Expression<Func<Listing, ListingSummaryDto>> ToSummaryDto = l => new ListingSummaryDto(
        l.Id,
        l.FundaId,
        l.Address,
        l.City,
        l.PostalCode,
        l.Price,
        l.Bedrooms,
        l.Bathrooms,
        l.LivingAreaM2,
        l.PlotAreaM2,
        l.PropertyType,
        l.Status,
        l.Url,
        l.ImageUrl,
        l.ListedDate,
        l.CreatedAt,
        l.EnergyLabel,
        l.IsSoldOrRented,
        l.Labels
    );
}
