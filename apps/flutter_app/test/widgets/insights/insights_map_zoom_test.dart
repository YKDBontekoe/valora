import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/map_amenity.dart';
import 'package:valora_app/models/map_amenity_cluster.dart';
import 'package:valora_app/models/map_city_insight.dart';
import 'package:valora_app/models/map_overlay_tile.dart';
import 'package:valora_app/providers/insights_provider.dart';
import 'package:valora_app/widgets/insights/insights_map.dart';
import 'package:valora_app/models/map_overlay.dart';

// Create a mock provider that extends ChangeNotifier to support listeners
class MockInsightsProvider extends ChangeNotifier implements InsightsProvider {
  @override
  bool showOverlays = false;
  @override
  bool showAmenities = false;
  @override
  List<MapOverlay> overlays = [];
  @override
  List<MapOverlayTile> overlayTiles = [];
  @override
  List<MapAmenity> amenities = [];
  @override
  List<MapAmenityCluster> amenityClusters = [];
  @override
  List<MapCityInsight> cities = [];
  @override
  MapOverlayMetric selectedOverlayMetric = MapOverlayMetric.pricePerSquareMeter;

  @override
  double? getScore(MapCityInsight city) => city.compositeScore;

  // Implement other required members with dummy values or throws
  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  testWidgets('InsightsMap renders tiles when overlayTiles are present', (WidgetTester tester) async {
    final provider = MockInsightsProvider();
    provider.showOverlays = true;
    provider.overlayTiles = [
      MapOverlayTile(
        latitude: 52.0,
        longitude: 5.0,
        size: 0.1,
        value: 100,
        displayValue: '100',
      ),
    ];

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<InsightsProvider>.value(
          value: provider,
          child: InsightsMap(
            mapController: MapController(),
            onMapChanged: () {},
          ),
        ),
      ),
    );

    await tester.pump(); // Allow map to build

    // Verify that PolygonLayer is present
    expect(find.byType(PolygonLayer), findsOneWidget);
  });

  testWidgets('InsightsMap renders clusters when amenityClusters are present', (WidgetTester tester) async {
    final provider = MockInsightsProvider();
    provider.showAmenities = true;
    provider.amenityClusters = [
      MapAmenityCluster(
        latitude: 52.0,
        longitude: 5.0,
        count: 10,
        typeCounts: {'school': 10},
      ),
    ];

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<InsightsProvider>.value(
          value: provider,
          child: InsightsMap(
            mapController: MapController(),
            onMapChanged: () {},
          ),
        ),
      ),
    );

    await tester.pump();

    // Verify that MarkerLayer is present (for clusters)
    // There's always one MarkerLayer for cities, so we expect 2 if clusters are shown
    expect(find.byType(MarkerLayer), findsAtLeastNWidgets(2));
  });
}
