import 'dart:async';
import 'dart:developer' as developer;
import 'package:flutter/material.dart';
import '../core/exceptions/app_exceptions.dart';
import '../models/map_city_insight.dart';
import '../models/map_amenity.dart';
import '../models/map_amenity_cluster.dart';
import '../models/map_overlay.dart';
import '../models/map_overlay_tile.dart';
import '../services/api_service.dart';

enum InsightMetric { composite, safety, social, amenities }

class InsightsProvider extends ChangeNotifier {
  static const double _prefetchPaddingFactor = 0.2;
  static const double _amenityDetailZoomThreshold = 13.0;
  static const double _overlayDetailZoomThreshold = 11.0;

  ApiService _apiService;

  List<MapCityInsight> _cities = [];
  List<MapAmenity> _amenities = [];
  List<MapAmenityCluster> _amenityClusters = [];
  List<MapOverlay> _overlays = [];
  List<MapOverlayTile> _overlayTiles = [];

  bool _isLoading = false;
  String? _error;
  String? _mapError;
  InsightMetric _selectedMetric = InsightMetric.composite;

  // Layer Toggles
  bool _showAmenities = false;
  bool _showOverlays = false;
  MapOverlayMetric _selectedOverlayMetric =
      MapOverlayMetric.pricePerSquareMeter;

  int _requestSequence = 0;

  _MapBounds? _amenitiesCoverage;
  int? _amenitiesCoverageZoomBucket;

  _MapBounds? _amenityClustersCoverage;
  int? _amenityClustersCoverageZoomBucket;

  _MapBounds? _overlaysCoverage;
  int? _overlaysCoverageZoomBucket;
  String? _overlaysCoverageMetric;

  _MapBounds? _overlayTilesCoverage;
  int? _overlayTilesCoverageZoomBucket;
  String? _overlayTilesCoverageMetric;

  final Map<String, List<MapAmenity>> _amenitiesCache = {};
  final Map<String, List<MapAmenityCluster>> _amenityClustersCache = {};
  final Map<String, List<MapOverlay>> _overlaysCache = {};
  final Map<String, List<MapOverlayTile>> _overlayTilesCache = {};

  InsightsProvider(this._apiService);

  List<MapCityInsight> get cities => _cities;
  List<MapAmenity> get amenities => _amenities;
  List<MapAmenityCluster> get amenityClusters => _amenityClusters;
  List<MapOverlay> get overlays => _overlays;
  List<MapOverlayTile> get overlayTiles => _overlayTiles;

  bool get isLoading => _isLoading;
  String? get error => _error;
  String? get mapError => _mapError;
  InsightMetric get selectedMetric => _selectedMetric;

  bool get showAmenities => _showAmenities;
  bool get showOverlays => _showOverlays;
  MapOverlayMetric get selectedOverlayMetric => _selectedOverlayMetric;

  void update(ApiService apiService) {
    _apiService = apiService;
  }

  Future<void> loadInsights() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _cities = await _apiService.getCityInsights();
    } catch (e) {
      _error = e is AppException ? e.message : 'Failed to load insights';
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
    double zoom = 7.5,
  }) async {
    if (!_showAmenities && !_showOverlays) return;

    final int requestId = ++_requestSequence;
    _mapError = null;
    bool hasAnyChange = false;
    final int zoomBucket = zoom.floor();
    final _MapBounds viewport = _MapBounds(
      minLat: minLat,
      minLon: minLon,
      maxLat: maxLat,
      maxLon: maxLon,
    );

    final List<Future<_LayerFetchResult>> fetches = [];

    // Amenities Logic
    if (_showAmenities) {
      if (zoom >= _amenityDetailZoomThreshold) {
        // Detailed Amenities
        if (_amenityClusters.isNotEmpty) {
          _amenityClusters = [];
          hasAnyChange = true;
        }

        final bool isCovered =
            _amenitiesCoverage != null &&
            _amenitiesCoverageZoomBucket == zoomBucket &&
            _amenitiesCoverage!.contains(viewport);

        if (!isCovered) {
          fetches.add(_fetchAmenitiesForViewport(viewport, zoomBucket));
        }
      } else {
        // Clustered Amenities
        if (_amenities.isNotEmpty) {
          _amenities = [];
          hasAnyChange = true;
        }

        final bool isCovered =
            _amenityClustersCoverage != null &&
            _amenityClustersCoverageZoomBucket == zoomBucket &&
            _amenityClustersCoverage!.contains(viewport);

        if (!isCovered) {
          fetches.add(
            _fetchAmenityClustersForViewport(viewport, zoomBucket, zoom),
          );
        }
      }
    } else {
      if (_amenities.isNotEmpty || _amenityClusters.isNotEmpty) {
        _amenities = [];
        _amenityClusters = [];
        _amenitiesCoverage = null;
        _amenityClustersCoverage = null;
        hasAnyChange = true;
      }
    }

    // Overlays Logic
    final String overlayMetric = _getOverlayMetricString(
      _selectedOverlayMetric,
    );

    if (_showOverlays) {
      if (zoom >= _overlayDetailZoomThreshold) {
        // Detailed Overlays
        if (_overlayTiles.isNotEmpty) {
          _overlayTiles = [];
          hasAnyChange = true;
        }

        final bool isCovered =
            _overlaysCoverage != null &&
            _overlaysCoverageZoomBucket == zoomBucket &&
            _overlaysCoverageMetric == overlayMetric &&
            _overlaysCoverage!.contains(viewport);

        if (!isCovered) {
          fetches.add(
            _fetchOverlaysForViewport(viewport, zoomBucket, overlayMetric),
          );
        }
      } else {
        // Tiled Overlays
        if (_overlays.isNotEmpty) {
          _overlays = [];
          hasAnyChange = true;
        }

        final bool isCovered =
            _overlayTilesCoverage != null &&
            _overlayTilesCoverageZoomBucket == zoomBucket &&
            _overlayTilesCoverageMetric == overlayMetric &&
            _overlayTilesCoverage!.contains(viewport);

        if (!isCovered) {
          fetches.add(
            _fetchOverlayTilesForViewport(
              viewport,
              zoomBucket,
              zoom,
              overlayMetric,
            ),
          );
        }
      }
    } else {
      if (_overlays.isNotEmpty || _overlayTiles.isNotEmpty) {
        _overlays = [];
        _overlayTiles = [];
        _overlaysCoverage = null;
        _overlayTilesCoverage = null;
        _overlaysCoverageMetric = null;
        _overlayTilesCoverageMetric = null;
        hasAnyChange = true;
      }
    }

    if (fetches.isNotEmpty) {
      final results = await Future.wait(fetches);

      if (requestId != _requestSequence) {
        return;
      }

      for (final result in results) {
        if (result.error != null) {
          developer.log(
            'Failed to fetch ${result.layer}',
            error: result.error,
            name: 'InsightsProvider',
          );
          _mapError = 'Some map features could not be loaded.';
          continue;
        }

        switch (result.layer) {
          case _MapLayer.amenities:
            if (result.amenities != null) {
              _amenities = result.amenities!;
              _amenitiesCoverage = result.bounds;
              _amenitiesCoverageZoomBucket = result.zoomBucket;
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
              _overlaysCoverageZoomBucket = result.zoomBucket;
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
      final result = await _apiService.getMapAmenities(
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
      final result = await _apiService.getMapAmenityClusters(
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
      final result = await _apiService.getMapOverlays(
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
    final String cacheKey =
        'overlay_tiles:$metric:${bounds.cacheKey(zoomBucket)}';
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
      final result = await _apiService.getMapOverlayTiles(
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
