namespace Valora.Application.DTOs.Map;

public record MapAmenityClusterDto(
    double Latitude,
    double Longitude,
    int Count,
    Dictionary<string, int> TypeCounts
);
