import 'package:flutter/material.dart';
import '../core/exceptions/app_exceptions.dart';
import '../models/map_city_insight.dart';
import '../models/map_amenity.dart';
import '../models/map_overlay.dart';
import '../services/api_service.dart';

enum InsightMetric {
  composite,
  safety,
  social,
  amenities,
}

class InsightsProvider extends ChangeNotifier {
  ApiService _apiService;

  List<MapCityInsight> _cities = [];
  List<MapAmenity> _amenities = [];
  List<MapOverlay> _overlays = [];

  bool _isLoading = false;
  String? _error;
  InsightMetric _selectedMetric = InsightMetric.composite;

  // Layer Toggles
  bool _showAmenities = false;
  bool _showOverlays = false;
  MapOverlayMetric _selectedOverlayMetric = MapOverlayMetric.pricePerSquareMeter;

  InsightsProvider(this._apiService);

  List<MapCityInsight> get cities => _cities;
  List<MapAmenity> get amenities => _amenities;
  List<MapOverlay> get overlays => _overlays;

  bool get isLoading => _isLoading;
  String? get error => _error;
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

    try {
      if (_showAmenities && zoom >= 13) {
        _amenities = await _apiService.getMapAmenities(
          minLat: minLat,
          minLon: minLon,
          maxLat: maxLat,
          maxLon: maxLon,
        );
      } else {
        _amenities = [];
      }

      if (_showOverlays && zoom >= 11) {
        _overlays = await _apiService.getMapOverlays(
          minLat: minLat,
          minLon: minLon,
          maxLat: maxLat,
          maxLon: maxLon,
          metric: _getOverlayMetricString(_selectedOverlayMetric),
        );
      } else {
        _overlays = [];
      }

      notifyListeners();
    } catch (e) {
      developer.log('Failed to fetch map data: $e');
    }
  }

  void toggleAmenities() {
    _showAmenities = !_showAmenities;
    if (!_showAmenities) _amenities = [];
    notifyListeners();
  }

  void toggleOverlays() {
    _showOverlays = !_showOverlays;
    if (!_showOverlays) _overlays = [];
    notifyListeners();
  }

  void setOverlayMetric(MapOverlayMetric metric) {
    _selectedOverlayMetric = metric;
    notifyListeners();
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
}

// Added this import for logging
import 'dart:developer' as developer;
