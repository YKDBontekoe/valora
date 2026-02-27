import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../core/utils/map_bounds.dart';
import '../models/map_amenity.dart';
import '../models/map_amenity_cluster.dart';
import '../models/map_city_insight.dart';
import '../models/map_overlay.dart';
import '../models/map_overlay_tile.dart';
import '../repositories/map_repository.dart';
import '../services/map_layer_service.dart';

enum InsightMetric { composite, safety, social, amenities }

enum MapMode { cities, overlays, amenities }

class InsightsProvider extends ChangeNotifier {
  MapRepository _repository;
  MapLayerService _mapLayerService;

  // State
  List<MapCityInsight> _cityInsights = [];
  List<MapAmenity> _amenities = [];
  List<MapAmenityCluster> _amenityClusters = [];
  List<MapOverlay> _overlays = [];
  List<MapOverlayTile> _overlayTiles = [];

  bool _isLoading = false;
  MapMode _mapMode = MapMode.cities;
  Object? _selectedFeature;

  Object? _exception;
  Object? _mapException;

  InsightMetric _selectedMetric = InsightMetric.composite;
  MapOverlayMetric _selectedOverlayMetric = MapOverlayMetric.pricePerSquareMeter;

  // Coverage tracking
  MapBounds? _amenitiesCoverage;
  MapBounds? _amenityClustersCoverage;
  MapBounds? _overlaysCoverage;
  MapBounds? _overlayTilesCoverage;

  int? _amenityClustersCoverageZoomBucket;
  int? _overlayTilesCoverageZoomBucket;

  String? _overlaysCoverageMetric;
  String? _overlayTilesCoverageMetric;

  static const double _prefetchPaddingFactor = 0.5;

  InsightsProvider(this._repository) : _mapLayerService = MapLayerService(_repository);

  void update(MapRepository repository) {
    _repository = repository;
    _mapLayerService = MapLayerService(repository);
  }

  // Getters
  List<MapCityInsight> get cityInsights => _cityInsights;
  List<MapCityInsight> get cities => _cityInsights;
  List<MapAmenity> get amenities => _amenities;
  List<MapAmenityCluster> get amenityClusters => _amenityClusters;
  List<MapOverlay> get overlays => _overlays;
  List<MapOverlayTile> get overlayTiles => _overlayTiles;
  bool get isLoading => _isLoading;

  MapMode get mapMode => _mapMode;
  Object? get selectedFeature => _selectedFeature;

  bool get showAmenities => _mapMode == MapMode.amenities;
  bool get showOverlays => _mapMode == MapMode.overlays;

  String? get error => _exception?.toString();
  String? get mapError => _mapException?.toString();
  Object? get exception => _exception;
  Object? get mapException => _mapException;
  InsightMetric get selectedMetric => _selectedMetric;
  MapOverlayMetric get selectedOverlayMetric => _selectedOverlayMetric;

  Future<void> loadInsights() => loadCityInsights();

