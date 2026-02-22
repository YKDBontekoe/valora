using Valora.Application.DTOs.Map;

namespace Valora.Application.Common.Interfaces;

public record NeighborhoodGeometryDto(string Code, string Name, string Type, double Latitude, double Longitude);

public interface ICbsGeoClient
{
    Task<List<MapOverlayDto>> GetNeighborhoodOverlaysAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        MapOverlayMetric metric,
        CancellationToken cancellationToken = default);

    Task<List<NeighborhoodGeometryDto>> GetNeighborhoodsByMunicipalityAsync(
        string municipalityName,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetAllMunicipalitiesAsync(CancellationToken cancellationToken = default);
}
