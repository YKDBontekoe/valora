import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:http/http.dart' as http;
import 'package:valora_app/services/api_service.dart';

@GenerateMocks([http.Client])
import 'api_service_support_test.mocks.dart';

void main() {
  late MockClient mockClient;
  late ApiService apiService;

  setUp(() {
    mockClient = MockClient();
    apiService = ApiService(client: mockClient);
  });

  group('getSupportStatus', () {
    test('returns support status on success', () async {
      final jsonResponse = {
        'isSupportActive': true,
        'supportMessage': 'Online',
        'statusPageUrl': 'https://status.test',
        'contactEmail': 'test@valora.nl'
      };

      when(mockClient.get(any)).thenAnswer(
        (_) async => http.Response(json.encode(jsonResponse), 200),
      );

      final status = await apiService.getSupportStatus();

      expect(status.isSupportActive, true);
      expect(status.supportMessage, 'Online');
      expect(status.statusPageUrl, 'https://status.test');
      expect(status.contactEmail, 'test@valora.nl');
    });

    test('returns fallback on API error', () async {
      when(mockClient.get(any)).thenThrow(http.ClientException('Network error'));

      final status = await apiService.getSupportStatus();

      expect(status.isSupportActive, true); // Fallback defaults to true
      expect(status.supportMessage, 'Support is available.'); // Default fallback message
    });

    test('returns fallback on server error', () async {
      when(mockClient.get(any)).thenAnswer(
        (_) async => http.Response('Server Error', 500),
      );

      final status = await apiService.getSupportStatus();

      expect(status.isSupportActive, true);
    });
  });
}
