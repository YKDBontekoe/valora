import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/widgets/report/context_report_view.dart';
import 'package:valora_app/widgets/report/report_widgets.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:mockito/mockito.dart';

class MockApiService extends Mock implements ApiService {}

void main() {
  final testReport = ContextReport(
    location: ContextLocation(
      query: 'Test Query',
      displayAddress: 'Test Address',
      latitude: 52.37,
      longitude: 4.89,
      neighborhoodName: 'Test Neighborhood',
      municipalityName: 'Test Municipality',
    ),
    socialMetrics: [
      ContextMetric(key: 'residents', label: 'Residents', source: 'CBS', value: 1000),
    ],
    crimeMetrics: [
      ContextMetric(key: 'theft', label: 'Theft', source: 'Police', value: 5),
    ],
    demographicsMetrics: [],
    housingMetrics: [],
    mobilityMetrics: [],
    amenityMetrics: [],
    environmentMetrics: [],
    compositeScore: 85.0,
    categoryScores: {'Social': 80.0, 'Safety': 90.0},
    sources: [
      SourceAttribution(source: 'CBS', url: 'https://cbs.nl', license: 'Open Data'),
    ],
    warnings: ['Test Warning'],
  );

  Widget createWidgetUnderTest(Widget child) {
    return MaterialApp(
      home: ChangeNotifierProvider(
        create: (_) => ContextReportProvider(apiService: MockApiService()),
        child: Scaffold(body: child),
      ),
    );
  }

  testWidgets('ContextReportView renders correctly', (tester) async {
    tester.view.physicalSize = const Size(1200, 2000);
    tester.view.devicePixelRatio = 1.0;

    await tester.pumpWidget(createWidgetUnderTest(
      ListView(
        children: [
          ContextReportView(report: testReport),
        ],
      ),
    ));

    expect(find.text('Test Address'), findsOneWidget);
    expect(find.text('Social'), findsOneWidget);

    await tester.pumpAndSettle();

    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });
  });

  testWidgets('ContextReportView.buildChild returns expected components', (tester) async {
    tester.view.physicalSize = const Size(1200, 2000);
    tester.view.devicePixelRatio = 1.0;

    await tester.pumpWidget(createWidgetUnderTest(
      Builder(
        builder: (context) {
          final count = ContextReportView.childCount(testReport);
          return ListView.builder(
            itemCount: count,
            itemBuilder: (context, index) => ContextReportView.buildChild(
              context,
              index,
              testReport,
            ),
          );
        },
      ),
    ));

    expect(find.text('Test Address'), findsOneWidget);
    expect(find.byType(ScoreGauge), findsOneWidget);
    expect(find.byType(MetricCategoryCard), findsNWidgets(2));

    await tester.pumpAndSettle();

    addTearDown(() {
      tester.view.resetPhysicalSize();
      tester.view.resetDevicePixelRatio();
    });
  });

  testWidgets('ScoreGauge handles zero score and settles', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: ScoreGauge(score: 0, animationDuration: Duration.zero),
        ),
      ),
    );
    await tester.pump();
    expect(find.text('0'), findsOneWidget);
  });

  testWidgets('ScoreGauge handles high score and settles', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: ScoreGauge(score: 100, animationDuration: Duration.zero),
        ),
      ),
    );
    await tester.pump();
    expect(find.text('100'), findsOneWidget);
  });

  testWidgets('ContextReportView respects showHeader parameter', (tester) async {
    await tester.pumpWidget(createWidgetUnderTest(
      ContextReportView(report: testReport, showHeader: false),
    ));
    await tester.pumpAndSettle();

    expect(find.text('Test Address'), findsNothing);
    expect(find.byType(ScoreGauge), findsOneWidget);
  });
}
