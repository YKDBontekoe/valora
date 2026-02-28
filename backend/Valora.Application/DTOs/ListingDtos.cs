using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public record ListingDetailDto(
    Guid Id,
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
    string? Description,
    string? EnergyLabel,
    int? YearBuilt,
    List<string> ImageUrls,
    double? Latitude,
    double? Longitude,

    // Scores
    double? ContextCompositeScore,
    double? ContextSafetyScore,
    double? ContextSocialScore,
    double? ContextAmenitiesScore,
    double? ContextEnvironmentScore,

    // The pre-generated report JSON
    Valora.Domain.Models.ContextReportModel? ContextReport
);
