import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/widgets/report/comparison_view.dart';
import 'package:mockito/mockito.dart';

import '../../screens/context_report_screen_test.mocks.dart';

void main() {
  testWidgets('ComparisonView renders empty state when no ids to compare', (tester) async {
    final mockRepo = MockContextReportRepository();
    final provider = ContextReportProvider(repository: mockRepo);

    await tester.pumpWidget(MaterialApp(
      home: ChangeNotifierProvider<ContextReportProvider>.value(
        value: provider,
        child: const Scaffold(body: ComparisonView()),
      ),
    ));

    expect(find.text('No reports to compare'), findsOneWidget);
  });

  testWidgets('ComparisonView renders table with scroll view and correct width columns', (tester) async {
    final mockRepo = MockContextReportRepository();
    final provider = ContextReportProvider(repository: mockRepo);

    final report1 = ContextReport(
      location: ContextLocation(query: 'test1', displayAddress: 'Test 1', latitude: 0, longitude: 0),
      socialMetrics: [], crimeMetrics: [], demographicsMetrics: [], housingMetrics: [],
      mobilityMetrics: [], amenityMetrics: [], environmentMetrics: [],
      compositeScore: 50, categoryScores: {'Safety': 50, 'Social': 80}, sources: [], warnings: [],
    );
    final report2 = ContextReport(
      location: ContextLocation(query: 'test2', displayAddress: 'Test 2', latitude: 0, longitude: 0),
      socialMetrics: [], crimeMetrics: [], demographicsMetrics: [], housingMetrics: [],
      mobilityMetrics: [], amenityMetrics: [], environmentMetrics: [],
      compositeScore: 60, categoryScores: {'Safety': 60}, sources: [], warnings: [],
    );
    final report3 = ContextReport(
      location: ContextLocation(query: 'test3', displayAddress: 'Test 3', latitude: 0, longitude: 0),
      socialMetrics: [], crimeMetrics: [], demographicsMetrics: [], housingMetrics: [],
      mobilityMetrics: [], amenityMetrics: [], environmentMetrics: [],
      compositeScore: 0, categoryScores: {'Safety': 0.0, 'Social': 0.0}, sources: [], warnings: [],
    );

    // Mock getting reports
    when(mockRepo.getContextReport('test1', radiusMeters: 1000)).thenAnswer((_) async => report1);
    when(mockRepo.getContextReport('test2', radiusMeters: 1000)).thenAnswer((_) async => report2);
    when(mockRepo.getContextReport('test3', radiusMeters: 1000)).thenAnswer((_) async => report3);

    await provider.addToComparison('test1', 1000);
    await provider.addToComparison('test2', 1000);
    await provider.addToComparison('test3', 1000);

    // Give time for fetch to complete
    await tester.pumpWidget(MaterialApp(
      home: ChangeNotifierProvider<ContextReportProvider>.value(
        value: provider,
        child: const Scaffold(body: ComparisonView()),
      ),
    ));
    await tester.pumpAndSettle();

    // Verify reports are rendered
    expect(find.text('Test 1'), findsWidgets);
    expect(find.text('Test 2'), findsWidgets);
    expect(find.text('Test 3'), findsWidgets);

    // Find the Table
    final tableFinder = find.byType(Table);
    expect(tableFinder, findsOneWidget);
    final Table table = tester.widget(tableFinder);

    // Test the widths we added
    expect(table.columnWidths![0], const FixedColumnWidth(100));
    expect(table.columnWidths![1], const FixedColumnWidth(120));
    expect(table.columnWidths![2], const FixedColumnWidth(120));
    expect(table.columnWidths![3], const FixedColumnWidth(120));

    // Find the SingleChildScrollView
    expect(find.ancestor(of: tableFinder, matching: find.byType(SingleChildScrollView)), findsWidgets);

    // Check null handling (report2 is missing Social score, should render "—")
    // Note: Test 1 has Social 80, Test 2 has missing Social.
    expect(find.text('—'), findsOneWidget);

    // Check zero handling (report3 has 0.0 for Safety and Social, should render "0.0")
    // Test 3 has 0.0 for Safety and Social. Note: ScoreGauge for Test 3 also has 0, so '0.0' or '0' might exist. The table uses toStringAsFixed(1)
    expect(find.text('0.0'), findsWidgets);
  });
}
