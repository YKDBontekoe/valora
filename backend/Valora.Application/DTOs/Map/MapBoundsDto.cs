namespace Valora.Application.DTOs.Map;

public record MapBoundsDto(
    double MinLat,
    double MinLon,
    double MaxLat,
    double MaxLon);
