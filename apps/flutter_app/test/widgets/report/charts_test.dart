import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/widgets/report/charts/context_bar_chart.dart';
import 'package:valora_app/widgets/report/charts/context_pie_chart.dart';
import 'package:valora_app/widgets/report/charts/proximity_chart.dart';

void main() {
  group('ContextBarChart Tests', () {
    testWidgets('renders bars for metrics', (tester) async {
      final metrics = [
        ContextMetric(key: 'age_0_15', label: '0-15', source: 'CBS', value: 100),
        ContextMetric(key: 'age_15_25', label: '15-25', source: 'CBS', value: 200),
      ];

      await tester.pumpWidget(MaterialApp(
        home: Scaffold(body: ContextBarChart(metrics: metrics)),
      ));
      await tester.pump(const Duration(seconds: 1));

      expect(find.text('100'), findsOneWidget);
      expect(find.text('200'), findsOneWidget);
    });
  });

  group('ContextPieChart Tests', () {
    testWidgets('renders slices and legend', (tester) async {
      final metrics = [
        ContextMetric(key: 'housing_owner', label: 'Owner', source: 'CBS', value: 60),
        ContextMetric(key: 'housing_rental', label: 'Rental', source: 'CBS', value: 40),
      ];

      await tester.pumpWidget(MaterialApp(
        home: Scaffold(body: ContextPieChart(metrics: metrics)),
      ));
      await tester.pump(const Duration(seconds: 1));

      expect(find.text('Owner'), findsOneWidget);
      expect(find.text('Rental'), findsOneWidget);
    });
  });

  group('ProximityChart Tests', () {
    testWidgets('renders progress bars and distances', (tester) async {
      final metrics = [
        ContextMetric(key: 'dist_school', label: 'School', source: 'PDOK', value: 0.5),
      ];

      await tester.pumpWidget(MaterialApp(
        home: Scaffold(body: ProximityChart(metrics: metrics)),
      ));
      await tester.pump(const Duration(seconds: 1));

      expect(find.text('School'), findsOneWidget);
      expect(find.text('0.5 km'), findsOneWidget);
    });
  });
}
