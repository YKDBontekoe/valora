import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../models/map_amenity.dart';
import '../models/map_amenity_cluster.dart';
import '../models/map_city_insight.dart';
import '../models/map_overlay.dart';
import '../models/map_overlay_tile.dart';
import '../repositories/map_repository.dart';

enum InsightMetric { composite, safety, social, amenities }

class InsightsProvider extends ChangeNotifier {
  MapRepository _repository;

  // State
  List<MapCityInsight> _cityInsights = [];
  List<MapAmenity> _amenities = [];
  List<MapAmenityCluster> _amenityClusters = [];
  List<MapOverlay> _overlays = [];
  List<MapOverlayTile> _overlayTiles = [];

  bool _isLoading = false;
  bool _showAmenities = false;
  bool _showOverlays = false;
  String? _error;
  String? _mapError;

  InsightMetric _selectedMetric = InsightMetric.composite;
  MapOverlayMetric _selectedOverlayMetric = MapOverlayMetric.pricePerSquareMeter;

  // Caching
  final Map<String, List<MapAmenity>> _amenitiesCache = {};
  final Map<String, List<MapAmenityCluster>> _amenityClustersCache = {};
  final Map<String, List<MapOverlay>> _overlaysCache = {};
  final Map<String, List<MapOverlayTile>> _overlayTilesCache = {};

  // Coverage tracking
  _MapBounds? _amenitiesCoverage;
  _MapBounds? _amenityClustersCoverage;
  _MapBounds? _overlaysCoverage;
  _MapBounds? _overlayTilesCoverage;

  int? _amenityClustersCoverageZoomBucket;
  int? _overlayTilesCoverageZoomBucket;

  String? _overlaysCoverageMetric;
  String? _overlayTilesCoverageMetric;

  static const double _prefetchPaddingFactor = 0.5;

  InsightsProvider(this._repository);

  void update(MapRepository repository) {
    _repository = repository;
  }

  // Getters
  List<MapCityInsight> get cityInsights => _cityInsights;
  List<MapCityInsight> get cities => _cityInsights;
  List<MapAmenity> get amenities => _amenities;
  List<MapAmenityCluster> get amenityClusters => _amenityClusters;
  List<MapOverlay> get overlays => _overlays;
  List<MapOverlayTile> get overlayTiles => _overlayTiles;
  bool get isLoading => _isLoading;
  bool get showAmenities => _showAmenities;
  bool get showOverlays => _showOverlays;
  String? get error => _error;
  String? get mapError => _mapError;
  InsightMetric get selectedMetric => _selectedMetric;
  MapOverlayMetric get selectedOverlayMetric => _selectedOverlayMetric;

  Future<void> loadInsights() => loadCityInsights();

