import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/models/map_amenity.dart';
import 'package:valora_app/models/map_amenity_cluster.dart';
import 'package:valora_app/models/map_overlay_tile.dart';
import 'package:valora_app/providers/insights_provider.dart';
import 'package:valora_app/repositories/map_repository.dart';
import 'package:latlong2/latlong.dart';

// Manual Mock
class MockMapRepository extends Fake implements MapRepository {
  List<MapAmenity> amenitiesResponse = [];
  List<MapAmenityCluster> clustersResponse = [];
  List<MapOverlayTile> tilesResponse = [];
  Exception? error;

  @override
  Future<List<MapAmenity>> getMapAmenities({required double minLat, required double minLon, required double maxLat, required double maxLon, List<String>? types}) async {
    if (error != null) throw error!;
    return amenitiesResponse;
  }

  @override
  Future<List<MapAmenityCluster>> getMapAmenityClusters({required double minLat, required double minLon, required double maxLat, required double maxLon, required double zoom, List<String>? types}) async {
    return clustersResponse;
  }

  @override
  Future<List<MapOverlayTile>> getMapOverlayTiles({required double minLat, required double minLon, required double maxLat, required double maxLon, required double zoom, required String metric}) async {
    return tilesResponse;
  }
}

void main() {
  late InsightsProvider provider;
  late MockMapRepository mockMapRepository;

  setUp(() {
    mockMapRepository = MockMapRepository();
    provider = InsightsProvider(mockMapRepository);
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

        mockMapRepository.amenitiesResponse = [
            MapAmenity(
              id: '1',
              type: 'school',
              name: 'School',
              location: const LatLng(52, 4),
            ),
        ];

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
      mockMapRepository.error = Exception('API Error');

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

      mockMapRepository.clustersResponse = [
          MapAmenityCluster(
            latitude: 52.0,
            longitude: 4.0,
            count: 10,
            typeCounts: {'school': 5, 'park': 5},
          ),
      ];

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

      mockMapRepository.tilesResponse = [
          MapOverlayTile(
            latitude: 52.0,
            longitude: 4.0,
            size: 0.1,
            value: 100,
            displayValue: '100',
          ),
      ];

      await provider.fetchMapData(
        minLat: 51.0,
        minLon: 3.0,
        maxLat: 53.0,
        maxLon: 5.0,
        zoom: 10, // Low zoom
      );

      expect(provider.overlayTiles, isNotEmpty);
      expect(provider.overlayTiles[0].value, 100);
      expect(provider.overlays, isEmpty);
    });
  });
}
