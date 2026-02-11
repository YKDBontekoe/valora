import 'package:flutter/material.dart';
import '../models/map_city_insight.dart';
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
  bool _isLoading = false;
  String? _error;
  InsightMetric _selectedMetric = InsightMetric.composite;

  InsightsProvider(this._apiService);

  List<MapCityInsight> get cities => _cities;
  bool get isLoading => _isLoading;
  String? get error => _error;
  InsightMetric get selectedMetric => _selectedMetric;

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
      _error = e.toString();
    } finally {
      _isLoading = false;
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
}
