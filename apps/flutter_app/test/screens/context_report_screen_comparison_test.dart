import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/screens/context_report_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/pdok_service.dart';
import 'package:valora_app/services/search_history_service.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/widgets/report/comparison_view.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/models/search_history_item.dart';

// Mocks
class MockApiService extends ApiService {
  final Map<String, ContextReport> reports = {};

  @override
  Future<ContextReport> getContextReport(String input, {int radiusMeters = 1000}) async {
    if (reports.containsKey(input)) {
      return reports[input]!;
    }
    // Return a default report for any other input to allow initial load
    return _createReport(input, 80);
  }
}

class MockPdokService extends PdokService {}

class MockHistoryService extends SearchHistoryService {
  @override
  Future<List<SearchHistoryItem>> getHistory() async => [];
  @override
  Future<void> addToHistory(String query) async {}
}

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
  final MockApiService apiService;
  final MockPdokService pdokService;

  const _TestScaffoldHost({
    required this.apiService,
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
        Provider<ApiService>.value(value: widget.apiService),
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
  late MockApiService apiService;
  late MockPdokService pdokService;

  setUp(() {
    SharedPreferences.setMockInitialValues({});
    apiService = MockApiService();
    pdokService = MockPdokService();

    apiService.reports['A'] = _createReport('A', 80);
    apiService.reports['B'] = _createReport('B', 90);
  });

  Widget createWidget() {
    return MaterialApp(
      home: _TestScaffoldHost(
        apiService: apiService,
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
    // Look for playlist_add icon
    final addToCompareIcon = find.byIcon(Icons.playlist_add_rounded);
    expect(addToCompareIcon, findsOneWidget);

    await tester.tap(addToCompareIcon);
    await tester.pumpAndSettle();

    // Icon should change to check
    expect(find.byIcon(Icons.playlist_add_check_rounded), findsOneWidget);

    // 3. FAB should appear (in the host Scaffold)
    final fabFinder = find.byType(FloatingActionButton);
    expect(fabFinder, findsOneWidget);
    expect(find.text('Compare (1)'), findsOneWidget);

    // 4. Click FAB to go to comparison view
    await tester.tap(fabFinder);
    await tester.pumpAndSettle();

    // Verify we are in comparison view
    expect(find.byType(ComparisonView), findsOneWidget);
    expect(find.text('Compare Properties'), findsOneWidget); // AppBar title

    // 5. Verify exit comparison mode
    final backButton = find.byIcon(Icons.arrow_back_rounded);
    expect(backButton, findsOneWidget);

    await tester.tap(backButton);
    await tester.pumpAndSettle();

    expect(find.byType(ComparisonView), findsNothing);
    // After exiting comparison, the report layout is shown (report is still loaded)
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

    // Verify clear button exists in comparison mode
    final clearButton = find.byIcon(Icons.delete_sweep_rounded);
    expect(clearButton, findsOneWidget);

    // Click clear
    await tester.tap(clearButton);
    await tester.pumpAndSettle();

    // Should verify clear happened - UI might just empty the list
    expect(find.text('No reports to compare'), findsOneWidget);
  });
}
