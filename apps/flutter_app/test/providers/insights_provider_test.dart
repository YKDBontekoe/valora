import 'dart:async';

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

    test(
      'fetchMapData calls API when layers enabled and zoom sufficient',
      () async {
        provider.toggleAmenities();

        when(
          mockApiService.getMapAmenities(
            minLat: anyNamed('minLat'),
            minLon: anyNamed('minLon'),
            maxLat: anyNamed('maxLat'),
            maxLon: anyNamed('maxLon'),
          ),
        ).thenAnswer(
          (_) async => [
            MapAmenity(
              id: '1',
              type: 'school',
              name: 'School',
              location: const LatLng(52, 4),
            ),
          ],
        );

        await provider.fetchMapData(
          minLat: 51.9,
          minLon: 3.9,
          maxLat: 52.1,
          maxLon: 4.1,
          zoom: 14,
        );

        expect(provider.amenities, isNotEmpty);
        expect(provider.amenities[0].name, 'School');
      },
    );

    test('fetchMapData handles errors gracefully', () async {
      provider.toggleAmenities();

      when(
        mockApiService.getMapAmenities(
          minLat: anyNamed('minLat'),
          minLon: anyNamed('minLon'),
          maxLat: anyNamed('maxLat'),
          maxLon: anyNamed('maxLon'),
        ),
      ).thenThrow(Exception('API Error'));

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

    test(
      'fetchMapData reuses coverage and skips redundant amenity call',
      () async {
        provider.toggleAmenities();

        when(
          mockApiService.getMapAmenities(
            minLat: anyNamed('minLat'),
            minLon: anyNamed('minLon'),
            maxLat: anyNamed('maxLat'),
            maxLon: anyNamed('maxLon'),
          ),
        ).thenAnswer(
          (_) async => [
            MapAmenity(
              id: '1',
              type: 'school',
              name: 'School',
              location: const LatLng(52, 4),
            ),
          ],
        );

        await provider.fetchMapData(
          minLat: 51.9,
          minLon: 3.9,
          maxLat: 52.1,
          maxLon: 4.1,
          zoom: 14,
        );

        await provider.fetchMapData(
          minLat: 51.95,
          minLon: 3.95,
          maxLat: 52.05,
          maxLon: 4.05,
          zoom: 14,
        );

        verify(
          mockApiService.getMapAmenities(
            minLat: anyNamed('minLat'),
            minLon: anyNamed('minLon'),
            maxLat: anyNamed('maxLat'),
            maxLon: anyNamed('maxLon'),
          ),
        ).called(1);
      },
    );

    test('fetchMapData ignores stale responses from older requests', () async {
      provider.toggleAmenities();

      final firstResponse = Completer<List<MapAmenity>>();
      final secondResponse = Completer<List<MapAmenity>>();
      var call = 0;

      when(
        mockApiService.getMapAmenities(
          minLat: anyNamed('minLat'),
          minLon: anyNamed('minLon'),
          maxLat: anyNamed('maxLat'),
          maxLon: anyNamed('maxLon'),
        ),
      ).thenAnswer((_) {
        call++;
        return call == 1 ? firstResponse.future : secondResponse.future;
      });

      final firstFetch = provider.fetchMapData(
        minLat: 51.9,
        minLon: 3.9,
        maxLat: 52.1,
        maxLon: 4.1,
        zoom: 14,
      );

      final secondFetch = provider.fetchMapData(
        minLat: 53.0,
        minLon: 5.0,
        maxLat: 53.2,
        maxLon: 5.2,
        zoom: 14,
      );

      secondResponse.complete([
        MapAmenity(
          id: '2',
          type: 'park',
          name: 'Fresh',
          location: const LatLng(53.1, 5.1),
        ),
      ]);
      firstResponse.complete([
        MapAmenity(
          id: '1',
          type: 'school',
          name: 'Stale',
          location: const LatLng(52, 4),
        ),
      ]);

      await Future.wait([firstFetch, secondFetch]);

      expect(provider.amenities, hasLength(1));
      expect(provider.amenities.first.name, 'Fresh');
    });
  });
}
