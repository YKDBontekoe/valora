using Valora.Application.DTOs.Map;

namespace Valora.Application.Common.Interfaces;

public interface IMapService
{
    Task<List<MapCityInsightDto>> GetCityInsightsAsync(CancellationToken cancellationToken = default);

    Task<List<MapAmenityDto>> GetMapAmenitiesAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        List<string>? types = null,
        CancellationToken cancellationToken = default);

    Task<List<MapAmenityClusterDto>> GetMapAmenityClustersAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        double zoom,
        List<string>? types = null,
        CancellationToken cancellationToken = default);

    Task<List<MapOverlayDto>> GetMapOverlaysAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        MapOverlayMetric metric,
        CancellationToken cancellationToken = default);

    Task<List<MapOverlayTileDto>> GetMapOverlayTilesAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        double zoom,
        MapOverlayMetric metric,
        CancellationToken cancellationToken = default);

    Task<MapQueryResultDto> ProcessQueryAsync(MapQueryRequest request, CancellationToken cancellationToken = default);
}
