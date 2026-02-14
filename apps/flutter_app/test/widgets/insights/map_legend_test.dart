import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/insights/map_legend.dart';
import 'package:valora_app/models/map_overlay.dart';

void main() {
  testWidgets('MapLegend displays correct labels for PricePerSquareMeter', (WidgetTester tester) async {
    await tester.pumpWidget(const MaterialApp(
      home: Scaffold(
        body: MapLegend(metric: MapOverlayMetric.pricePerSquareMeter),
      ),
    ));

    expect(find.text('Price / m²'), findsOneWidget);
    expect(find.text('<3k'), findsOneWidget);
    expect(find.text('>6k'), findsOneWidget);
  });

  testWidgets('MapLegend displays correct labels for CrimeRate', (WidgetTester tester) async {
    await tester.pumpWidget(const MaterialApp(
      home: Scaffold(
        body: MapLegend(metric: MapOverlayMetric.crimeRate),
      ),
    ));

    expect(find.text('Crime Rate'), findsOneWidget);
    expect(find.text('Low'), findsOneWidget);
    expect(find.text('Very High'), findsOneWidget);
  });

  testWidgets('MapLegend displays correct labels for PopulationDensity', (WidgetTester tester) async {
    await tester.pumpWidget(const MaterialApp(
      home: Scaffold(
        body: MapLegend(metric: MapOverlayMetric.populationDensity),
      ),
    ));

    expect(find.text('Pop. Density'), findsOneWidget);
    expect(find.text('Low'), findsOneWidget);
    expect(find.text('High'), findsOneWidget);
  });
}
