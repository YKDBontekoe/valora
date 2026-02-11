namespace Valora.Application.DTOs.Map;

public record MapCityInsightDto(
    string City,
    int Count,
    double Latitude,
    double Longitude,
    double? CompositeScore,
    double? SafetyScore,
    double? SocialScore,
    double? AmenitiesScore);
