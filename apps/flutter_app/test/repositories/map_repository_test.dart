import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/repositories/map_repository.dart';
import 'package:valora_app/services/api_client.dart';

@GenerateMocks([ApiClient])
import 'map_repository_test.mocks.dart';

void main() {
  late MapRepository repository;
  late MockApiClient mockClient;

  setUp(() {
    mockClient = MockApiClient();
    repository = MapRepository(mockClient);
  });

  group('MapRepository', () {
    test('getCityInsights parses response correctly', () async {
      final jsonBody = jsonEncode([
        {'city': 'Amsterdam', 'count': 1, 'compositeScore': 80.0, 'latitude': 52.3, 'longitude': 4.9}
      ]);
      final response = http.Response(jsonBody, 200);

      when(mockClient.get('/map/cities'))
          .thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((invocation) {
        final parser = invocation.positionalArguments[1] as Function;
        return parser(jsonBody);
      });

      final result = await repository.getCityInsights();

      expect(result.length, 1);
      expect(result.first.city, 'Amsterdam');
    });

    test('getMapAmenities passes parameters correctly', () async {
      final jsonBody = jsonEncode([]);
      final response = http.Response(jsonBody, 200);

      when(mockClient.get(
        '/map/amenities',
        queryParameters: anyNamed('queryParameters'),
      )).thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((invocation) {
        final parser = invocation.positionalArguments[1] as Function;
        return parser(jsonBody);
      });

      await repository.getMapAmenities(
        minLat: 52.0,
        minLon: 4.0,
        maxLat: 53.0,
        maxLon: 5.0,
        types: ['park'],
      );

      verify(mockClient.get(
        '/map/amenities',
        queryParameters: {
          'minLat': '52.0',
          'minLon': '4.0',
          'maxLat': '53.0',
          'maxLon': '5.0',
          'types': 'park',
        },
      )).called(1);
    });

    test('getMapAmenityClusters parses response correctly', () async {
      final jsonBody = jsonEncode([
        {'latitude': 52.0, 'longitude': 4.0, 'count': 10, 'typeCounts': {}}
      ]);
      final response = http.Response(jsonBody, 200);

      when(mockClient.get(
        '/map/amenities/clusters',
        queryParameters: anyNamed('queryParameters'),
      )).thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((invocation) {
        final parser = invocation.positionalArguments[1] as Function;
        return parser(jsonBody);
      });

      final result = await repository.getMapAmenityClusters(
        minLat: 52.0,
        minLon: 4.0,
        maxLat: 53.0,
        maxLon: 5.0,
        zoom: 10,
      );

      expect(result.length, 1);
      expect(result.first.count, 10);
    });

    test('getMapOverlays parses response correctly', () async {
      final jsonBody = jsonEncode([
        {'id': '1', 'name': 'Overlay', 'metricName': 'price', 'metricValue': 100.0, 'displayValue': '100', 'geoJson': {}}
      ]);
      final response = http.Response(jsonBody, 200);

      when(mockClient.get(
        '/map/overlays',
        queryParameters: anyNamed('queryParameters'),
      )).thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((invocation) {
        final parser = invocation.positionalArguments[1] as Function;
        return parser(jsonBody);
      });

      final result = await repository.getMapOverlays(
        minLat: 52.0,
        minLon: 4.0,
        maxLat: 53.0,
        maxLon: 5.0,
        metric: 'price',
      );

      expect(result.length, 1);
      expect(result.first.name, 'Overlay');
    });

    test('getMapOverlayTiles parses response correctly', () async {
      final jsonBody = jsonEncode([
        {'latitude': 52.0, 'longitude': 4.0, 'size': 0.1, 'value': 10.0, 'displayValue': '10'}
      ]);
      final response = http.Response(jsonBody, 200);

      when(mockClient.get(
        '/map/overlays/tiles',
        queryParameters: anyNamed('queryParameters'),
      )).thenAnswer((_) async => response);

      when(mockClient.handleResponse(any, any)).thenAnswer((invocation) {
        final parser = invocation.positionalArguments[1] as Function;
        return parser(jsonBody);
      });

      final result = await repository.getMapOverlayTiles(
        minLat: 52.0,
        minLon: 4.0,
        maxLat: 53.0,
        maxLon: 5.0,
        zoom: 10,
        metric: 'price',
      );

      expect(result.length, 1);
      expect(result.first.value, 10.0);
    });
  });
}
