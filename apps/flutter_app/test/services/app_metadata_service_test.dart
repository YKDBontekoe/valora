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
    // When init is called, it fetches support status too
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
    // Fallback logic in AppMetadataService creates a fallback status
    expect(appMetadataService.supportStatus?.isSupportActive, true);
    expect(appMetadataService.supportStatus?.contactEmail, 'support@valora.nl');
  });
}
