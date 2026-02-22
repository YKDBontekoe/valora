import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/insights_provider.dart';
import 'package:valora_app/widgets/insights/insights_map.dart';
import 'package:valora_app/models/map_overlay.dart';
import 'package:valora_app/models/map_overlay_tile.dart';
import 'package:valora_app/models/map_amenity.dart';
import 'package:valora_app/models/map_amenity_cluster.dart';
import 'package:valora_app/models/map_city_insight.dart';

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
  Object? selectedFeature;

  @override
  double? getScore(MapCityInsight city) => null;

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  testWidgets('InsightsMap compiles and builds with record access', (WidgetTester tester) async {
    final provider = MockInsightsProvider();

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

    // If we reach here without a compilation error or runtime exception from record access, pass.
    expect(find.byType(FlutterMap), findsOneWidget);
  });
}