  Future<void> loadCityInsights() async {
    if (_cityInsights.isNotEmpty) return;

    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _cityInsights = await _repository.getCityInsights();
    } catch (e) {
      debugPrint('Error loading city insights: $e');
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> fetchMapData({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    required double zoom,
  }) async {
    final bounds = LatLngBounds(
        LatLng(minLat, minLon),
        LatLng(maxLat, maxLon),
    );
    final center = LatLng((minLat + maxLat) / 2, (minLon + maxLon) / 2);
    return updateMapViewport(center: center, zoom: zoom, bounds: bounds);
  }

  Future<void> updateMapViewport({
    required LatLng center,
    required double zoom,
    required LatLngBounds bounds,
  }) async {
    _mapError = null;
    final _MapBounds viewport = _MapBounds(
      minLat: bounds.south,
      minLon: bounds.west,
      maxLat: bounds.north,
      maxLon: bounds.east,
    );

    // Identify zoom bucket (0-20)
    final int zoomBucket = zoom.floor();

    final List<Future<_LayerFetchResult>> tasks = [];

    // Check if we need amenities
    if (_showAmenities) {
      if (zoom >= 14) {
        // Fetch individual amenities
        _amenityClusters = []; // Clear clusters
        if (_amenitiesCoverage == null || !_amenitiesCoverage!.contains(viewport)) {
          tasks.add(_fetchAmenitiesForViewport(viewport, zoomBucket));
        }
      } else {
        // Fetch clusters
        _amenities = []; // Clear individual amenities
        if (_amenityClustersCoverage == null ||
            !_amenityClustersCoverage!.contains(viewport) ||
            _amenityClustersCoverageZoomBucket != zoomBucket) {
          tasks.add(_fetchAmenityClustersForViewport(viewport, zoomBucket, zoom));
        }
      }
    }

    // Check if we need overlays
    if (_showOverlays) {
      final metric = _getOverlayMetricString(_selectedOverlayMetric);

      if (zoom >= 13) {
        // Fetch detailed overlay polygons
        _overlayTiles = [];
        if (_overlaysCoverage == null ||
            !_overlaysCoverage!.contains(viewport) ||
            _overlaysCoverageMetric != metric) {
          tasks.add(_fetchOverlaysForViewport(viewport, zoomBucket, metric));
        }
      } else {
        // Fetch overlay tiles (heatmap/grid)
        _overlays = [];
        if (_overlayTilesCoverage == null ||
            !_overlayTilesCoverage!.contains(viewport) ||
            _overlayTilesCoverageZoomBucket != zoomBucket ||
            _overlayTilesCoverageMetric != metric) {
          tasks.add(_fetchOverlayTilesForViewport(viewport, zoomBucket, zoom, metric));
        }
      }
    }

    if (tasks.isEmpty) return;

    final results = await Future.wait(tasks);
    bool hasAnyChange = false;

    for (final result in results) {
      if (result.error != null) {
        debugPrint('Error fetching ${result.layer}: ${result.error}');
        _mapError = result.error.toString();
        notifyListeners(); // Notify error
        continue;
      }

      if (result.bounds != null) {
        switch (result.layer) {
          case _MapLayer.amenities:
            if (result.amenities != null) {
              _amenities = result.amenities!;
              _amenitiesCoverage = result.bounds;
              hasAnyChange = true;
            }
            break;
          case _MapLayer.amenityClusters:
            if (result.amenityClusters != null) {
              _amenityClusters = result.amenityClusters!;
              _amenityClustersCoverage = result.bounds;
              _amenityClustersCoverageZoomBucket = result.zoomBucket;
              hasAnyChange = true;
            }
            break;
          case _MapLayer.overlays:
            if (result.overlays != null) {
              _overlays = result.overlays!;
              _overlaysCoverage = result.bounds;
              _overlaysCoverageMetric = result.metric;
              hasAnyChange = true;
            }
            break;
          case _MapLayer.overlayTiles:
            if (result.overlayTiles != null) {
              _overlayTiles = result.overlayTiles!;
              _overlayTilesCoverage = result.bounds;
              _overlayTilesCoverageZoomBucket = result.zoomBucket;
              _overlayTilesCoverageMetric = result.metric;
              hasAnyChange = true;
            }
            break;
        }
      }
    }

    if (hasAnyChange) {
      notifyListeners();
    }
  }

  void toggleAmenities() {
    _showAmenities = !_showAmenities;
    if (!_showAmenities) {
      _amenities = [];
      _amenityClusters = [];
      _amenitiesCoverage = null;
      _amenityClustersCoverage = null;
    }
    notifyListeners();
  }

  void toggleOverlays() {
    _showOverlays = !_showOverlays;
    if (!_showOverlays) {
      _overlays = [];
      _overlayTiles = [];
      _overlaysCoverage = null;
      _overlayTilesCoverage = null;
    }
    notifyListeners();
  }

  void setOverlayMetric(MapOverlayMetric metric) {
    if (_selectedOverlayMetric != metric) {
      _selectedOverlayMetric = metric;
      _overlaysCoverage = null;
      _overlayTilesCoverage = null;
      notifyListeners();
    }
  }

  void setMetric(InsightMetric metric) {
    if (_selectedMetric != metric) {
      _selectedMetric = metric;
      notifyListeners();
    }
  }

  double? getScore(MapCityInsight city) {
    switch (_selectedMetric) {
      case InsightMetric.composite:
        return city.compositeScore;
      case InsightMetric.safety:
        return city.safetyScore;
      case InsightMetric.social:
        return city.socialScore;
      case InsightMetric.amenities:
        return city.amenitiesScore;
    }
  }

  String _getOverlayMetricString(MapOverlayMetric metric) {
    switch (metric) {
      case MapOverlayMetric.pricePerSquareMeter:
        return 'PricePerSquareMeter';
      case MapOverlayMetric.crimeRate:
        return 'CrimeRate';
      case MapOverlayMetric.populationDensity:
        return 'PopulationDensity';
      case MapOverlayMetric.averageWoz:
        return 'AverageWoz';
    }
  }

  Future<_LayerFetchResult> _fetchAmenitiesForViewport(
    _MapBounds viewport,
    int zoomBucket,
  ) async {
    final _MapBounds bounds = viewport.expand(_prefetchPaddingFactor);
    final String cacheKey = 'amenities:${bounds.cacheKey(zoomBucket)}';
    final cached = _amenitiesCache[cacheKey];
    if (cached != null) {
      return _LayerFetchResult.amenities(
        amenities: cached,
        bounds: bounds,
        zoomBucket: zoomBucket,
      );
    }

    try {
      final result = await _repository.getMapAmenities(
        minLat: bounds.minLat,
        minLon: bounds.minLon,
        maxLat: bounds.maxLat,
        maxLon: bounds.maxLon,
      );
      _amenitiesCache[cacheKey] = result;
      return _LayerFetchResult.amenities(
        amenities: result,
        bounds: bounds,
        zoomBucket: zoomBucket,
      );
    } catch (e) {
      return _LayerFetchResult.error(_MapLayer.amenities, e);
    }
  }

  Future<_LayerFetchResult> _fetchAmenityClustersForViewport(
    _MapBounds viewport,
    int zoomBucket,
    double zoom,
  ) async {
    final _MapBounds bounds = viewport.expand(_prefetchPaddingFactor);
    final String cacheKey = 'amenity_clusters:${bounds.cacheKey(zoomBucket)}';
    final cached = _amenityClustersCache[cacheKey];
    if (cached != null) {
      return _LayerFetchResult.amenityClusters(
        amenityClusters: cached,
        bounds: bounds,
        zoomBucket: zoomBucket,
      );
    }

    try {
      final result = await _repository.getMapAmenityClusters(
        minLat: bounds.minLat,
        minLon: bounds.minLon,
        maxLat: bounds.maxLat,
        maxLon: bounds.maxLon,
        zoom: zoom,
      );
      _amenityClustersCache[cacheKey] = result;
      return _LayerFetchResult.amenityClusters(
        amenityClusters: result,
        bounds: bounds,
        zoomBucket: zoomBucket,
      );
    } catch (e) {
      return _LayerFetchResult.error(_MapLayer.amenityClusters, e);
    }
  }

  Future<_LayerFetchResult> _fetchOverlaysForViewport(
    _MapBounds viewport,
    int zoomBucket,
    String metric,
  ) async {
    final _MapBounds bounds = viewport.expand(_prefetchPaddingFactor);
    final String cacheKey = 'overlays:$metric:${bounds.cacheKey(zoomBucket)}';
    final cached = _overlaysCache[cacheKey];
    if (cached != null) {
      return _LayerFetchResult.overlays(
        overlays: cached,
        bounds: bounds,
        zoomBucket: zoomBucket,
        metric: metric,
      );
    }

    try {
      final result = await _repository.getMapOverlays(
        minLat: bounds.minLat,
        minLon: bounds.minLon,
        maxLat: bounds.maxLat,
        maxLon: bounds.maxLon,
        metric: metric,
      );
      _overlaysCache[cacheKey] = result;
      return _LayerFetchResult.overlays(
        overlays: result,
        bounds: bounds,
        zoomBucket: zoomBucket,
        metric: metric,
      );
    } catch (e) {
      return _LayerFetchResult.error(_MapLayer.overlays, e);
    }
  }

  Future<_LayerFetchResult> _fetchOverlayTilesForViewport(
    _MapBounds viewport,
    int zoomBucket,
    double zoom,
    String metric,
  ) async {
    final _MapBounds bounds = viewport.expand(_prefetchPaddingFactor);
    final String cacheKey = 'overlay_tiles:$metric:${bounds.cacheKey(zoomBucket)}';
    final cached = _overlayTilesCache[cacheKey];
    if (cached != null) {
      return _LayerFetchResult.overlayTiles(
        overlayTiles: cached,
        bounds: bounds,
        zoomBucket: zoomBucket,
        metric: metric,
      );
    }

    try {
      final result = await _repository.getMapOverlayTiles(
        minLat: bounds.minLat,
        minLon: bounds.minLon,
        maxLat: bounds.maxLat,
        maxLon: bounds.maxLon,
        zoom: zoom,
        metric: metric,
      );
      _overlayTilesCache[cacheKey] = result;
      return _LayerFetchResult.overlayTiles(
        overlayTiles: result,
        bounds: bounds,
        zoomBucket: zoomBucket,
        metric: metric,
      );
    } catch (e) {
      return _LayerFetchResult.error(_MapLayer.overlayTiles, e);
    }
  }
}

class _MapBounds {
  const _MapBounds({
    required this.minLat,
    required this.minLon,
    required this.maxLat,
    required this.maxLon,
  });

