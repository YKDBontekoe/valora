import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/screens/context_report_screen.dart';
import 'package:valora_app/repositories/context_report_repository.dart';
import 'package:valora_app/repositories/ai_repository.dart';
import 'package:valora_app/services/pdok_service.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/widgets/report/comparison_view.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:mockito/mockito.dart';

// Mocks
class MockContextReportRepository extends Fake implements ContextReportRepository {
  final Map<String, ContextReport> reports = {};

  @override
  Future<ContextReport> getContextReport(String input, {int radiusMeters = 1000}) async {
    if (reports.containsKey(input)) {
      return reports[input]!;
    }
    return _createReport(input, 80);
  }
}

class MockAiRepository extends Fake implements AiRepository {
  @override
  Future<String> getAiAnalysis(ContextReport report) async => "Analysis";
}

class MockPdokService extends PdokService {}

ContextReport _createReport(String query, double score) {
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
    categoryScores: {},
    sources: [],
    warnings: [],
  );
}

/// A test host that wraps ContextReportScreen in a Scaffold to capture the FAB.
class _TestScaffoldHost extends StatefulWidget {
  final MockContextReportRepository contextReportRepository;
  final MockAiRepository aiRepository;
  final MockPdokService pdokService;

  const _TestScaffoldHost({
    required this.contextReportRepository,
    required this.aiRepository,
    required this.pdokService,
  });

  @override
  State<_TestScaffoldHost> createState() => _TestScaffoldHostState();
}

class _TestScaffoldHostState extends State<_TestScaffoldHost> {
  Widget? _fab;

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        Provider<ContextReportRepository>.value(value: widget.contextReportRepository),
        Provider<AiRepository>.value(value: widget.aiRepository),
      ],
      child: Scaffold(
        floatingActionButton: _fab,
        body: ContextReportScreen(
          pdokService: widget.pdokService,
          onFabChanged: (fab) {
            if (mounted) setState(() => _fab = fab);
          },
        ),
      ),
    );
  }
}

void main() {
  late MockContextReportRepository contextReportRepository;
  late MockAiRepository aiRepository;
  late MockPdokService pdokService;

  setUp(() {
    SharedPreferences.setMockInitialValues({});
    contextReportRepository = MockContextReportRepository();
    aiRepository = MockAiRepository();
    pdokService = MockPdokService();

    contextReportRepository.reports['A'] = _createReport('A', 80);
    contextReportRepository.reports['B'] = _createReport('B', 90);
  });

  Widget createWidget() {
    return MaterialApp(
      home: _TestScaffoldHost(
        contextReportRepository: contextReportRepository,
        aiRepository: aiRepository,
        pdokService: pdokService,
      ),
    );
  }

  testWidgets('ContextReportScreen comparison flow', (tester) async {
    await tester.pumpWidget(createWidget());
    await tester.pumpAndSettle();

    // 1. Search and generate a report
    final inputFinder = find.byType(TextField);
    await tester.enterText(inputFinder, 'A');
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await tester.pumpAndSettle();

    // Verify report is shown
    expect(find.text('A address'), findsWidgets);

    // 2. Add to comparison via AppBar icon
    final addToCompareIcon = find.byIcon(Icons.playlist_add_rounded);
    expect(addToCompareIcon, findsOneWidget);

    await tester.tap(addToCompareIcon);
    await tester.pumpAndSettle();

    // Icon should change to check
    expect(find.byIcon(Icons.playlist_add_check_rounded), findsOneWidget);

    // 3. FAB should appear (in the host Scaffold)
    final fabFinder = find.byType(FloatingActionButton);
    expect(fabFinder, findsOneWidget);
    expect(find.textContaining('Compare (1)'), findsOneWidget);

    // 4. Click FAB to go to comparison view
    await tester.tap(fabFinder);
    await tester.pumpAndSettle();

    // Verify we are in comparison view
    expect(find.byType(ComparisonView), findsOneWidget);
    expect(find.text('Compare Properties'), findsOneWidget);

    // 5. Verify exit comparison mode
    final backButton = find.byIcon(Icons.arrow_back_rounded);
    expect(backButton, findsOneWidget);

    await tester.tap(backButton);
    await tester.pumpAndSettle();

    expect(find.byType(ComparisonView), findsNothing);
    expect(find.text('A address'), findsWidgets);
  });

  testWidgets('ContextReportScreen clear comparison', (tester) async {
    await tester.pumpWidget(createWidget());
    await tester.pumpAndSettle();

    // Search and add A
    final inputFinder = find.byType(TextField);
    await tester.enterText(inputFinder, 'A');
    await tester.testTextInput.receiveAction(TextInputAction.search);
    await tester.pumpAndSettle();

    // Add to compare
    await tester.tap(find.byIcon(Icons.playlist_add_rounded));
    await tester.pumpAndSettle();

    // Go to comparison
    await tester.tap(find.byType(FloatingActionButton));
    await tester.pumpAndSettle();

    // Verify clear button exists
    final clearButton = find.byIcon(Icons.delete_sweep_rounded);
    expect(clearButton, findsOneWidget);

    // Click clear
    await tester.tap(clearButton);
    await tester.pumpAndSettle();

    // Verify comparison is empty
    expect(find.text('No reports to compare'), findsOneWidget);
  });
}