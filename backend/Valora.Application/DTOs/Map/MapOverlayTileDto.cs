using System.Diagnostics.CodeAnalysis;

namespace Valora.Application.DTOs.Map;

[ExcludeFromCodeCoverage]
public record MapOverlayTileDto(
    double Latitude,
    double Longitude,
    double Size,
    double Value,
    string DisplayValue
);
