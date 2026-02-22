import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:valora_app/providers/insights_provider.dart';
import 'package:valora_app/widgets/insights/insights_header.dart';
import 'package:valora_app/widgets/insights/insights_legend.dart';
import 'package:valora_app/widgets/insights/insights_metric_selector.dart';
import 'package:valora_app/widgets/insights/insights_controls.dart';
import 'package:valora_app/widgets/insights/insights_map.dart';
import 'package:valora_app/models/map_city_insight.dart';
import 'package:valora_app/models/map_overlay.dart';
import 'package:valora_app/models/map_amenity.dart';
import 'package:latlong2/latlong.dart';
import 'dart:io';

@GenerateNiceMocks([MockSpec<InsightsProvider>()])
import 'insights_widgets_test.mocks.dart';

void main() {
  late MockInsightsProvider mockProvider;

  setUp(() {
    mockProvider = MockInsightsProvider();
    when(mockProvider.selectedMetric).thenReturn(InsightMetric.composite);
    when(mockProvider.showOverlays).thenReturn(false);
    when(mockProvider.showAmenities).thenReturn(false);
    when(mockProvider.cities).thenReturn([]);
    when(mockProvider.overlays).thenReturn([]);
    when(mockProvider.amenities).thenReturn([]);
    when(
      mockProvider.selectedOverlayMetric,
    ).thenReturn(MapOverlayMetric.pricePerSquareMeter);
    when(mockProvider.mapError).thenReturn(null);

    HttpOverrides.global = null;
  });

  Widget createWidget(Widget child) {
    return ChangeNotifierProvider<InsightsProvider>.value(
      value: mockProvider,
      child: MaterialApp(
        home: Scaffold(
          body: SizedBox(
            width: 800,
            height: 600,
            child: Stack(children: [child]),
          ),
        ),
      ),
    );
  }

  // ... (Existing tests) ...
  group('InsightsHeader', () {
    testWidgets('displays title and city count', (WidgetTester tester) async {
      when(mockProvider.cities).thenReturn([
        MapCityInsight(city: 'City A', count: 10, location: const LatLng(0, 0)),
        MapCityInsight(city: 'City B', count: 5, location: const LatLng(0, 0)),
      ]);

      await tester.pumpWidget(createWidget(const InsightsHeader()));

      expect(find.text('Area Insights'), findsOneWidget);
      expect(find.text('2 cities'), findsOneWidget);
      expect(find.byIcon(Icons.insights_rounded), findsOneWidget);
    });
  });

  group('InsightsLegend', () {
    testWidgets('displays correct label for Overall metric', (
      WidgetTester tester,
    ) async {
      when(mockProvider.selectedMetric).thenReturn(InsightMetric.composite);
      await tester.pumpWidget(createWidget(const InsightsLegend()));
      expect(find.text('Overall score'), findsOneWidget);
    });

    testWidgets('displays correct label for Safety metric', (
      WidgetTester tester,
    ) async {
      when(mockProvider.selectedMetric).thenReturn(InsightMetric.safety);
      await tester.pumpWidget(createWidget(const InsightsLegend()));
      expect(find.text('Safety score'), findsOneWidget);
    });

    testWidgets('displays legend rows', (WidgetTester tester) async {
      when(mockProvider.selectedMetric).thenReturn(InsightMetric.composite);
      await tester.pumpWidget(createWidget(const InsightsLegend()));
      expect(find.text('80+'), findsOneWidget);
      expect(find.text('60-79'), findsOneWidget);
      expect(find.text('40-59'), findsOneWidget);
      expect(find.text('<40'), findsOneWidget);
    });
  });

  group('InsightsMetricSelector', () {
    testWidgets('displays all metric chips', (WidgetTester tester) async {
      when(mockProvider.selectedMetric).thenReturn(InsightMetric.composite);
      await tester.pumpWidget(createWidget(const InsightsMetricSelector()));

      expect(find.text('Overall'), findsOneWidget);
      expect(find.text('Safety'), findsOneWidget);
      expect(find.text('Social'), findsOneWidget);
      expect(find.text('Amenities'), findsOneWidget);
    });

    testWidgets('calls setMetric on chip tap', (WidgetTester tester) async {
      when(mockProvider.selectedMetric).thenReturn(InsightMetric.composite);
      await tester.pumpWidget(createWidget(const InsightsMetricSelector()));

      await tester.tap(find.text('Safety'));
      verify(mockProvider.setMetric(InsightMetric.safety)).called(1);
    });
  });

  group('InsightsControls', () {
    testWidgets('displays zoom buttons and toggles', (
      WidgetTester tester,
    ) async {
      when(mockProvider.showOverlays).thenReturn(false);
      when(mockProvider.showAmenities).thenReturn(false);

      await tester.pumpWidget(
        createWidget(
          InsightsControls(
            onZoomIn: () {},
            onZoomOut: () {},
            onMapChanged: () {},
          ),
        ),
      );

      expect(find.byIcon(Icons.add_rounded), findsOneWidget);
      expect(find.byIcon(Icons.remove_rounded), findsOneWidget);
      expect(find.byIcon(Icons.place_rounded), findsOneWidget);
      expect(find.byIcon(Icons.layers_rounded), findsOneWidget);
    });

    testWidgets('triggers zoom callbacks', (WidgetTester tester) async {
      bool zoomInCalled = false;
      bool zoomOutCalled = false;

      await tester.pumpWidget(
        createWidget(
          InsightsControls(
            onZoomIn: () => zoomInCalled = true,
            onZoomOut: () => zoomOutCalled = true,
            onMapChanged: () {},
          ),
        ),
      );

      await tester.tap(find.byIcon(Icons.add_rounded));
      expect(zoomInCalled, isTrue);

      await tester.tap(find.byIcon(Icons.remove_rounded));
      expect(zoomOutCalled, isTrue);
    });

    testWidgets('toggles provider amenities state', (
      WidgetTester tester,
    ) async {
      when(mockProvider.showAmenities).thenReturn(false);
      await tester.pumpWidget(
        createWidget(
          InsightsControls(
            onZoomIn: () {},
            onZoomOut: () {},
            onMapChanged: () {},
          ),
        ),
      );

      await tester.tap(find.byIcon(Icons.place_rounded));
      verify(mockProvider.toggleAmenities()).called(1);
    });

    testWidgets('toggles provider overlays state', (WidgetTester tester) async {
      when(mockProvider.showOverlays).thenReturn(false);
      await tester.pumpWidget(
        createWidget(
          InsightsControls(
            onZoomIn: () {},
            onZoomOut: () {},
            onMapChanged: () {},
          ),
        ),
      );

      await tester.tap(find.byIcon(Icons.layers_rounded));
      verify(mockProvider.toggleOverlays()).called(1);
    });

    testWidgets('shows dropdown when overlays are enabled', (
      WidgetTester tester,
    ) async {
      when(mockProvider.showOverlays).thenReturn(true);
      when(
        mockProvider.selectedOverlayMetric,
      ).thenReturn(MapOverlayMetric.pricePerSquareMeter);

      await tester.pumpWidget(
        createWidget(
          InsightsControls(
            onZoomIn: () {},
            onZoomOut: () {},
            onMapChanged: () {},
          ),
        ),
      );

      expect(find.byType(DropdownButton<MapOverlayMetric>), findsOneWidget);
      expect(find.text('Price/m²'), findsOneWidget);
    });
  });

  group('InsightsMap Widget', () {
    testWidgets('renders FlutterMap with TileLayer', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        createWidget(
          InsightsMap(mapController: MapController(), onMapChanged: () {}),
        ),
      );

      expect(find.byType(FlutterMap), findsOneWidget);
      expect(find.byType(TileLayer), findsOneWidget);
    });

    testWidgets('renders city markers via widget tree', (
      WidgetTester tester,
    ) async {
      when(mockProvider.cities).thenReturn([
        MapCityInsight(
          city: 'Test City',
          count: 100,
          location: const LatLng(52.0, 5.0),
          compositeScore: 85,
        ),
      ]);
      when(mockProvider.getScore(any)).thenReturn(85.0);

      await tester.pumpWidget(
        createWidget(
          InsightsMap(mapController: MapController(), onMapChanged: () {}),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.byType(MarkerLayer), findsWidgets);
      expect(find.text('85'), findsOneWidget);
    });
  });

  group('InsightsMap Static Builders (Unit Tests)', () {
    test('buildPolygon returns correct Polygon', () {
      final overlay = MapOverlay(
        id: '1',
        name: 'Test',
        metricName: 'price',
        metricValue: 5000,
        displayValue: 'High',
        geoJson: {
          'geometry': {
            'type': 'Polygon',
            'coordinates': [
              [
                [5.0, 52.0],
                [5.1, 52.0],
                [5.1, 52.1],
                [5.0, 52.1],
                [5.0, 52.0],
              ],
            ],
          },
        },
      );

      final polygon = InsightsMap.buildPolygon(
        overlay,
        MapOverlayMetric.pricePerSquareMeter,
      );

      expect(polygon.points.length, 5);
      expect(polygon.points.first.latitude, 52.0);
      expect(polygon.points.first.longitude, 5.0);
      expect(polygon.color, isNotNull);
      expect(polygon.borderColor, Colors.orange);
    });

    testWidgets('buildAmenityMarker returns correct Marker', (
      WidgetTester tester,
    ) async {
      final amenity = MapAmenity(
        id: '1',
        type: 'school',
        name: 'School',
        location: const LatLng(52.0, 5.0),
      );

      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) {
              final marker = InsightsMap.buildAmenityMarker(context, amenity);
              return SizedBox(width: 100, height: 100, child: marker.child);
            },
          ),
        ),
      );

      expect(find.byIcon(Icons.school_rounded), findsOneWidget);
    });

    testWidgets('buildCityMarker returns correct Marker', (
      WidgetTester tester,
    ) async {
      final city = MapCityInsight(
        city: 'City',
        count: 100,
        location: const LatLng(52.0, 5.0),
      );

      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) {
              final marker = InsightsMap.buildCityMarker(context, city, 85.0);
              return SizedBox(width: 100, height: 100, child: marker.child);
            },
          ),
        ),
      );

      expect(find.text('85'), findsOneWidget);
    });

    testWidgets('buildDetailRow displays label and value', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Builder(
            builder: (context) =>
                InsightsMap.buildDetailRow(context, 'Label', 'Value'),
          ),
        ),
      );

      expect(find.text('Label'), findsOneWidget);
      expect(find.text('Value'), findsOneWidget);
    });

    testWidgets('buildCityDetailsSheet displays city info', (
      WidgetTester tester,
    ) async {
      final city = MapCityInsight(
        city: 'Test City',
        count: 100,
        location: const LatLng(0, 0),
        compositeScore: 85.5,
        safetyScore: 90.0,
      );

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) {
                return InsightsMap.buildCityDetailsSheet(context, city);
              },
            ),
          ),
        ),
      );

      expect(find.text('Test City'), findsOneWidget);
      expect(find.text('Data points'), findsOneWidget);
      expect(find.text('100'), findsOneWidget);
      expect(find.text('85.5'), findsOneWidget);
      expect(find.text('90.0'), findsOneWidget);
    });

    testWidgets('buildAmenityDetailsSheet displays amenity info', (
      WidgetTester tester,
    ) async {
      final amenity = MapAmenity(
        id: '1',
        type: 'school',
        name: 'Test School',
        location: const LatLng(0, 0),
        metadata: {'Key': 'Value'},
      );

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) {
                return InsightsMap.buildAmenityDetailsSheet(context, amenity);
              },
            ),
          ),
        ),
      );

      expect(find.text('Test School'), findsOneWidget);
      expect(find.text('SCHOOL'), findsOneWidget);
      expect(find.text('Key: Value'), findsOneWidget);
    });
  });
}
