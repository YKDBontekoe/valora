namespace Valora.Application.DTOs.Map;

public record MapAmenityDto(
    string Id,
    string Type,
    string Name,
    double Latitude,
    double Longitude,
    Dictionary<string, string>? Metadata = null);
