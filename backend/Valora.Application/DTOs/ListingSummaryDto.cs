namespace Valora.Application.DTOs;

public record ListingSummaryDto(
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
    string? EnergyLabel,
    bool IsSoldOrRented,
    IReadOnlyList<string> Labels,
    double? ContextCompositeScore,
    int? WozValue
);
