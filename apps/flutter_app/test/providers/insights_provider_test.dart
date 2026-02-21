import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/models/map_amenity.dart';
import 'package:valora_app/models/map_amenity_cluster.dart';
import 'package:valora_app/models/map_overlay_tile.dart';
import 'package:valora_app/models/map_property.dart';
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
      expect(provider.amenityClusters, isEmpty);
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

    test('fetchMapData calls getMapAmenityClusters when zoom is low', () async {
      provider.toggleAmenities();

      when(
        mockApiService.getMapAmenityClusters(
          minLat: anyNamed('minLat'),
          minLon: anyNamed('minLon'),
          maxLat: anyNamed('maxLat'),
          maxLon: anyNamed('maxLon'),
          zoom: anyNamed('zoom'),
        ),
      ).thenAnswer(
        (_) async => [
          MapAmenityCluster(
            latitude: 52.0,
            longitude: 4.0,
            count: 10,
            typeCounts: {'school': 5, 'park': 5},
          ),
        ],
      );

      await provider.fetchMapData(
        minLat: 51.0,
        minLon: 3.0,
        maxLat: 53.0,
        maxLon: 5.0,
        zoom: 10, // Low zoom
      );

      expect(provider.amenityClusters, isNotEmpty);
      expect(provider.amenityClusters[0].count, 10);
      expect(provider.amenities, isEmpty);
    });

    test('fetchMapData calls getMapOverlayTiles when zoom is low', () async {
      provider.toggleOverlays();

      when(
        mockApiService.getMapOverlayTiles(
          minLat: anyNamed('minLat'),
          minLon: anyNamed('minLon'),
          maxLat: anyNamed('maxLat'),
          maxLon: anyNamed('maxLon'),
          zoom: anyNamed('zoom'),
          metric: anyNamed('metric'),
        ),
      ).thenAnswer(
        (_) async => [
          MapOverlayTile(
            latitude: 52.0,
            longitude: 4.0,
            size: 0.1,
            value: 100,
            displayValue: '100',
          ),
        ],
      );
      await provider.fetchMapData(
        minLat: 51.0,
        minLon: 3.0,
        maxLat: 53.0,
        maxLon: 5.0,
        zoom: 10, // Low zoom
      );
        minLat: 51.0,
        minLon: 3.0,
        maxLon: 5.0,
        zoom: 10, // Low zoom
      );

      expect(provider.overlayTiles, isNotEmpty);
      expect(provider.overlayTiles[0].value, 100);
      expect(provider.overlays, isEmpty);
    });

    test("toggleProperties updates state and clears data when disabled", () {
      expect(provider.showProperties, isTrue); // Defaults to true

      provider.toggleProperties();
      expect(provider.showProperties, isFalse);
      expect(provider.properties, isEmpty);
    });

    test('fetchMapData calls getMapProperties when enabled', () async {
      // Ensure properties are enabled
      if (!provider.showProperties) provider.toggleProperties();

      when(
        mockApiService.getMapProperties(
          minLat: anyNamed('minLat'),
          minLon: anyNamed('minLon'),
          maxLat: anyNamed('maxLat'),
          maxLon: anyNamed('maxLon'),
        ),
      ).thenAnswer(
        (_) async => [
          MapProperty(
        zoom: 14,
      );

      expect(provider.properties, isNotEmpty);
      expect(provider.properties[0].id, "1");
    });

    test("fetchMapData handles properties errors gracefully", () async {
      if (!provider.showProperties) provider.toggleProperties();

      when(
        mockApiService.getMapProperties(
          minLat: anyNamed("minLat"),
          minLon: anyNamed("minLon"),
          maxLat: anyNamed("maxLat"),
          maxLon: anyNamed("maxLon"),
        ),
      ).thenThrow(Exception("API Error"));

      await provider.fetchMapData(
        minLat: 51.9,
        minLon: 3.9,
        maxLat: 52.1,
        maxLon: 4.1,
        zoom: 14,
      );
        maxLat: 52.1,
        maxLon: 4.1,
      expect(provider.properties, isNotEmpty);
      expect(provider.properties[0].id, "1");
    });

    test("fetchMapData handles properties errors gracefully", () async {
      if (!provider.showProperties) provider.toggleProperties();

      when(
        mockApiService.getMapProperties(
          minLat: anyNamed("minLat"),
          minLon: anyNamed("minLon"),
          maxLat: anyNamed("maxLat"),
          maxLon: anyNamed("maxLon"),
        ),
        zoom: 14,
      );

      expect(provider.properties, isEmpty);
      // We don't necessarily expose a specific property error state yet,
      // but ensure it doesn't crash and list remains empty.
  });
  });
}
