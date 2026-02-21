namespace Valora.Application.DTOs.Map;

public record MapPropertyDto(
    Guid Id,
    decimal? Price,
    double Latitude,
    double Longitude,
    string? Status
);
