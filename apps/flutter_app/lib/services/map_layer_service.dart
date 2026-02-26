import '../core/utils/map_bounds.dart';
import '../models/map_amenity.dart';
import '../models/map_amenity_cluster.dart';
import '../models/map_overlay.dart';
import '../models/map_overlay_tile.dart';
import '../repositories/map_repository.dart';

class MapLayerService {
  final MapRepository _repository;

  // Caching
  final Map<String, List<MapAmenity>> _amenitiesCache = {};
  final Map<String, List<MapAmenityCluster>> _amenityClustersCache = {};
  final Map<String, List<MapOverlay>> _overlaysCache = {};
  final Map<String, List<MapOverlayTile>> _overlayTilesCache = {};

  MapLayerService(this._repository);

  Future<List<MapAmenity>> getAmenities(MapBounds bounds, int zoomBucket) async {
    final String cacheKey = 'amenities:${bounds.cacheKey(zoomBucket)}';
    if (_amenitiesCache.containsKey(cacheKey)) {
      return _amenitiesCache[cacheKey]!;
    }

    final result = await _repository.getMapAmenities(
      minLat: bounds.minLat,
      minLon: bounds.minLon,
      maxLat: bounds.maxLat,
      maxLon: bounds.maxLon,
    );
    _amenitiesCache[cacheKey] = result;
    return result;
  }

  Future<List<MapAmenityCluster>> getAmenityClusters(
    MapBounds bounds,
    int zoomBucket,
    double zoom,
  ) async {
    // Include zoom in cache key to account for different clustering at different zoom levels
    final String cacheKey = 'amenity_clusters:${bounds.cacheKey(zoomBucket)}:${zoom.toStringAsFixed(1)}';
    if (_amenityClustersCache.containsKey(cacheKey)) {
      return _amenityClustersCache[cacheKey]!;
    }

    final result = await _repository.getMapAmenityClusters(
      minLat: bounds.minLat,
      minLon: bounds.minLon,
      maxLat: bounds.maxLat,
      maxLon: bounds.maxLon,
      zoom: zoom,
    );
    _amenityClustersCache[cacheKey] = result;
    return result;
  }

  Future<List<MapOverlay>> getOverlays(
    MapBounds bounds,
    int zoomBucket,
    String metric,
  ) async {
    final String cacheKey = 'overlays:$metric:${bounds.cacheKey(zoomBucket)}';
    if (_overlaysCache.containsKey(cacheKey)) {
      return _overlaysCache[cacheKey]!;
    }

    final result = await _repository.getMapOverlays(
      minLat: bounds.minLat,
      minLon: bounds.minLon,
      maxLat: bounds.maxLat,
      maxLon: bounds.maxLon,
      metric: metric,
    );
    _overlaysCache[cacheKey] = result;
    return result;
  }

  Future<List<MapOverlayTile>> getOverlayTiles(
    MapBounds bounds,
    int zoomBucket,
    double zoom,
    String metric,
  ) async {
    // Include zoom in cache key to account for different tile sets at different zoom levels
    final String cacheKey = 'overlay_tiles:$metric:${bounds.cacheKey(zoomBucket)}:${zoom.toStringAsFixed(1)}';
    if (_overlayTilesCache.containsKey(cacheKey)) {
      return _overlayTilesCache[cacheKey]!;
    }

    final result = await _repository.getMapOverlayTiles(
      minLat: bounds.minLat,
      minLon: bounds.minLon,
      maxLat: bounds.maxLat,
      maxLon: bounds.maxLon,
      zoom: zoom,
      metric: metric,
    );
    _overlayTilesCache[cacheKey] = result;
    return result;
  }
}
