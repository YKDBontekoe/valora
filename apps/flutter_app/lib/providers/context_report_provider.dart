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
  ContextReport? _report;
  int _radiusMeters = 1000;
  List<SearchHistoryItem> _history = [];
  int _historyLoadSeq = 0;

  bool get isLoading => _isLoading;
  String? get error => _error;
  ContextReport? get report => _report;
  int get radiusMeters => _radiusMeters;
  List<SearchHistoryItem> get history => List.unmodifiable(_history);

  @override
  void dispose() {
    _isDisposed = true;
    super.dispose();
  }

  void setRadiusMeters(int value) {
    _radiusMeters = value.clamp(200, 5000);
    notifyListeners();
  }

  Future<void> _loadHistory() async {
    final int seq = ++_historyLoadSeq;
    final history = await _historyService.getHistory();

    if (_isDisposed || seq != _historyLoadSeq) return;

    _history = history;
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
      // 1. Fetch Report
      final report = await _apiService.getContextReport(
        trimmed,
        radiusMeters: _radiusMeters,
      );

      if (_isDisposed) return;

      _report = report;
      _error = null;

      // 2. Update History (Failures here should not fail the report)
      try {
        await _historyService.addToHistory(trimmed);
        await _loadHistory();
      } catch (_) {
        // Ignore history save errors
      }
    } catch (e) {
      if (_isDisposed) return;
      _report = null;
      _error =
          e is AppException ? e.message : 'Failed to generate context report.';
    } finally {
      if (!_isDisposed) {
        _isLoading = false;
        notifyListeners();
      }
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
