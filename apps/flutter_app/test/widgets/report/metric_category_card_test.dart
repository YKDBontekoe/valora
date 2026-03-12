import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/report/metric_category_card.dart';
import 'package:valora_app/models/context_report.dart';

void main() {
  testWidgets('MetricCategoryCard renders', (tester) async {
    bool isExpanded = true;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: MetricCategoryCard(
            title: 'Social',
            icon: Icons.people,
            metrics: [
              ContextMetric(key: 'pop', label: 'Population', source: 'CBS', value: 1000, score: 50, note: ''),
            ],
            score: 50,
            accentColor: Colors.blue,
            isExpanded: isExpanded,
            onToggle: (val) {
              isExpanded = val;
            },
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Social'), findsOneWidget);
    expect(find.text('Population'), findsOneWidget);

    // Tap to collapse
    await tester.tap(find.text('Social'));
    await tester.pumpAndSettle();
    expect(isExpanded, false);
  });
}
