import 'package:flutter/foundation.dart';

import '../core/exceptions/app_exceptions.dart';
import '../models/context_report.dart';
import '../models/search_history_item.dart';
import '../services/api_service.dart';
import '../services/search_history_service.dart';

class ContextReportProvider extends ChangeNotifier {
  ContextReportProvider({
    required ApiService apiService,
    SearchHistoryService? historyService,
  }) : _apiService = apiService,
       _historyService = historyService ?? SearchHistoryService() {
    _loadHistory();
  }

  final ApiService _apiService;
  final SearchHistoryService _historyService;

  bool _isLoading = false;
  bool _isDisposed = false;
  String? _error;
  ContextLocation? _location;

  // Metrics
  List<ContextMetric>? _socialMetrics;
  List<ContextMetric>? _crimeMetrics;
  List<ContextMetric>? _demographicsMetrics;
  List<ContextMetric>? _housingMetrics;
  List<ContextMetric>? _mobilityMetrics;
  List<ContextMetric>? _amenityMetrics;
  List<ContextMetric>? _environmentMetrics;

  // Scores
  final Map<String, double> _categoryScores = {};

  // Loading states
  bool _loadingSocial = false;
  bool _loadingCrime = false;
  bool _loadingDemographics = false;
  bool _loadingHousing = false;
  bool _loadingMobility = false;
  bool _loadingAmenities = false;
  bool _loadingEnvironment = false;

  int _radiusMeters = 1000;
  List<SearchHistoryItem> _history = [];
  int _historyLoadSeq = 0;

  final Map<String, bool> _expansionStates = {};
  final Map<String, String> _aiInsights = {};
  final Map<String, String> _aiInsightErrors = {};
  final Set<String> _loadingAiInsights = {};

  bool get isLoading => _isLoading;
  String? get error => _error;
  ContextLocation? get location => _location;

  List<ContextMetric>? get socialMetrics => _socialMetrics;
  List<ContextMetric>? get crimeMetrics => _crimeMetrics;
  List<ContextMetric>? get demographicsMetrics => _demographicsMetrics;
  List<ContextMetric>? get housingMetrics => _housingMetrics;
  List<ContextMetric>? get mobilityMetrics => _mobilityMetrics;
  List<ContextMetric>? get amenityMetrics => _amenityMetrics;
  List<ContextMetric>? get environmentMetrics => _environmentMetrics;

  Map<String, double> get categoryScores => Map.unmodifiable(_categoryScores);

  bool get loadingSocial => _loadingSocial;
  bool get loadingCrime => _loadingCrime;
  bool get loadingDemographics => _loadingDemographics;
  bool get loadingHousing => _loadingHousing;
  bool get loadingMobility => _loadingMobility;
  bool get loadingAmenities => _loadingAmenities;
  bool get loadingEnvironment => _loadingEnvironment;

  int get radiusMeters => _radiusMeters;
  List<SearchHistoryItem> get history => List.unmodifiable(_history);

  ContextReport? get report {
    if (_location == null) return null;
    return ContextReport(
      location: _location!,
      socialMetrics: _socialMetrics ?? [],
      crimeMetrics: _crimeMetrics ?? [],
      demographicsMetrics: _demographicsMetrics ?? [],
      housingMetrics: _housingMetrics ?? [],
      mobilityMetrics: _mobilityMetrics ?? [],
      amenityMetrics: _amenityMetrics ?? [],
      environmentMetrics: _environmentMetrics ?? [],
      compositeScore: _calculateCompositeScore(),
      categoryScores: _categoryScores,
      sources: [],
      warnings: [],
    );
  }

  double _calculateCompositeScore() {
    if (_categoryScores.isEmpty) return 0;
    // Simple average for now as weights are on backend, but we could replicate them
    return _categoryScores.values.reduce((a, b) => a + b) / _categoryScores.length;
  }

  @override
  void dispose() {
    _isDisposed = true;
    super.dispose();
  }

  void setRadiusMeters(int value) {
    _radiusMeters = value.clamp(200, 5000);
    notifyListeners();
  }

  bool isExpanded(String category, {bool defaultValue = false}) {
    return _expansionStates[category] ?? defaultValue;
  }

  void setExpanded(String category, bool expanded) {
    if (_expansionStates[category] == expanded) return;
    _expansionStates[category] = expanded;
    notifyListeners();
  }

  String? getAiInsight(String location) => _aiInsights[location];
  String? getAiInsightError(String location) => _aiInsightErrors[location];
  bool isAiInsightLoading(String location) => _loadingAiInsights.contains(location);

