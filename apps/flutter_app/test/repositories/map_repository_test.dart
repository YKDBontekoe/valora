import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/repositories/map_repository.dart';
import 'package:mockito/mockito.dart';
import 'package:http/http.dart' as http;

import '../services/ai_service_test.mocks.dart';

void main() {
  late MockApiClient mockClient;
  late MapRepository repository;

  setUp(() {
    mockClient = MockApiClient();
    repository = MapRepository(mockClient);
  });

  test('getCityInsights caches result as UnmodifiableListView and invalidateCityInsightsCache clears it', () async {
    final mockResponse = http.Response(jsonEncode([
      {
        "city": "Test City",
        "count": 100,
        "latitude": 52.0,
        "longitude": 4.0,
        "compositeScore": 80.0,
        "safetyScore": 75.0,
        "socialScore": 85.0,
        "amenitiesScore": 90.0,
      }
    ]), 200);

    when(mockClient.get('/map/cities')).thenAnswer((_) async => mockResponse);
    when(mockClient.handleResponse<String>(any, any)).thenAnswer((_) async => mockResponse.body);

    final firstResult = await repository.getCityInsights();
    expect(firstResult.length, 1);
    expect(firstResult.first.city, "Test City");

    // Verify it throws when modified
    expect(() => firstResult.removeAt(0), throwsUnsupportedError);

    // Call again, should use cache and not call API again
    final secondResult = await repository.getCityInsights();
    expect(identical(firstResult, secondResult), true);

    verify(mockClient.get('/map/cities')).called(1);

    // Invalidate cache
    repository.invalidateCityInsightsCache();

    // Call again, should fetch fresh
    final thirdResult = await repository.getCityInsights();
    expect(identical(firstResult, thirdResult), false);
    verify(mockClient.get('/map/cities')).called(1);
  });
}
