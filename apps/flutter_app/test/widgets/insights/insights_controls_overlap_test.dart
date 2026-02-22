import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/insights_provider.dart';
import 'package:valora_app/widgets/insights/insights_controls.dart';
import 'package:valora_app/models/map_city_insight.dart';
import 'package:latlong2/latlong.dart';
import 'package:mockito/mockito.dart';

class MockInsightsProvider extends ChangeNotifier implements InsightsProvider {
  @override
  bool showOverlays = false;
  @override
  bool showAmenities = false;
  @override
  Object? selectedFeature;
  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  testWidgets('InsightsControls moves up when selectedFeature is present', (WidgetTester tester) async {
    final provider = MockInsightsProvider();

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<InsightsProvider>.value(
          value: provider,
          child: Stack(
            children: [
              InsightsControls(
                onZoomIn: () {},
                onZoomOut: () {},
                onMapChanged: () {},
              ),
            ],
          ),
        ),
      ),
    );

    // Initial state: transform should be zero (or near zero depending on default state)
    // We check the RenderObject of the AnimatedContainer
    final containerFinder = find.byType(AnimatedContainer);
    final container = tester.widget<AnimatedContainer>(containerFinder);

    // Matrix4.identity() is expected initially
    expect(container.transform, equals(Matrix4.translationValues(0, 0, 0)));

    // Update state to simulate selection
    provider.selectedFeature = MapCityInsight(city: 'Test', count: 10, location: const LatLng(0,0));
    provider.notifyListeners();
    await tester.pump(); // Start animation
    await tester.pumpAndSettle(); // Finish animation

    final updatedContainer = tester.widget<AnimatedContainer>(containerFinder);

    // Should have moved up by -220
    expect(updatedContainer.transform, equals(Matrix4.translationValues(0, -220, 0)));
  });
}
