import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/search_history_service.dart';
import 'package:valora_app/widgets/report/comparison_view.dart';
import 'package:valora_app/widgets/report/score_gauge.dart';
import 'package:valora_app/widgets/report/category_radar.dart';
import 'package:valora_app/widgets/report/ai_insight_card.dart';
import 'package:shared_preferences/shared_preferences.dart';

// Mock ApiService
class _MockApiService extends ApiService {
  final Map<String, ContextReport> reports = {};

  @override
  Future<ContextReport> getContextReport(String input, {int radiusMeters = 1000}) async {
    if (reports.containsKey(input)) {
      return reports[input]!;
    }
    throw Exception('Report not found');
  }
}

void main() {
  late ContextReportProvider provider;
  late _MockApiService apiService;

  ContextReport createReport(String query, double score) {
    return ContextReport(
      location: ContextLocation(
        query: query,
        displayAddress: '$query address',
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
      compositeScore: score,
      categoryScores: {
        'Social': 80,
        'Safety': 90,
      },
      sources: [],
      warnings: [],
    );
  }

  setUp(() {
    SharedPreferences.setMockInitialValues({});
    apiService = _MockApiService();
    apiService.reports['A'] = createReport('A', 80);
    apiService.reports['B'] = createReport('B', 90);

    provider = ContextReportProvider(
      apiService: apiService,
      historyService: SearchHistoryService(),
    );
  });

  Widget createTestWidget() {
    return MaterialApp(
      home: ChangeNotifierProvider<ContextReportProvider>.value(
        value: provider,
        child: const Scaffold(body: ComparisonView()),
      ),
    );
  }

  testWidgets('ComparisonView shows message when empty', (WidgetTester tester) async {
    await tester.pumpWidget(createTestWidget());
    expect(find.text('No reports to compare'), findsOneWidget);
  });

  testWidgets('ComparisonView shows reports when added', (WidgetTester tester) async {
    await provider.addToComparison('A', 1000);
    await provider.addToComparison('B', 1000);

    await tester.pumpWidget(createTestWidget());
    await tester.pumpAndSettle(); // Wait for images/animations

    // Check headers - A address appears in Header, Metrics Table Header, AI Insight title
    expect(find.text('A address'.split(',')[0]), findsWidgets);

    // Check gauges
    expect(find.byType(ScoreGauge), findsNWidgets(2));

    // Check radars
    expect(find.byType(CategoryRadar), findsNWidgets(2));

    // Check table
    expect(find.text('Metrics Comparison'), findsOneWidget);
    expect(find.text('Social'), findsOneWidget); // Category name

    // Check AI cards
    // Scroll to find AI cards
    await tester.drag(find.byType(ListView), const Offset(0, -800));
    await tester.pumpAndSettle();

    // Check AI cards
    expect(find.byType(AiInsightCard), findsWidgets);
  });

  testWidgets('Remove button removes report', (WidgetTester tester) async {
    await provider.addToComparison('A', 1000);
    await provider.addToComparison('B', 1000);

    await tester.pumpWidget(createTestWidget());
    await tester.pumpAndSettle();

    expect(find.byIcon(Icons.close), findsNWidgets(2));

    // Tap remove on first report
    await tester.tap(find.byIcon(Icons.close).first);
    await tester.pumpAndSettle();

    expect(provider.comparisonIds.length, 1);
    expect(find.byType(ScoreGauge), findsOneWidget);
  });
}