  Future<void> generateAiInsight(ContextReport report) async {
    final location = report.location.displayAddress;
    if (_loadingAiInsights.contains(location)) return;

    _loadingAiInsights.add(location);
    _aiInsightErrors.remove(location);
    notifyListeners();

    try {
      final insight = await _apiService.getAiAnalysis(report);
      if (_isDisposed) return;
      _aiInsights[location] = insight;
    } catch (e) {
      if (_isDisposed) return;
      _aiInsightErrors[location] = e.toString();
    } finally {
      if (!_isDisposed) {
        _loadingAiInsights.remove(location);
        notifyListeners();
      }
    }
  }

  Future<void> _loadHistory() async {
    final int seq = ++_historyLoadSeq;
    final history = await _historyService.getHistory();
    if (_isDisposed || seq != _historyLoadSeq) return;
    _history = history;
    notifyListeners();
  }

  Future<void> generate(String input) async {
    if (_isLoading) return;

    final String trimmed = input.trim();
    if (trimmed.isEmpty) {
      _error = 'Enter an address or listing link.';
      notifyListeners();
      return;
    }

    _isLoading = true;
    _error = null;
    _location = null;
    _clearMetrics();
    notifyListeners();

    try {
      final location = await _apiService.resolveLocation(trimmed);
      if (_isDisposed) return;
      if (location == null) {
        _error = 'Could not resolve address.';
        _isLoading = false;
        notifyListeners();
        return;
      }

      _location = location;
      _isLoading = false;
      notifyListeners();

      _fetchMetrics(location);

      try {
        await _historyService.addToHistory(trimmed);
        await _loadHistory();
      } catch (_) {}
    } catch (e) {
      if (_isDisposed) return;
      _error = e is AppException ? e.message : 'Failed to resolve location.';
      _isLoading = false;
      notifyListeners();
    }
  }

  void _fetchMetrics(ContextLocation location) {
    _fetchCategory('Social', (loc) => _apiService.getContextMetrics('social', loc),
        (m) => _socialMetrics = m, (l) => _loadingSocial = l);
    _fetchCategory('Safety', (loc) => _apiService.getContextMetrics('safety', loc),
        (m) => _crimeMetrics = m, (l) => _loadingCrime = l);
    _fetchCategory('Demographics', (loc) => _apiService.getContextMetrics('demographics', loc),
        (m) => _demographicsMetrics = m, (l) => _loadingDemographics = l);
    _fetchCategory('Housing', (loc) => _apiService.getContextMetrics('housing', loc),
        (m) => _housingMetrics = m, (l) => _loadingHousing = l);
    _fetchCategory('Mobility', (loc) => _apiService.getContextMetrics('mobility', loc),
        (m) => _mobilityMetrics = m, (l) => _loadingMobility = l);
    _fetchCategory('Amenities', (loc) => _apiService.getContextMetrics('amenities', loc, radiusMeters: _radiusMeters),
        (m) => _amenityMetrics = m, (l) => _loadingAmenities = l);
    _fetchCategory('Environment', (loc) => _apiService.getContextMetrics('environment', loc),
        (m) => _environmentMetrics = m, (l) => _loadingEnvironment = l);
  }

  Future<void> _fetchCategory(
    String name,
    Future<ContextCategoryMetrics> Function(ContextLocation) fetcher,
    void Function(List<ContextMetric>) setter,
    void Function(bool) setLoading,
  ) async {
    setLoading(true);
    notifyListeners();
    try {
      final result = await fetcher(_location!);
      if (_isDisposed) return;
      setter(result.metrics);
      if (result.score != null) {
        _categoryScores[name] = result.score!;
      }
    } catch (e) {
      if (_isDisposed) return;
    } finally {
      if (!_isDisposed) {
        setLoading(false);
        notifyListeners();
      }
    }
  }

  void _clearMetrics() {
    _socialMetrics = null;
    _crimeMetrics = null;
    _demographicsMetrics = null;
    _housingMetrics = null;
    _mobilityMetrics = null;
    _amenityMetrics = null;
    _environmentMetrics = null;
    _categoryScores.clear();

    _loadingSocial = false;
    _loadingCrime = false;
    _loadingDemographics = false;
    _loadingHousing = false;
    _loadingMobility = false;
    _loadingAmenities = false;
    _loadingEnvironment = false;
  }

  void clear() {
    _error = null;
    _location = null;
    _clearMetrics();
    _expansionStates.clear();
    notifyListeners();
  }

  Future<void> clearHistory() async {
    await _historyService.clearHistory();
    await _loadHistory();
  }

  Future<void> removeFromHistory(String query) async {
    await _historyService.removeFromHistory(query);
    await _loadHistory();
  }
}
