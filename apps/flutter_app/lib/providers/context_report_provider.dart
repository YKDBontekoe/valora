import 'package:flutter/foundation.dart';
import '../core/exceptions/app_exceptions.dart';
import '../models/context_report.dart';
import '../models/search_history_item.dart';
import '../services/api_service.dart';
import '../services/search_history_service.dart';

class ContextReportProvider extends ChangeNotifier {
  ApiService _apiService;
  final SearchHistoryService _historyService;

  ContextReportProvider({
    required ApiService apiService,
    SearchHistoryService? historyService,
  }) : _apiService = apiService,
       _historyService = historyService ?? SearchHistoryService() {
    _init();
  }

  // Called by ChangeNotifierProxyProvider.update
  void update(ApiService apiService) {
    _apiService = apiService;
  }

  bool _isLoading = false;
  String? _error;
  ContextReport? _report;
  int _radiusMeters = 1000;
  List<SearchHistoryItem> _history = [];

  // Persistent state for report children
  final Map<String, bool> _expansionStates = {};

  // AI Insights State
  final Map<String, String> _aiInsights = {};
  final Map<String, String> _aiInsightErrors = {};
  final Set<String> _loadingAiInsights = {};

  bool get isLoading => _isLoading;
  String? get error => _error;
  ContextReport? get report => _report;
  int get radiusMeters => _radiusMeters;
  List<SearchHistoryItem> get history => List.unmodifiable(_history);

  void _init() {
    _loadHistory();
  }

  void setRadiusMeters(int value) {
    _radiusMeters = value.clamp(200, 5000);
    notifyListeners();
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

  Future<void> _loadHistory() async {
    try {
      final history = await _historyService.getHistory();
      _history = history;
      notifyListeners();
    } catch (_) {
      // Ignore
    }
  }

  Future<void> generate(String input) async {
    if (_isLoading) return;

    final String trimmed = input.trim();
    if (trimmed.isEmpty) {
      _error = 'Enter an address.';
      notifyListeners();
      return;
    }

    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final report = await _apiService.getContextReport(trimmed);

      _report = report;
      _error = null;

      _expansionStates.clear();

      await _historyService.addToHistory(trimmed);
      await _loadHistory();
    } catch (e) {
      _report = null;
      _error = e.toString();
      if (e is AppException) {
         _error = e.message;
      }
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  void clear() {
    _error = null;
    _report = null;
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

  // AI Insight Methods
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
      // Placeholder:
      await Future.delayed(const Duration(seconds: 1));
      _aiInsights[location] = "AI Analysis not implemented in this refactor yet.";

    } catch (e) {
      _aiInsightErrors[location] = e.toString();
    } finally {
      _loadingAiInsights.remove(location);
      notifyListeners();
    }
  }
}
