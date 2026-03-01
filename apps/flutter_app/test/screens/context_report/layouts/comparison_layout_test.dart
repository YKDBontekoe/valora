import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/screens/context_report/layouts/comparison_layout.dart';
import 'package:valora_app/widgets/valora_widgets.dart';
import 'package:mockito/mockito.dart';

import '../../context_report_screen_test.mocks.dart';

void main() {
  testWidgets('ComparisonLayout shows ValoraDialog on clear all and respects cancel', (tester) async {
    final mockRepo = MockContextReportRepository();
    final provider = ContextReportProvider(repository: mockRepo);

    // Setup some comparison data to show the view
    final report = ContextReport(
      location: ContextLocation(query: 'test', displayAddress: 'Test 1', latitude: 0, longitude: 0),
      socialMetrics: [], crimeMetrics: [], demographicsMetrics: [], housingMetrics: [],
      mobilityMetrics: [], amenityMetrics: [], environmentMetrics: [],
      compositeScore: 50, categoryScores: {'Safety': 50}, sources: [], warnings: [],
    );

    when(mockRepo.getContextReport(any, radiusMeters: anyNamed('radiusMeters')))
        .thenAnswer((_) async => report);

    bool backCalled = false;
    bool clearCalled = false;

    await tester.pumpWidget(MaterialApp(
      home: ChangeNotifierProvider<ContextReportProvider>.value(
        value: provider,
        child: ComparisonLayout(
          onBack: () => backCalled = true,
          onClear: () => clearCalled = true,
        ),
      ),
    ));

    // Wait for the UI to settle
    await tester.pumpAndSettle();

    // Tap the clear button
    await tester.tap(find.byIcon(Icons.delete_sweep_rounded));
    await tester.pumpAndSettle();

    // Verify dialog shows
    expect(find.byType(ValoraDialog), findsOneWidget);
    expect(find.text('Clear Comparison?'), findsOneWidget);

    // Tap cancel
    await tester.tap(find.text('Cancel'));
    await tester.pumpAndSettle();

    expect(find.byType(ValoraDialog), findsNothing);
    expect(clearCalled, isFalse);
    expect(backCalled, isFalse);

    // Tap clear button again
    await tester.tap(find.byIcon(Icons.delete_sweep_rounded));
    await tester.pumpAndSettle();

    // Tap clear all
    await tester.tap(find.text('Clear All'));
    await tester.pumpAndSettle();

    expect(find.byType(ValoraDialog), findsNothing);
    expect(clearCalled, isTrue);
    expect(backCalled, isTrue);
  });
}
