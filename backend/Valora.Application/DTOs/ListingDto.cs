namespace Valora.Application.DTOs;

public record ListingDto(
    Guid Id,
    string FundaId,
    string Address,
    string? City,
    string? PostalCode,
    decimal? Price,
    int? Bedrooms,
    int? Bathrooms,
    int? LivingAreaM2,
    int? PlotAreaM2,
    string? PropertyType,
    string? Status,
    string? Url,
    string? ImageUrl,
    DateTime? ListedDate,
    DateTime CreatedAt,
    // Rich Data
    string? Description,
    string? EnergyLabel,
    int? YearBuilt,
    List<string> ImageUrls,
    // Phase 2
    string? OwnershipType,
    string? CadastralDesignation,
    decimal? VVEContribution,
    string? HeatingType,
    string? InsulationType,
    string? GardenOrientation,
    bool HasGarage,
    string? ParkingType,
    // Phase 3
    string? AgentName,
    int? VolumeM3,
    int? BalconyM2,
    int? GardenM2,
    int? ExternalStorageM2,
    Dictionary<string, string> Features,
    // Geo & Media
    double? Latitude,
    double? Longitude,
    string? VideoUrl,
    string? VirtualTourUrl,
    List<string> FloorPlanUrls,
    string? BrochureUrl,
    // Construction
    string? RoofType,
    int? NumberOfFloors,
    string? ConstructionPeriod,
    string? CVBoilerBrand,
    int? CVBoilerYear,
    // Broker
    string? BrokerPhone,
    string? BrokerLogoUrl,
    // Infra
    bool? FiberAvailable,
    // Status
    DateTime? PublicationDate,
    bool IsSoldOrRented,
    List<string> Labels,
    
    // Phase 5: Context
    double? ContextCompositeScore,
    double? ContextSafetyScore,
    Valora.Domain.Models.ContextReportModel? ContextReport
);
