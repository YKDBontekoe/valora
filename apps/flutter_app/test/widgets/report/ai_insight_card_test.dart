import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/widgets/report/ai_insight_card.dart';

@GenerateMocks([ApiService])
import 'ai_insight_card_test.mocks.dart';

void main() {
  late MockApiService mockApiService;
  late ContextReport report;

  setUp(() {
    mockApiService = MockApiService();
    report = ContextReport(
      location: ContextLocation(
        query: 'Amsterdam',
        displayAddress: 'Dam Square 1',
        latitude: 52.373,
        longitude: 4.892,
      ),
      socialMetrics: [],
      crimeMetrics: [],
      demographicsMetrics: [],
      amenityMetrics: [],
      environmentMetrics: [],
      compositeScore: 8.5,
      categoryScores: {'Safety': 9.0},
      sources: [],
      warnings: [],
    );
  });

  Widget createWidgetUnderTest() {
    return MaterialApp(
      home: Scaffold(
        body: Provider<ApiService>.value(
          value: mockApiService,
          child: AiInsightCard(report: report),
        ),
      ),
    );
  }

  testWidgets('AiInsightCard shows initial state correctly', (tester) async {
    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    expect(find.text('Unlock Neighborhood Insights'), findsOneWidget);
    expect(
      find.text('Get an AI-powered summary of the pros & cons.'),
      findsOneWidget,
    );
    expect(find.text('Generate Insight'), findsOneWidget);
  });

  testWidgets('AiInsightCard shows summary on success', (tester) async {
    when(mockApiService.getAiAnalysis(any))
        .thenAnswer((_) async => 'This is a **great** neighborhood.');

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Generate Insight'));
    await tester.pump(); // Start loading
    // We cannot pumpAndSettle here easily because of infinite animations or async gap,
    // but the ApiService mock returns immediately in the next microtask.
    await tester.pump(const Duration(milliseconds: 100));
    // Settle animations
    await tester.pumpAndSettle();

    expect(find.text('AI Insight'), findsOneWidget);
    expect(find.byKey(const Key('ai-summary-text')), findsOneWidget);

    // Check rich text content indirectly or by finding the widget
    final richTextFinder = find.descendant(
      of: find.byKey(const Key('ai-summary-text')),
      matching: find.byType(RichText),
    );
    final RichText richText = tester.widget(richTextFinder);
    final textSpan = richText.text as TextSpan;
    // 'This is a ' (normal) + 'great' (bold) + ' neighborhood.' (normal)
    expect(textSpan.children!.length, 3);
  });

  testWidgets('AiInsightCard shows generic error on unknown exception', (
    tester,
  ) async {
    when(mockApiService.getAiAnalysis(any)).thenThrow(Exception('Boom!'));

    await tester.pumpWidget(createWidgetUnderTest());

    await tester.tap(find.text('Generate Insight'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(
      find.text('Failed to generate insight. Please try again.'),
      findsOneWidget,
    );
    expect(find.text('Retry'), findsOneWidget);
  });

  testWidgets('AiInsightCard shows specific error on AppException', (
    tester,
  ) async {
    when(
      mockApiService.getAiAnalysis(any),
    ).thenThrow(ServerException('Server is busy.'));

    await tester.pumpWidget(createWidgetUnderTest());

    await tester.tap(find.text('Generate Insight'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.text('Server is busy.'), findsOneWidget);
    expect(find.text('Retry'), findsOneWidget);
  });

  testWidgets('AiInsightCard retry button works', (tester) async {
    // First call fails
    when(mockApiService.getAiAnalysis(any)).thenThrow(Exception('Boom!'));

    await tester.pumpWidget(createWidgetUnderTest());

    await tester.tap(find.text('Generate Insight'));
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.text('Retry'), findsOneWidget);

    // Second call succeeds
    when(mockApiService.getAiAnalysis(any)).thenAnswer((_) async => 'Success!');

    // Settle pending timers from previous pump
    await tester.pumpAndSettle();

    await tester.tap(find.text('Retry'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.text('AI Insight'), findsOneWidget);
  });
}