  Future<void> loadCityInsights() async {
    if (_cityInsights.isNotEmpty) return;

    _isLoading = true;
    _exception = null;
    notifyListeners();

    try {
      _cityInsights = await _repository.getCityInsights();
    } catch (e) {
      debugPrint('Error loading city insights: $e');
      _exception = e;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  void setMapMode(MapMode mode) {
    if (_mapMode == mode) return;

    _mapMode = mode;
    _selectedFeature = null;

    // Clear data if leaving a mode
    if (_mapMode != MapMode.amenities) {
      _amenities = [];
      _amenityClusters = [];
      _amenitiesCoverage = null;
      _amenityClustersCoverage = null;
    }
    if (_mapMode != MapMode.overlays) {
      _overlays = [];
      _overlayTiles = [];
      _overlaysCoverage = null;
      _overlayTilesCoverage = null;
    }

    notifyListeners();
  }

  void selectFeature(Object? feature) {
    if (_selectedFeature != feature) {
      _selectedFeature = feature;
      notifyListeners();
    }
  }

  void clearSelection() {
    if (_selectedFeature != null) {
      _selectedFeature = null;
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
    _mapException = null;
    final MapBounds viewport = MapBounds(
      minLat: bounds.south,
      minLon: bounds.west,
      maxLat: bounds.north,
      maxLon: bounds.east,
    );

    // Identify zoom bucket (0-20)
    final int zoomBucket = zoom.floor();

    final List<Future<_LayerFetchResult>> tasks = [];

    // Check if we need amenities
    if (showAmenities) {
      if (zoom >= 14) {
        // Fetch individual amenities
        _amenityClusters = []; // Clear clusters
        if (_amenitiesCoverage == null || !_amenitiesCoverage!.contains(viewport)) {
          tasks.add(_fetchAmenities(viewport, zoomBucket));
        }
      } else {
        // Fetch clusters
        _amenities = []; // Clear individual amenities
        if (_amenityClustersCoverage == null ||
            !_amenityClustersCoverage!.contains(viewport) ||
            _amenityClustersCoverageZoomBucket != zoomBucket) {
          tasks.add(_fetchAmenityClusters(viewport, zoomBucket, zoom));
        }
      }
    }

    // Check if we need overlays
    if (showOverlays) {
      final metric = _getOverlayMetricString(_selectedOverlayMetric);

      if (zoom >= 13) {
        // Fetch detailed overlay polygons
        _overlayTiles = [];
        if (_overlaysCoverage == null ||
            !_overlaysCoverage!.contains(viewport) ||
            _overlaysCoverageMetric != metric) {
          tasks.add(_fetchOverlays(viewport, zoomBucket, metric));
        }
      } else {
        // Fetch overlay tiles (heatmap/grid)
        _overlays = [];
        if (_overlayTilesCoverage == null ||
            !_overlayTilesCoverage!.contains(viewport) ||
            _overlayTilesCoverageZoomBucket != zoomBucket ||
            _overlayTilesCoverageMetric != metric) {
          tasks.add(_fetchOverlayTiles(viewport, zoomBucket, zoom, metric));
        }
      }
    }

    if (tasks.isEmpty) return;

    final results = await Future.wait(tasks);
    bool hasAnyChange = false;

    for (final result in results) {
      if (result.error != null) {
        debugPrint('Error fetching ${result.layer}: ${result.error}');
        _mapException = result.error;
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
    if (_mapMode == MapMode.amenities) {
      setMapMode(MapMode.cities);
    } else {
      setMapMode(MapMode.amenities);
    }
  }

  void toggleOverlays() {
    if (_mapMode == MapMode.overlays) {
      setMapMode(MapMode.cities);
    } else {
      setMapMode(MapMode.overlays);
    }
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
        return 'Crime Rate';
      case MapOverlayMetric.populationDensity:
        return 'Pop. Density';
      case MapOverlayMetric.averageWoz:
        return 'Avg WOZ';
    }
  }

  Future<_LayerFetchResult> _fetchAmenities(
    MapBounds viewport,
    int zoomBucket,
  ) async {
    final MapBounds bounds = viewport.expand(_prefetchPaddingFactor);
    try {
      final result = await _mapLayerService.getAmenities(bounds, zoomBucket);
      return _LayerFetchResult.amenities(
        amenities: result,
        bounds: bounds,
        zoomBucket: zoomBucket,
      );
    } catch (e) {
      return _LayerFetchResult.error(_MapLayer.amenities, e);
    }
  }

  Future<_LayerFetchResult> _fetchAmenityClusters(
    MapBounds viewport,
    int zoomBucket,
    double zoom,
  ) async {
    final MapBounds bounds = viewport.expand(_prefetchPaddingFactor);
    try {
      final result = await _mapLayerService.getAmenityClusters(bounds, zoomBucket, zoom);
      return _LayerFetchResult.amenityClusters(
        amenityClusters: result,
        bounds: bounds,
        zoomBucket: zoomBucket,
      );
    } catch (e) {
      return _LayerFetchResult.error(_MapLayer.amenityClusters, e);
    }
  }

  Future<_LayerFetchResult> _fetchOverlays(
    MapBounds viewport,
    int zoomBucket,
    String metric,
  ) async {
    final MapBounds bounds = viewport.expand(_prefetchPaddingFactor);
    try {
      final result = await _mapLayerService.getOverlays(bounds, zoomBucket, metric);
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

  Future<_LayerFetchResult> _fetchOverlayTiles(
    MapBounds viewport,
    int zoomBucket,
    double zoom,
    String metric,
  ) async {
    final MapBounds bounds = viewport.expand(_prefetchPaddingFactor);
    try {
      final result = await _mapLayerService.getOverlayTiles(bounds, zoomBucket, zoom, metric);
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
  final MapBounds? bounds;
  final int? zoomBucket;
  final String? metric;
  final Object? error;

  factory _LayerFetchResult.amenities({
    required List<MapAmenity> amenities,
    required MapBounds bounds,
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
    required MapBounds bounds,
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
    required MapBounds bounds,
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
    required MapBounds bounds,
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
