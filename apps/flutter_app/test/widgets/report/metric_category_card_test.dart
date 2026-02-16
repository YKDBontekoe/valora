import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/widgets/report/metric_category_card.dart';
import 'package:valora_app/widgets/report/charts/context_bar_chart.dart';
import 'package:valora_app/widgets/report/charts/context_pie_chart.dart';
import 'package:valora_app/widgets/report/charts/proximity_chart.dart';

void main() {
  testWidgets('MetricCategoryCard builds BarChart for Demographics', (tester) async {
    final metrics = [
      ContextMetric(key: 'age_0_15', label: '0-15', source: 'S', value: 10),
    ];

    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: MetricCategoryCard(
          title: 'Demographics',
          icon: Icons.person,
          metrics: metrics,
          score: 80,
          isExpanded: true,
        ),
      ),
    ));
    await tester.pump(const Duration(seconds: 1));

    expect(find.byType(ContextBarChart), findsOneWidget);
  });

  testWidgets('MetricCategoryCard builds PieChart for Housing', (tester) async {
    final metrics = [
      ContextMetric(key: 'housing_owner', label: 'Owner', source: 'S', value: 10),
    ];

    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: MetricCategoryCard(
          title: 'Housing',
          icon: Icons.home,
          metrics: metrics,
          score: 80,
          isExpanded: true,
        ),
      ),
    ));
    await tester.pump(const Duration(seconds: 1));

    expect(find.byType(ContextPieChart), findsOneWidget);
  });

  testWidgets('MetricCategoryCard builds ProximityChart for Amenities', (tester) async {
    final metrics = [
      ContextMetric(key: 'dist_school', label: 'School', source: 'S', value: 10),
    ];

    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: MetricCategoryCard(
          title: 'Amenities',
          icon: Icons.store,
          metrics: metrics,
          score: 80,
          isExpanded: true,
        ),
      ),
    ));
    await tester.pump(const Duration(seconds: 1));

    expect(find.byType(ProximityChart), findsOneWidget);
  });
}
