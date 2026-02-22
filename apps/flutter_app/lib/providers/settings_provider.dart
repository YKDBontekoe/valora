import 'package:flutter/material.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'auth_provider.dart';
import '../services/search_history_service.dart';

class SettingsProvider extends ChangeNotifier {
  static const String _reportRadiusKey = 'settings_report_radius';
  static const String _mapMetricKey = 'settings_map_metric';
  static const String _notificationsEnabledKey = 'settings_notifications_enabled';
  static const String _notificationFrequencyKey = 'settings_notification_frequency';
  static const String _diagnosticsEnabledKey = 'settings_diagnostics_enabled';

  double _reportRadius = 500.0;
  String _mapDefaultMetric = 'price';
  bool _notificationsEnabled = true;
  String _notificationFrequency = 'daily';
  bool _diagnosticsEnabled = false;

  String _appVersion = '';
  String _buildNumber = '';
  bool _isInitialized = false;

  double get reportRadius => _reportRadius;
  String get mapDefaultMetric => _mapDefaultMetric;
  bool get notificationsEnabled => _notificationsEnabled;
  String get notificationFrequency => _notificationFrequency;
  bool get diagnosticsEnabled => _diagnosticsEnabled;
  String get appVersion => _appVersion;
  String get buildNumber => _buildNumber;
  bool get isInitialized => _isInitialized;

  SettingsProvider() {
    _loadSettings();
    _loadAppInfo();
  }

  Future<void> _loadSettings() async {
    final prefs = await SharedPreferences.getInstance();
    _reportRadius = prefs.getDouble(_reportRadiusKey) ?? 500.0;
    _mapDefaultMetric = prefs.getString(_mapMetricKey) ?? 'price';
    _notificationsEnabled = prefs.getBool(_notificationsEnabledKey) ?? true;
    _notificationFrequency = prefs.getString(_notificationFrequencyKey) ?? 'daily';
    _diagnosticsEnabled = prefs.getBool(_diagnosticsEnabledKey) ?? false;
    _isInitialized = true;
    notifyListeners();
  }

  Future<void> _loadAppInfo() async {
    try {
      final packageInfo = await PackageInfo.fromPlatform();
      _appVersion = packageInfo.version;
      _buildNumber = packageInfo.buildNumber;
    } catch (e) {
      // Fallback or ignore if platform info fails
      _appVersion = 'Unknown';
      _buildNumber = '0';
      debugPrint('Error loading app info: $e');
    }
    notifyListeners();
  }

  // Update UI immediately, but persist only when finished
  void setReportRadius(double value) {
    _reportRadius = value;
    notifyListeners();
  }

  Future<void> persistReportRadius() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setDouble(_reportRadiusKey, _reportRadius);
  }

  Future<void> setMapDefaultMetric(String value) async {
    _mapDefaultMetric = value;
    notifyListeners();
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_mapMetricKey, value);
  }

  Future<void> setNotificationsEnabled(bool value) async {
    _notificationsEnabled = value;
    notifyListeners();
    final prefs = await SharedPreferences.getInstance();
    await prefs.setBool(_notificationsEnabledKey, value);
  }

  Future<void> setNotificationFrequency(String value) async {
    _notificationFrequency = value;
    notifyListeners();
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_notificationFrequencyKey, value);
  }

  Future<void> setDiagnosticsEnabled(bool value) async {
    _diagnosticsEnabled = value;
    notifyListeners();
    final prefs = await SharedPreferences.getInstance();
    await prefs.setBool(_diagnosticsEnabledKey, value);
  }

  Future<void> clearAllData(BuildContext context) async {
    // 1. Clear search history
    await SearchHistoryService().clearHistory();

    // 2. Clear local settings (reset to defaults)
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_reportRadiusKey);
    await prefs.remove(_mapMetricKey);
    await prefs.remove(_notificationsEnabledKey);
    await prefs.remove(_notificationFrequencyKey);
    await prefs.remove(_diagnosticsEnabledKey);

    _reportRadius = 500.0;
    _mapDefaultMetric = 'price';
    _notificationsEnabled = true;
    _notificationFrequency = 'daily';
    _diagnosticsEnabled = false;
    notifyListeners();

    // 3. Clear auth/user data (logout)
    if (context.mounted) {
      await context.read<AuthProvider>().logout();
    }
  }
}
