import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/insights_provider.dart';
import 'package:valora_app/repositories/map_repository.dart';
import 'package:valora_app/widgets/insights/map_mode_selector.dart';
import 'package:valora_app/widgets/insights/persistent_details_panel.dart';
import 'package:valora_app/models/map_city_insight.dart';
import 'package:valora_app/models/map_amenity.dart';
import 'package:valora_app/models/map_amenity_cluster.dart';
import 'package:valora_app/models/map_overlay.dart';
import 'package:valora_app/models/map_overlay_tile.dart';
import 'package:latlong2/latlong.dart';
import 'package:mockito/mockito.dart';

class FakeMapRepository extends Fake implements MapRepository {
  @override
  Future<List<MapCityInsight>> getCityInsights() async => [];

  @override
  Future<List<MapAmenity>> getMapAmenities({required double minLat, required double minLon, required double maxLat, required double maxLon, List<String>? types}) async => [];

  @override
  Future<List<MapAmenityCluster>> getMapAmenityClusters({required double minLat, required double minLon, required double maxLat, required double maxLon, required double zoom, List<String>? types}) async => [];

  @override
  Future<List<MapOverlay>> getMapOverlays({required double minLat, required double minLon, required double maxLat, required double maxLon, required String metric}) async => [];

  @override
  Future<List<MapOverlayTile>> getMapOverlayTiles({required double minLat, required double minLon, required double maxLat, required double maxLon, required double zoom, required String metric}) async => [];
}

void main() {
  late InsightsProvider provider;
  late FakeMapRepository fakeRepo;

  setUp(() {
    fakeRepo = FakeMapRepository();
    provider = InsightsProvider(fakeRepo);
  });

  testWidgets('MapModeSelector switches modes', (WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider.value(
          value: provider,
          child: const Scaffold(body: MapModeSelector()),
        ),
      ),
    );

    // Initial state
    expect(provider.mapMode, MapMode.cities);
    expect(find.text('Cities'), findsOneWidget);

    // Tap Amenities
    await tester.tap(find.text('Amenities'));
    await tester.pumpAndSettle();

    expect(provider.mapMode, MapMode.amenities);
  });

  testWidgets('PersistentDetailsPanel shows city details when selected', (WidgetTester tester) async {
    final city = MapCityInsight(
      city: 'Test City',
      location: const LatLng(52, 5),
      count: 100,
      compositeScore: 85.0,
      safetyScore: 90.0,
      socialScore: 80.0,
      amenitiesScore: 85.0,
    );

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider.value(
          value: provider,
          child: const Scaffold(body: Stack(children: [PersistentDetailsPanel()])),
        ),
      ),
    );

    // Initially hidden
    expect(find.text('Test City'), findsNothing);

    // Select city
    provider.selectFeature(city);
    await tester.pumpAndSettle();

    // Should be visible
    expect(find.text('Test City'), findsOneWidget);
    expect(find.text('Composite Score'), findsOneWidget);

    // Close via provider directly to avoid off-screen tap issues in test environment due to animation
    provider.clearSelection();
    await tester.pumpAndSettle();

    expect(find.text('Test City'), findsNothing);
    expect(provider.selectedFeature, isNull);
  });
}
