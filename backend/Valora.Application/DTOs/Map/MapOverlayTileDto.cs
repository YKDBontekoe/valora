namespace Valora.Application.DTOs.Map;

public record MapOverlayTileDto(
    double Latitude,
    double Longitude,
    double Size,
    double Value,
    string DisplayValue
);
