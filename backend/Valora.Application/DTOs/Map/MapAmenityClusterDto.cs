using System.Diagnostics.CodeAnalysis;

namespace Valora.Application.DTOs.Map;

[ExcludeFromCodeCoverage]
public record MapAmenityClusterDto(
    double Latitude,
    double Longitude,
    int Count,
    Dictionary<string, int> TypeCounts
);
