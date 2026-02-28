namespace Valora.Application.DTOs.Listings;

public record ListingSearchRequest(
    string? City,
    string? PostalCode,
    double? MinLat,
    double? MinLon,
    double? MaxLat,
    double? MaxLon,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinArea,
    string? PropertyType,
    string? EnergyLabel,
    int? MinYearBuilt,
    int? MaxYearBuilt,
    string? SortBy = "newest", // relevance, newest, price, pricePerSqm, commute
    int Page = 1,
    int PageSize = 20
);
