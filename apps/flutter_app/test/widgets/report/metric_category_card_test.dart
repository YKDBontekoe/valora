import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/report/metric_category_card.dart';
import 'package:valora_app/models/context_report.dart';

void main() {
  final List<ContextMetric> metrics = [
    ContextMetric(key: 'residents', label: 'Residents', source: 'CBS', value: 1000, unit: null, score: 80),
  ];

  testWidgets('MetricCategoryCard shows info icon and opens explanation', (WidgetTester tester) async {
    await tester.pumpWidget(MaterialApp(
      home: Scaffold(
        body: SingleChildScrollView(
          child: MetricCategoryCard(
            title: 'Social',
            icon: Icons.people,
            metrics: metrics,
            score: 85,
            isExpanded: true,
          ),
        ),
      ),
    ));

    // Wait for expansion animation
    await tester.pumpAndSettle();

    expect(find.byIcon(Icons.info_outline_rounded), findsWidgets);

    // Tap the info icon (it's inside an InkWell that covers the label and icon)
    await tester.tap(find.text('Residents'));
    await tester.pumpAndSettle();

    // Check if modal bottom sheet is shown
    expect(find.text('Total number of people living in this area.'), findsOneWidget);
    expect(find.text('Got it'), findsOneWidget);

    // Close modal
    await tester.tap(find.text('Got it'));
    await tester.pumpAndSettle();

    expect(find.text('Total number of people living in this area.'), findsNothing);
  });
}
