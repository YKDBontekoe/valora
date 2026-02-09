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
  String? _error;
  ContextReport? _report;
  int _radiusMeters = 1000;
  List<SearchHistoryItem> _history = [];

  bool get isLoading => _isLoading;
  String? get error => _error;
  ContextReport? get report => _report;
  int get radiusMeters => _radiusMeters;
  List<SearchHistoryItem> get history => _history;

  void setRadiusMeters(int value) {
    _radiusMeters = value.clamp(200, 5000);
    notifyListeners();
  }

  Future<void> _loadHistory() async {
    _history = await _historyService.getHistory();
    notifyListeners();
  }

  Future<void> generate(String input) async {
    final String trimmed = input.trim();
    if (trimmed.isEmpty) {
      _error = 'Enter an address or listing link.';
      notifyListeners();
      return;
    }

    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _report = await _apiService.getContextReport(
        trimmed,
        radiusMeters: _radiusMeters,
      );
      _error = null;
      await _historyService.addToHistory(trimmed);
      await _loadHistory();
    } catch (e) {
      _report = null;
      _error =
          e is AppException ? e.message : 'Failed to generate context report.';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  void clear() {
    _error = null;
    _report = null;
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
