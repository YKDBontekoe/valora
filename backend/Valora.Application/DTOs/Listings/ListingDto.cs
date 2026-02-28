namespace Valora.Application.DTOs.Listings;

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
    string? PropertyType,
    string? Status,
    string? ImageUrl,
    DateTime? ListedDate,
    string? EnergyLabel,
    int? YearBuilt,
    double? Latitude,
    double? Longitude,
    double? ContextCompositeScore
);
