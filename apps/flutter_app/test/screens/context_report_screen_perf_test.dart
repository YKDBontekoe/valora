import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/screens/context_report_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:shared_preferences/shared_preferences.dart';

class _FakeApiService extends ApiService {
  _FakeApiService({required this.location, required this.metrics});
  final ContextLocation location;
  final List<ContextMetric> metrics;

  @override
  Future<ContextLocation?> resolveLocation(String input) async => location;

  @override
  Future<ContextCategoryMetrics> getContextMetrics(String category, ContextLocation location, {int? radiusMeters}) async {
    return ContextCategoryMetrics(metrics: metrics, warnings: [], score: 50.0);
  }
}

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  testWidgets('ContextReportScreen uses progressive loading AND preserves expansion state via Provider', (tester) async {
    tester.view.physicalSize = const Size(400, 600);
    tester.view.devicePixelRatio = 1.0;

    final metrics = [
      ContextMetric(key: 'm1', label: 'SafetyMetric', source: 'S1', value: 10),
    ];

    final location = ContextLocation(
      query: 'Test',
      displayAddress: 'Test Address',
      latitude: 0,
      longitude: 0,
    );

    final apiService = _FakeApiService(location: location, metrics: metrics);

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
    await tester.pump(); // Start resolve
    await tester.pump(); // Resolve complete, start metrics
    await tester.pump(); // Metrics complete

    expect(find.text('Test Address'), findsOneWidget);

    // Find Safety and expand it
    final listFinder = find.byType(ListView);
    await tester.dragUntilVisible(find.text('Safety'), listFinder, const Offset(0, -100));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Safety'));
    await tester.pumpAndSettle();

    // Verify it's expanded
    expect(find.text('SafetyMetric'), findsWidgets);

    // Scroll it way off-screen
    await tester.drag(listFinder, const Offset(0, -2000));
    await tester.pumpAndSettle();

    // Scroll back to top and find Safety
    await tester.dragUntilVisible(find.text('Safety'), listFinder, const Offset(0, 100));
    await tester.pumpAndSettle();

    // Verify expansion state was preserved
    expect(find.text('SafetyMetric'), findsWidgets);

    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });
  });
}
