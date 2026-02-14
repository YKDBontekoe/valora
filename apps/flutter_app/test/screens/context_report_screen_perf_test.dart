import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/screens/context_report_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/providers/user_profile_provider.dart';
import 'package:valora_app/widgets/report/metric_category_card.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../mocks/mock_user_profile_provider.dart';

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
    // V3 of the test to avoid confusion
    tester.view.physicalSize = const Size(400, 800);
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
      MultiProvider(
        providers: [
          Provider<ApiService>.value(value: apiService),
          ChangeNotifierProvider<UserProfileProvider>.value(value: MockUserProfileProvider()),
        ],
        child: const MaterialApp(
          home: ContextReportScreen(),
        ),
      ),
    );

    await tester.pumpAndSettle();

    final textField = find.byType(TextField);
    await tester.enterText(textField, 'Amsterdam');
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await tester.pumpAndSettle();

    expect(find.text('Test Address'), findsOneWidget);

    final listFinder = find.byType(ListView);

    // Find 'Safety' section
    final safetyFinder = find.text('Safety');
    await tester.dragUntilVisible(safetyFinder, listFinder, const Offset(0, -200));
    await tester.pumpAndSettle();

    // Expand
    await tester.tap(safetyFinder);
    await tester.pumpAndSettle();

    // Check expanded content
    expect(find.text('SafetyMetric'), findsWidgets);

    // Scroll away (down)
    await tester.drag(listFinder, const Offset(0, -3000));
    await tester.pumpAndSettle();

    // Scroll back (up)
    await tester.dragUntilVisible(safetyFinder, listFinder, const Offset(0, 500));
    await tester.pumpAndSettle();

    // Verify expansion state was preserved
    expect(find.text('SafetyMetric'), findsWidgets);

    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });
  });
}
