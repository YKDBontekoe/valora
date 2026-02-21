using Valora.Application.DTOs.Map;

namespace Valora.Application.DTOs.Property;

public record PropertyDetailDto(
    Guid Id,
    string? FundaId,
    decimal? Price,
    string Address,
    string? City,
    string? PostalCode,
    int? Bedrooms,
    int? Bathrooms,
    int? LivingAreaM2,
    string? EnergyLabel,
    string? Description,
    List<string> ImageUrls,
    double? ContextCompositeScore,
    double? ContextSafetyScore,
    double? ContextSocialScore,
    double? ContextAmenitiesScore,
    double? ContextEnvironmentScore,
    decimal? PricePerM2,
    decimal? NeighborhoodAvgPriceM2,
    double? PricePercentile, // 0-100 indicating where this property stands
    List<MapAmenityDto> NearbyAmenities,
    double? Latitude,
    double? Longitude
);
