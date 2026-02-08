import 'package:flutter/foundation.dart';

import '../core/exceptions/app_exceptions.dart';
import '../models/context_report.dart';
import '../services/api_service.dart';

class ContextReportProvider extends ChangeNotifier {
  ContextReportProvider({required ApiService apiService}) : _apiService = apiService;

  final ApiService _apiService;

  bool _isLoading = false;
  String? _error;
  ContextReport? _report;
  int _radiusMeters = 1000;

  bool get isLoading => _isLoading;
  String? get error => _error;
  ContextReport? get report => _report;
  int get radiusMeters => _radiusMeters;

  void setRadiusMeters(int value) {
    _radiusMeters = value.clamp(200, 5000);
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
      _report = await _apiService.getContextReport(trimmed, radiusMeters: _radiusMeters);
      _error = null;
    } catch (e) {
      _report = null;
      _error = e is AppException ? e.message : 'Failed to generate context report.';
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
}
