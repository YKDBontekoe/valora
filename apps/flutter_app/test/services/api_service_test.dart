import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/services/api_service.dart';

void main() {
  late ApiService apiService;
  late MockClient mockClient;

  Future<http.Response> defaultHandler(http.Request request) async {
    return http.Response('{}', 200);
  }

  setUp(() {
    mockClient = MockClient(defaultHandler);
    apiService = ApiService(client: mockClient);
  });

  group('ApiService', () {
    test('getContextReport makes correct request', () async {
      mockClient = MockClient((request) async {
        expect(request.url.path.endsWith('/context/report'), isTrue);
        expect(request.method, 'POST');
        final body = json.decode(request.body);
        expect(body['input'], 'Test Address');

        return http.Response(
          json.encode({
            'location': {
              'query': 'Test Address',
              'displayAddress': 'Test Address, City',
              'latitude': 52.3676,
              'longitude': 4.9041,
              'municipalityName': 'Amsterdam',
            },
            'socialMetrics': [],
            'crimeMetrics': [],
            'demographicsMetrics': [],
            'housingMetrics': [],
            'mobilityMetrics': [],
            'amenityMetrics': [],
            'environmentMetrics': [],
            'compositeScore': 8.5,
            'categoryScores': {'safety': 9.0},
            'sources': [],
            'warnings': [],
          }),
          200,
        );
      });
      apiService = ApiService(client: mockClient);

      await apiService.getContextReport('Test Address');
    });

    test('getUnreadNotificationCount returns count', () async {
      mockClient = MockClient((request) async {
        return http.Response(json.encode({'count': 5}), 200);
      });
      apiService = ApiService(client: mockClient);

      final count = await apiService.getUnreadNotificationCount();
      expect(count, 5);
    });

    test('healthCheck returns true on 200', () async {
      mockClient = MockClient((request) async {
        return http.Response('OK', 200);
      });
      apiService = ApiService(client: mockClient);

      expect(await apiService.healthCheck(), isTrue);
    });

    test('throws JsonParsingException when server returns invalid JSON (e.g. HTML)', () async {
      mockClient = MockClient((request) async {
        return http.Response('<html>Bad Gateway</html>', 200);
      });
      apiService = ApiService(client: mockClient);

      expect(
        () => apiService.getContextReport('Test Address'),
        throwsA(isA<JsonParsingException>()),
      );
    });
  });
}
