import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/report/category_radar.dart';

void main() {
  testWidgets('CategoryRadar renders empty state', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: CategoryRadar(categoryScores: {}),
        ),
      ),
    );

    expect(find.text('No data'), findsOneWidget);
  });

  testWidgets('CategoryRadar handles animation', (tester) async {
    final scores = {'A': 50.0};

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: CategoryRadar(
            categoryScores: scores,
            animationDuration: const Duration(milliseconds: 100),
          ),
        ),
      ),
    );

    await tester.pump(); // Start
    await tester.pump(const Duration(milliseconds: 50)); // Middle
    await tester.pump(const Duration(milliseconds: 50)); // End

    // Check CustomPaint exists
    expect(find.byType(CustomPaint), findsAtLeastNWidgets(1));
  });

  testWidgets('CategoryRadar paints chart with data', (tester) async {
    final scores = {'A': 50.0, 'B': 100.0, 'C': 0.0};

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: CategoryRadar(
            categoryScores: scores,
            animationDuration: Duration.zero,
          ),
        ),
      ),
    );

    await tester.pump();

    expect(find.byType(CustomPaint), findsAtLeastNWidgets(1));
  });
}
