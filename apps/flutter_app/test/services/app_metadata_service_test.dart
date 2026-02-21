import 'dart:async';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:package_info_plus/package_info_plus.dart';
import 'package:valora_app/services/app_metadata_service.dart';
import 'package:valora_app/models/support_status.dart';
import 'app_metadata_service_test.mocks.dart';

void main() {
  late MockApiService mockApiService;
  late AppMetadataService appMetadataService;

  setUp(() {
    mockApiService = MockApiService();
    appMetadataService = AppMetadataService(mockApiService);

    PackageInfo.setMockInitialValues(
      appName: 'Valora Test',
      packageName: 'nl.valora.test',
      version: '1.2.3',
      buildNumber: '456',
      buildSignature: 'signature',
      installerStore: 'store',
    );
  });

  test('init loads package info', () async {
    when(mockApiService.getSupportStatus()).thenAnswer((_) async => SupportStatus.fallback());

    await appMetadataService.init();

    expect(appMetadataService.appName, 'Valora Test');
    expect(appMetadataService.version, '1.2.3');
    expect(appMetadataService.buildNumber, '456');
  });

  test('fetchSupportStatus loads status from API', () async {
    final status = SupportStatus(
      isSupportActive: true,
      supportMessage: 'Online',
      contactEmail: 'test@valora.nl',
    );

    when(mockApiService.getSupportStatus()).thenAnswer((_) async => status);

    await appMetadataService.fetchSupportStatus();

    expect(appMetadataService.supportStatus, status);
    expect(appMetadataService.supportStatus?.supportMessage, 'Online');
  });

  test('fetchSupportStatus handles error with fallback', () async {
    when(mockApiService.getSupportStatus()).thenThrow(Exception('API Error'));

    await appMetadataService.fetchSupportStatus();

    expect(appMetadataService.supportStatus, isNotNull);
    expect(appMetadataService.supportStatus?.isSupportActive, true);
    expect(appMetadataService.supportStatus?.contactEmail, 'support@valora.nl');
  });

  test('getters return default values before init', () {
    expect(appMetadataService.appName, 'Valora');
    expect(appMetadataService.version, 'Unknown');
    expect(appMetadataService.buildNumber, 'Unknown');
    expect(appMetadataService.packageName, 'Unknown');
  });

  test('update updates the api service', () {
    final newApiService = MockApiService();
    appMetadataService.update(newApiService);
  });

  test('fetchSupportStatus does nothing if already loading', () async {
    var completer = Completer<SupportStatus>();
    when(mockApiService.getSupportStatus()).thenAnswer((_) => completer.future);

    // Start first fetch
    final future1 = appMetadataService.fetchSupportStatus();

    // Start second fetch (should return early)
    final future2 = appMetadataService.fetchSupportStatus();

    // Complete the first one
    completer.complete(SupportStatus.fallback());
    await future1;
    await future2;

    // Verify API called only once
    verify(mockApiService.getSupportStatus()).called(1);
  });
}
