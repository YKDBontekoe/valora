import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/widgets/report/metric_category_card.dart';
import 'package:valora_app/widgets/report/charts/context_bar_chart.dart';

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
    await tester.pumpAndSettle();

    expect(find.byType(ContextBarChart), findsOneWidget);
  });

  testWidgets('MetricCategoryCard expansion toggle didUpdateWidget coverage', (tester) async {
    bool isExpanded = false;

    await tester.pumpWidget(StatefulBuilder(
      builder: (context, setState) {
        return MaterialApp(
          home: Scaffold(
            body: MetricCategoryCard(
              title: 'Test',
              icon: Icons.person,
              metrics: [],
              score: 80,
              isExpanded: isExpanded,
            ),
          ),
        );
      }
    ));

    expect(tester.widget<SizeTransition>(find.byType(SizeTransition)).sizeFactor.value, 0.0);

    // Update parent state
    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: MetricCategoryCard(
          title: 'Test',
          icon: Icons.person,
          metrics: [],
          score: 80,
          isExpanded: true,
        ),
      ),
    ));
    await tester.pumpAndSettle();

    expect(tester.widget<SizeTransition>(find.byType(SizeTransition)).sizeFactor.value, 1.0);
  });

  testWidgets('MetricCategoryCard icon mapping covers various keys', (tester) async {
    final testKeys = [
      'residents', 'population_density', 'low_income_households', 'average_woz',
      'income_per_recipient', 'urbanity', 'education_low', 'gender_men',
      'households_with_children', 'single_households', 'age_0_14', 'age_65_plus',
      'housing_owner', 'housing_social', 'housing_multifamily', 'housing_pre2000',
      'mobility_cars_household', 'mobility_car_density', 'dist_supermarket',
      'dist_gp', 'dist_school', 'dist_daycare', 'unknown_key'
    ];

    for (final key in testKeys) {
      await tester.pumpWidget(MaterialApp(
        home: Scaffold(
          body: MetricCategoryCard(
            title: 'Test',
            icon: Icons.person,
            metrics: [ContextMetric(key: key, label: 'Label', source: 'S', value: 1)],
            score: 80,
            isExpanded: true,
          ),
        ),
      ));
      await tester.pump();
    }
  });

  testWidgets('MetricRow handles note-only metrics', (tester) async {
    final metric = ContextMetric(
      key: 'test',
      label: 'Note Metric',
      source: 'S',
      note: 'Very long note',
    );

    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: MetricCategoryCard(
          title: 'Test',
          icon: Icons.person,
          metrics: [metric],
          score: 80,
          isExpanded: true,
        ),
      ),
    ));
    await tester.pump();

    expect(find.text('Note Metric'), findsOneWidget);
    expect(find.text('Very long note'), findsOneWidget);
  });
}
