import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/screens/context_report_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/widgets/report/metric_category_card.dart';
import 'package:shared_preferences/shared_preferences.dart';

class _FakeApiService extends ApiService {
  _FakeApiService({required this.report});
  final ContextReport report;

  @override
  Future<ContextReport> getContextReport(String input, {int radiusMeters = 1000}) async {
    return report;
  }
}

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  testWidgets('ContextReportScreen uses lazy loading AND preserves expansion state via Provider', (tester) async {
    tester.view.physicalSize = const Size(400, 600);
    tester.view.devicePixelRatio = 1.0;

    final metrics = [
      ContextMetric(key: 'm1', label: 'SafetyMetric', source: 'S1', value: 10),
    ];

    final report = ContextReport(
      location: ContextLocation(
        query: 'Test',
        displayAddress: 'Test Address',
        latitude: 0,
        longitude: 0,
      ),
      socialMetrics: metrics,
      crimeMetrics: metrics,
      demographicsMetrics: metrics,
      housingMetrics: metrics,
      mobilityMetrics: metrics,
      amenityMetrics: metrics,
      environmentMetrics: metrics,
      compositeScore: 50,
      categoryScores: {
        'Social': 50,
        'Safety': 50,
        'Demographics': 50,
        'Housing': 50,
        'Mobility': 50,
        'Amenities': 50,
        'Environment': 50,
      },
      sources: [],
      warnings: [],
    );

    final apiService = _FakeApiService(report: report);

    await tester.pumpWidget(
      MaterialApp(
        home: Provider<ApiService>.value(
          value: apiService,
          child: const ContextReportScreen(),
        ),
      ),
    );

    await tester.enterText(find.byType(TextField), 'Amsterdam');
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await tester.pumpAndSettle();

    expect(find.text('Test Address'), findsOneWidget);

    // 1. Verify Lazy Loading
    expect(find.byType(MetricCategoryCard).evaluate().length, lessThan(7));

    // 2. Find Safety and expand it
    final listFinder = find.byType(ListView);
    await tester.dragUntilVisible(find.text('Safety'), listFinder, const Offset(0, -100));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Safety'));
    await tester.pumpAndSettle();

    // Verify it's expanded
    expect(find.text('SafetyMetric'), findsWidgets);

    // 3. Scroll it way off-screen
    await tester.drag(listFinder, const Offset(0, -2000));
    await tester.pumpAndSettle();
    expect(find.text('Safety'), findsNothing);

    // 4. Scroll back to top and find Safety
    await tester.dragUntilVisible(find.text('Safety'), listFinder, const Offset(0, 100));
    await tester.pumpAndSettle();

    // 5. Verify expansion state was preserved
    expect(find.text('SafetyMetric'), findsWidgets);

    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });
  });
}
