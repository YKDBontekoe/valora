import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/widgets/report/metric_category_card.dart';

void main() {
  testWidgets('MetricRow icon selection coverage', (tester) async {
    final keys = [
      'residents', 'population_density', 'low_income_households', 'average_woz',
      'income_per_recipient', 'income_per_inhabitant', 'avg_income_recipient', 'avg_income_inhabitant',
      'urbanity', 'education_low', 'education_medium', 'education_high',
      'gender_men', 'gender_women', 'households_with_children', 'households_without_children',
      'single_households', 'age_0_14', 'age_0_15', 'age_15_24', 'age_15_25',
      'age_25_44', 'age_25_45', 'age_45_64', 'age_45_65', 'age_65_plus',
      'housing_owner', 'housing_rental', 'housing_social', 'housing_multifamily',
      'housing_pre2000', 'housing_post2000', 'mobility_cars_household', 'mobility_total_cars',
      'mobility_car_density', 'dist_supermarket', 'dist_gp', 'dist_school', 'schools_3km',
      'dist_daycare', 'unknown'
    ];

    for (final key in keys) {
      await tester.pumpWidget(MaterialApp(
        home: Scaffold(
          body: MetricCategoryCard(
            title: 'Test',
            icon: Icons.person,
            metrics: [ContextMetric(key: key, label: 'L', source: 'S', value: 1)],
            score: 80,
            isExpanded: true,
          ),
        ),
      ));
      await tester.pump();
    }
  });

  testWidgets('MetricRow displayValue formatting coverage', (tester) async {
    final testCases = [
      ContextMetric(key: 'a', label: 'L', source: 'S', value: 10, unit: 'm'),
      ContextMetric(key: 'b', label: 'L', source: 'S', value: 10.5, unit: 'km'),
      ContextMetric(key: 'c', label: 'L', source: 'S', note: 'Some Note'),
      ContextMetric(key: 'd', label: 'L', source: 'S'), // No value or note
    ];

    for (final metric in testCases) {
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

      if (metric.value != null) {
        expect(find.textContaining(metric.unit!), findsOneWidget);
      } else if (metric.note != null) {
        expect(find.text(metric.note!), findsOneWidget);
      } else {
        expect(find.text('—'), findsOneWidget);
      }
    }
  });

  testWidgets('ScoreBadge color ranges', (tester) async {
    final scores = [90.0, 70.0, 50.0, 30.0];
    for (final score in scores) {
       await tester.pumpWidget(MaterialApp(
        home: Scaffold(
          body: MetricCategoryCard(
            title: 'Test',
            icon: Icons.person,
            metrics: [ContextMetric(key: 'a', label: 'L', source: 'S', value: 1, score: score)],
            score: 80,
            isExpanded: true,
          ),
        ),
      ));
      await tester.pump();
      expect(find.text(score.round().toString()), findsOneWidget);
    }
  });
}
