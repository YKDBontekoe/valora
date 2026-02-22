import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/repositories/context_report_repository.dart';
import 'package:valora_app/repositories/ai_repository.dart';
import 'package:valora_app/services/search_history_service.dart';
import 'package:valora_app/widgets/report/comparison_view.dart';
import 'package:valora_app/widgets/report/score_gauge.dart';
import 'package:valora_app/widgets/report/category_radar.dart';
import 'package:valora_app/widgets/report/ai_insight_card.dart';
import 'package:shared_preferences/shared_preferences.dart';

// Mock ContextReportRepository
class _MockContextReportRepository extends Fake implements ContextReportRepository {
  final Map<String, ContextReport> reports = {};
  bool delayFetch = false;

  @override
  Future<ContextReport> getContextReport(String input, {int radiusMeters = 1000}) async {
    if (reports.containsKey(input)) {
      return reports[input]!;
    }
    throw Exception('Report not found');
  }
}

// Mock AiRepository
class _MockAiRepository extends Fake implements AiRepository {
  @override
  Future<String> getAiAnalysis(ContextReport report) async {
    return "Mock AI Analysis";
  }
}

// Mock SearchHistoryService
class _MockHistoryService extends SearchHistoryService {
  @override
  Future<void> addToHistory(String query) async {}

  @override
  Future<void> clearHistory() async {}
}

void main() {
  late ContextReportProvider provider;
  late _MockContextReportRepository contextReportRepository;

  ContextReport createReport(String query, double score, {Map<String, double>? categories}) {
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
      categoryScores: categories ?? {
        'Social': 80,
        'Safety': 90,
      },
      sources: [],
      warnings: [],
    );
  }

  setUp(() {
    SharedPreferences.setMockInitialValues({});
    contextReportRepository = _MockContextReportRepository();
    contextReportRepository.reports['A'] = createReport('A', 80);
    contextReportRepository.reports['B'] = createReport('B', 90);

    provider = ContextReportProvider(
      contextReportRepository: contextReportRepository,
      aiRepository: _MockAiRepository(),
      historyService: _MockHistoryService(),
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

    // Check headers - using substring match logic or exact match
    expect(find.text('A address'.split(',')[0]), findsWidgets);

    // Check gauges
    expect(find.byType(ScoreGauge), findsNWidgets(2));

    // Check radars
    expect(find.byType(CategoryRadar), findsNWidgets(2));

    // Check table
    expect(find.text('Metrics Comparison'), findsOneWidget);
    expect(find.text('Social'), findsOneWidget); // Category name

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

  testWidgets('ComparisonView handles different categories', (WidgetTester tester) async {
    contextReportRepository.reports['C'] = createReport('C', 70, categories: {'Social': 60, 'Environment': 80});
    await provider.addToComparison('A', 1000); // Has Social, Safety
    await provider.addToComparison('C', 1000); // Has Social, Environment

    await tester.pumpWidget(createTestWidget());
    await tester.pumpAndSettle();

    // Table should show union of categories
    expect(find.text('Social'), findsOneWidget);
    expect(find.text('Safety'), findsOneWidget);
    expect(find.text('Environment'), findsOneWidget);

    // Should show scores
    expect(find.text('80.0'), findsWidgets); // A Social
    expect(find.text('60.0'), findsWidgets); // C Social
  });
}
