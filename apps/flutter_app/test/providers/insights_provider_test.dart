import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/models/map_amenity.dart';
import 'package:valora_app/providers/insights_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:latlong2/latlong.dart';

import 'insights_provider_test.mocks.dart';

@GenerateMocks([ApiService])
void main() {
  late InsightsProvider provider;
  late MockApiService mockApiService;

  setUp(() {
    mockApiService = MockApiService();
    provider = InsightsProvider(mockApiService);
  });

  group('InsightsProvider', () {
    test('toggleAmenities updates state and clears data when disabled', () {
      expect(provider.showAmenities, isFalse);

      provider.toggleAmenities();
      expect(provider.showAmenities, isTrue);

      provider.toggleAmenities();
      expect(provider.showAmenities, isFalse);
      expect(provider.amenities, isEmpty);
    });

    test('fetchMapData calls API when layers enabled and zoom sufficient', () async {
      provider.toggleAmenities();

      when(mockApiService.getMapAmenities(
        minLat: anyNamed('minLat'),
        minLon: anyNamed('minLon'),
        maxLat: anyNamed('maxLat'),
        maxLon: anyNamed('maxLon'),
      )).thenAnswer((_) async => [
        MapAmenity(id: '1', type: 'school', name: 'School', location: const LatLng(52, 4))
      ]);

      await provider.fetchMapData(
        minLat: 51.9,
        minLon: 3.9,
        maxLat: 52.1,
        maxLon: 4.1,
        zoom: 14,
      );

      expect(provider.amenities, isNotEmpty);
      expect(provider.amenities[0].name, 'School');
    });

    test('fetchMapData handles errors gracefully', () async {
      provider.toggleAmenities();

      when(mockApiService.getMapAmenities(
        minLat: anyNamed('minLat'),
        minLon: anyNamed('minLon'),
        maxLat: anyNamed('maxLat'),
        maxLon: anyNamed('maxLon'),
      )).thenThrow(Exception('API Error'));

      await provider.fetchMapData(
        minLat: 51.9,
        minLon: 3.9,
        maxLat: 52.1,
        maxLon: 4.1,
        zoom: 14,
      );

      expect(provider.amenities, isEmpty);
      expect(provider.mapError, isNotNull);
    });
  });
}
