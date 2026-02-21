import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/search_history_service.dart';
import 'package:valora_app/widgets/report/ai_insight_card.dart';
import 'package:valora_app/widgets/common/valora_shimmer.dart';
import 'package:valora_app/widgets/common/valora_button.dart';
import 'package:shared_preferences/shared_preferences.dart';

// Mock Provider using subclass
class MockProvider extends ContextReportProvider {
  MockProvider() : super(apiService: ApiService(), historyService: SearchHistoryService());

  String? _insight;
  String? _error;
  bool _loading = false;

  @override
  String? getAiInsight(String location) => _insight;

  @override
  String? getAiInsightError(String location) => _error;

  @override
  bool isAiInsightLoading(String location) => _loading;

  @override
  Future<void> generateAiInsight(ContextReport report) async {
    // No-op for test interaction verification
  }

  // Helpers to set state
  void setInsight(String? value) { _insight = value; notifyListeners(); }
  void setError(String? value) { _error = value; notifyListeners(); }
  void setLoading(bool value) { _loading = value; notifyListeners(); }
}

void main() {
  late MockProvider provider;

  final report = ContextReport(
    location: ContextLocation(
      query: 'Test',
      displayAddress: 'Test Address',
      latitude: 0,
      longitude: 0,
    ),
    socialMetrics: [],
    crimeMetrics: [],
    demographicsMetrics: [],
    housingMetrics: [],
    mobilityMetrics: [],
    amenityMetrics: [],
    environmentMetrics: [],
    compositeScore: 0,
    categoryScores: {},
    sources: [],
    warnings: [],
  );

  setUp(() {
    SharedPreferences.setMockInitialValues({});
    provider = MockProvider();
  });

  Widget createWidget() {
    return MaterialApp(
      home: ChangeNotifierProvider<ContextReportProvider>.value(
        value: provider,
        child: Scaffold(
          body: AiInsightCard(report: report),
        ),
      ),
    );
  }

  testWidgets('AiInsightCard shows initial state', (tester) async {
    await tester.pumpWidget(createWidget());
    await tester.pumpAndSettle(const Duration(seconds: 1));

    expect(find.text('Unlock Neighborhood Insights'), findsOneWidget);
    expect(find.byType(ValoraButton), findsOneWidget);
  });

  testWidgets('AiInsightCard shows loading state', (tester) async {
    provider.setLoading(true);
    await tester.pumpWidget(createWidget());
    // Shimmer loops, so we cannot use pumpAndSettle
    await tester.pump(const Duration(milliseconds: 100));
    expect(find.byType(ValoraShimmer), findsOneWidget);
  });

  testWidgets('AiInsightCard shows error state', (tester) async {
    provider.setError('Network Error');
    await tester.pumpWidget(createWidget());
    await tester.pumpAndSettle(const Duration(seconds: 1));

    expect(find.text('Failed to generate insight'), findsOneWidget);
    expect(find.text('Retry'), findsOneWidget);
  });

  testWidgets('AiInsightCard shows insight content', (tester) async {
    provider.setInsight('This area is **great**.');
    await tester.pumpWidget(createWidget());
    await tester.pumpAndSettle(const Duration(seconds: 1));

    expect(find.text('AI Insight'), findsOneWidget);

    // RichText parsing check
    final richTextFinder = find.byKey(const Key('ai-summary-text'));
    expect(richTextFinder, findsOneWidget);
  });
}