  final double minLat;
  final double minLon;
  final double maxLat;
  final double maxLon;

  _MapBounds expand(double factor) {
    final latSpan = maxLat - minLat;
    final lonSpan = maxLon - minLon;
    final latPadding = latSpan * factor;
    final lonPadding = lonSpan * factor;
    return _MapBounds(
      minLat: minLat - latPadding,
      minLon: minLon - lonPadding,
      maxLat: maxLat + latPadding,
      maxLon: maxLon + lonPadding,
    );
  }

  bool contains(_MapBounds other) {
    return other.minLat >= minLat &&
        other.minLon >= minLon &&
        other.maxLat <= maxLat &&
        other.maxLon <= maxLon;
  }

  String cacheKey(int zoomBucket) {
    return '$zoomBucket:${_round(minLat)}:${_round(minLon)}:${_round(maxLat)}:${_round(maxLon)}';
  }

  String _round(double value) => value.toStringAsFixed(3);
}

enum _MapLayer { amenities, overlays, amenityClusters, overlayTiles }

class _LayerFetchResult {
  const _LayerFetchResult._({
    required this.layer,
    this.amenities,
    this.amenityClusters,
    this.overlays,
    this.overlayTiles,
    this.bounds,
    this.zoomBucket,
    this.metric,
    this.error,
  });

