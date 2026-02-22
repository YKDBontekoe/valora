import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/screens/context_report_screen.dart';
import 'package:valora_app/repositories/context_report_repository.dart';
import 'package:valora_app/repositories/ai_repository.dart';
import 'package:valora_app/services/pdok_service.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:shared_preferences/shared_preferences.dart';

class MockContextReportRepository extends Fake implements ContextReportRepository {
  @override
  Future<ContextReport> getContextReport(String input, {int radiusMeters = 1000}) async {
    return ContextReport(
      location: ContextLocation(
        query: input,
        displayAddress: '$input address',
        municipalityName: 'Amsterdam',
        neighborhoodName: 'Centrum',
        latitude: 52.0,
        longitude: 4.0,
      ),
      compositeScore: 80,
      categoryScores: {'Social': 80},
      socialMetrics: [],
      crimeMetrics: [],
      demographicsMetrics: [],
      housingMetrics: [],
      mobilityMetrics: [],
      amenityMetrics: [],
      environmentMetrics: [],
      sources: [],
      warnings: [],
    );
  }
}

class MockAiRepository extends Fake implements AiRepository {
  @override
  Future<String> getAiAnalysis(ContextReport report) async => "";
}

class MockPdokService extends Fake implements PdokService {}

void main() {
  late MockContextReportRepository contextReportRepository;
  late MockAiRepository aiRepository;
  late MockPdokService pdokService;

  setUp(() {
    SharedPreferences.setMockInitialValues({});
    contextReportRepository = MockContextReportRepository();
    aiRepository = MockAiRepository();
    pdokService = MockPdokService();
  });

  Widget createWidget() {
    return MaterialApp(
      home: MultiProvider(
        providers: [
          Provider<ContextReportRepository>.value(value: contextReportRepository),
          Provider<AiRepository>.value(value: aiRepository),
        ],
        child: ContextReportScreen(pdokService: pdokService),
      ),
    );
  }

  // Test specifically for the new slider optimization
  testWidgets('ContextReportScreen slider updates radius', (tester) async {
    await tester.runAsync(() async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      // Find the slider
      final sliderFinder = find.byType(Slider);
      expect(sliderFinder, findsOneWidget);

      // Verify initial value in the Badge
      expect(find.text('1000m'), findsOneWidget); // Default is usually 1000

      // Drag the slider
      // Get the slider widget

      // Tap/Drag on the slider
      await tester.tap(sliderFinder);
      await tester.pumpAndSettle();

      await tester.drag(sliderFinder, const Offset(100, 0));
      await tester.pump();
      await tester.pumpAndSettle();

      // Verify that the badge text has changed from the default '1000m'
      expect(find.text('1000m'), findsNothing);
    });
  });
}
