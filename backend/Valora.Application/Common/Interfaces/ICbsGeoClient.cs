using Valora.Application.DTOs.Map;

namespace Valora.Application.Common.Interfaces;

public interface ICbsGeoClient
{
    Task<List<MapOverlayDto>> GetNeighborhoodOverlaysAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        MapOverlayMetric metric,
        CancellationToken cancellationToken = default);
}