  final _MapLayer layer;
  final List<MapAmenity>? amenities;
  final List<MapAmenityCluster>? amenityClusters;
  final List<MapOverlay>? overlays;
  final List<MapOverlayTile>? overlayTiles;
  final _MapBounds? bounds;
  final int? zoomBucket;
  final String? metric;
  final Object? error;

  factory _LayerFetchResult.amenities({
    required List<MapAmenity> amenities,
    required _MapBounds bounds,
    required int zoomBucket,
  }) {
    return _LayerFetchResult._(
      layer: _MapLayer.amenities,
      amenities: amenities,
      bounds: bounds,
      zoomBucket: zoomBucket,
    );
  }

  factory _LayerFetchResult.amenityClusters({
    required List<MapAmenityCluster> amenityClusters,
    required _MapBounds bounds,
    required int zoomBucket,
  }) {
    return _LayerFetchResult._(
      layer: _MapLayer.amenityClusters,
      amenityClusters: amenityClusters,
      bounds: bounds,
      zoomBucket: zoomBucket,
    );
  }

  factory _LayerFetchResult.overlays({
    required List<MapOverlay> overlays,
    required _MapBounds bounds,
    required int zoomBucket,
    required String metric,
  }) {
    return _LayerFetchResult._(
      layer: _MapLayer.overlays,
      overlays: overlays,
      bounds: bounds,
      zoomBucket: zoomBucket,
      metric: metric,
    );
  }

  factory _LayerFetchResult.overlayTiles({
    required List<MapOverlayTile> overlayTiles,
    required _MapBounds bounds,
    required int zoomBucket,
    required String metric,
  }) {
    return _LayerFetchResult._(
      layer: _MapLayer.overlayTiles,
      overlayTiles: overlayTiles,
      bounds: bounds,
      zoomBucket: zoomBucket,
      metric: metric,
    );
  }

  factory _LayerFetchResult.error(_MapLayer layer, Object error) {
    return _LayerFetchResult._(layer: layer, error: error);
  }
}
