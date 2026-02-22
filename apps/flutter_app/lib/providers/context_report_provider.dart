import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

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
    loadComparisonSet();
  }

  final ApiService _apiService;
  final SearchHistoryService _historyService;

  bool _isLoading = false;
  bool _isDisposed = false;
  String? _error;

  // Multiple reports state
  final Map<String, ContextReport> _activeReports = {};
  final Set<String> _comparisonIds = {};
  String? _currentReportId;

  int _radiusMeters = 1000;
  List<SearchHistoryItem> _history = [];
  int _historyLoadSeq = 0;

  // Persistent state for report children
  final Map<String, bool> _expansionStates = {};
  final Map<String, String> _aiInsights = {};
  final Map<String, String> _aiInsightErrors = {};
  final Set<String> _loadingAiInsights = {};

  bool get isLoading => _isLoading;
  String? get error => _error;

  ContextReport? getReportById(String id) => _activeReports[id];

  ContextReport? get report {
    if (_currentReportId != null) {
      return _activeReports[_currentReportId];
    }
    return null;
  }

  List<ContextReport> get comparisonReports {
    return _comparisonIds
        .map((id) => _activeReports[id])
        .whereType<ContextReport>()
        .toList();
  }

  Set<String> get comparisonIds => Set.unmodifiable(_comparisonIds);

  int get radiusMeters => _radiusMeters;
  List<SearchHistoryItem> get history => List.unmodifiable(_history);

  @override
  void dispose() {
    _isDisposed = true;
    super.dispose();
  }

  String _getReportId(String query, int radius) => '$query|$radius';

  void setRadiusMeters(int value) {
    _radiusMeters = value.clamp(200, 5000);
    notifyListeners();
  }

  // Comparison management
  bool isComparing(String query, int radius) {
    final id = _getReportId(query, radius);
    return _comparisonIds.contains(id);
  }

  Future<void> addToComparison(String query, int radius) async {
    final id = _getReportId(query, radius);
    if (!_comparisonIds.contains(id)) {
      _comparisonIds.add(id);
      notifyListeners();
      _saveComparisonSet();

      if (!_activeReports.containsKey(id)) {
        await fetchReport(query, radius);
      }
    }
  }

  Future<void> fetchReport(String query, int radius) async {
    final id = _getReportId(query, radius);
    if (_activeReports.containsKey(id)) return;

    try {
      final report = await _apiService.getContextReport(
        query,
        radiusMeters: radius,
      );
      if (_isDisposed) return;
      _activeReports[id] = report;
      notifyListeners();
    } catch (e) {
      // If fetch fails, ignore
    }
  }

  void removeFromComparison(String query, int radius) {
    final id = _getReportId(query, radius);
    if (_comparisonIds.contains(id)) {
      _comparisonIds.remove(id);
      notifyListeners();
      _saveComparisonSet();
    }
  }

  Future<void> toggleComparison(String query, int radius) async {
    if (isComparing(query, radius)) {
      removeFromComparison(query, radius);
    } else {
      await addToComparison(query, radius);
    }
  }

  void clearComparison() {
    if (_comparisonIds.isNotEmpty) {
      _comparisonIds.clear();
      notifyListeners();
      _saveComparisonSet();
    }
  }

  Future<void> _saveComparisonSet() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setStringList("comparison_ids", _comparisonIds.toList());
    } catch (_) {
      // Ignore persistence errors
    }
  }

  Future<void> loadComparisonSet() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final ids = prefs.getStringList("comparison_ids") ?? [];
      if (ids.isNotEmpty) {
        _comparisonIds.addAll(ids);
        notifyListeners();
      }
    } catch (_) {
      // Ignore persistence errors
    }
  }

  // Expansion state management
  bool isExpanded(String category, {bool defaultValue = false}) {
    return _expansionStates[category] ?? defaultValue;
  }

  void setExpanded(String category, bool expanded) {
    if (_expansionStates[category] == expanded) return;
    _expansionStates[category] = expanded;
    notifyListeners();
  }

  // AI Insight state management
  String? getAiInsight(String location) => _aiInsights[location];
  String? getAiInsightError(String location) => _aiInsightErrors[location];
  bool isAiInsightLoading(String location) =>
      _loadingAiInsights.contains(location);

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
      _error = 'Enter an address or Funda URL.';
      notifyListeners();
      return;
    }

    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      // 1. Fetch Report
      final report = await _apiService.getContextReport(
        trimmed,
        radiusMeters: _radiusMeters,
      );

      if (_isDisposed) return;

      final id = _getReportId(trimmed, _radiusMeters);
      _activeReports[id] = report;
      _currentReportId = id;
      _error = null;

      // Reset expansion states for new report
      _expansionStates.clear();

      // 2. Update History (Failures here should not fail the report)
      try {
        await _historyService.addToHistory(trimmed);
        await _loadHistory();
      } catch (_) {
        // Ignore history save errors
      }
    } catch (e) {
      if (_isDisposed) return;
      _currentReportId = null;
      _error = e is AppException
          ? e.message
          : 'Failed to generate context report.';
    } finally {
      if (!_isDisposed) {
        _isLoading = false;
        notifyListeners();
      }
    }
  }

  void clear() {
    _error = null;
    _currentReportId = null;
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
