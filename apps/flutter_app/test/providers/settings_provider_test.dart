import 'package:flutter_test/flutter_test.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/providers/settings_provider.dart';

void main() {
  group('SettingsProvider', () {
    late SettingsProvider provider;

    setUp(() async {
      SharedPreferences.setMockInitialValues({});
      PackageInfo.setMockInitialValues(
        appName: 'Valora',
        packageName: 'com.valora.app',
        version: '1.0.0',
        buildNumber: '1',
        buildSignature: '',
      );
      provider = SettingsProvider();
      await Future.delayed(Duration.zero); // Wait for async init
    });

    test('initial values are correct', () {
      expect(provider.reportRadius, 500.0);
      expect(provider.mapDefaultMetric, 'price');
      expect(provider.notificationsEnabled, isTrue);
      expect(provider.notificationFrequency, 'daily');
      expect(provider.diagnosticsEnabled, isFalse);
    });

    test('updates report radius immediately', () {
      provider.setReportRadius(1000.0);
      expect(provider.reportRadius, 1000.0);
    });

    test('persists report radius', () async {
      provider.setReportRadius(1500.0);
      await provider.persistReportRadius();

      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getDouble('settings_report_radius'), 1500.0);
    });

    test('updates and persists map metric', () async {
      await provider.setMapDefaultMetric('size');
      expect(provider.mapDefaultMetric, 'size');

      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getString('settings_map_metric'), 'size');
    });

    test('updates and persists notifications enabled', () async {
      await provider.setNotificationsEnabled(false);
      expect(provider.notificationsEnabled, isFalse);

      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getBool('settings_notifications_enabled'), isFalse);
    });

    test('updates and persists notification frequency', () async {
      await provider.setNotificationFrequency('weekly');
      expect(provider.notificationFrequency, 'weekly');

      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getString('settings_notification_frequency'), 'weekly');
    });

    test('updates and persists diagnostics enabled', () async {
      await provider.setDiagnosticsEnabled(true);
      expect(provider.diagnosticsEnabled, isTrue);

      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getBool('settings_diagnostics_enabled'), isTrue);
    });

    test('loads app info correctly', () async {
      // Re-initialize to trigger loadAppInfo
      provider = SettingsProvider();
      await Future.delayed(const Duration(milliseconds: 100)); // Allow async load to complete

      expect(provider.appVersion, '1.0.0');
      expect(provider.buildNumber, '1');
    });
  });
}
