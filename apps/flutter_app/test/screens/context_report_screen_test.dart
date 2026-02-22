import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/screens/context_report_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/pdok_service.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/models/search_history_item.dart';
import 'package:valora_app/widgets/report/context_report_view.dart';
import 'package:valora_app/widgets/report/context_report_skeleton.dart';
import 'package:valora_app/widgets/valora_widgets.dart'; // Import ValoraEmptyState
import 'package:valora_app/core/exceptions/app_exceptions.dart'; // Import AppException
import 'package:valora_app/widgets/report/comparison_view.dart'; // Import ComparisonView
import 'package:google_fonts/google_fonts.dart';
import 'package:shared_preferences/shared_preferences.dart'; // Import shared_preferences
import 'package:flutter/services.dart'; // For PlatformException
import 'dart:async'; // For StreamController
import 'package:flutter/foundation.dart'; // For ByteData

// import 'package:valora_app/core/theme/valora_typography.dart'; // Import ValoraTypography

@GenerateMocks([ApiService, PdokService])
import 'context_report_screen_test.mocks.dart';

void main() {
  late MockApiService mockApiService;
  late MockPdokService mockPdokService;
  late ValueNotifier<Widget?> fabNotifier; // To capture FAB changes

  setUp(() {
    mockApiService = MockApiService();
    mockPdokService = MockPdokService();
    fabNotifier = ValueNotifier<Widget?>(null);

    // Default mock responses
    when(mockApiService.getContextReport(any, radiusMeters: anyNamed('radiusMeters'))).thenAnswer((_) async =>
      ContextReport(
        location: ContextLocation(
          query: 'Test Address',
          displayAddress: 'Test Address 123',
          municipalityName: 'Test Municipality',
          neighborhoodName: 'Test Neighborhood',
          latitude: 52.0,
          longitude: 4.0,
        ),
        compositeScore: 80,
        categoryScores: {'Social': 80},
        socialMetrics: [
          ContextMetric(key: 'metric_a', label: 'Metric A', value: 10.0, unit: '%', source: 'Source A')
        ],
        crimeMetrics: [],
        demographicsMetrics: [],
        housingMetrics: [],
        mobilityMetrics: [],
        amenityMetrics: [
          ContextMetric(key: 'amenity_a', label: 'Amenity A', value: 5, unit: '', source: 'Source B')
        ],
        environmentMetrics: [],
        sources: [],
        warnings: [],
      )
    );

    when(mockPdokService.search(any)).thenAnswer((_) async => [
      PdokSuggestion(id: '1', displayName: 'Suggestion 1', type: 'Address', score: 1.0),
    ]);

    when(mockPdokService.reverseLookup(any, any)).thenAnswer((_) async => 'Mock Address from Map');
  });

  Widget createWidget({ValueChanged<Widget?>? onFabChanged}) {
    return MaterialApp(
      home: Provider<ApiService>.value(
        value: mockApiService,
        child: ContextReportScreen(
          pdokService: mockPdokService,
          onFabChanged: onFabChanged,
        ),
      ),
      theme: ThemeData(
        fontFamily: 'Roboto', // Use a generic font family
        useMaterial3: true,
      ),
    );
  }

  setUpAll(() {
    TestWidgetsFlutterBinding.ensureInitialized();
    GoogleFonts.config.allowRuntimeFetching = false;
    // Initialize mock for SharedPreferences
    SharedPreferences.setMockInitialValues({});
    
    // Mock flutter_keyboard_visibility
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger.setMockMethodCallHandler(
      const MethodChannel('flutter_keyboard_visibility'),
      (MethodCall methodCall) async {
        if (methodCall.method == 'listen') {
          // Return null to simulate no stream from the platform side
          return Future.value(null);
        }
        return null;
      },
    );
  });

  testWidgets('ContextReportScreen renders search form components', (tester) async {
    await tester.runAsync(() async {
      await tester.pumpWidget(createWidget());
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));
      await tester.pumpAndSettle();

      expect(find.text('Property Analytics'), findsAtLeastNWidgets(1));
      expect(find.text('Search city, zip, or address...'), findsOneWidget);
      expect(find.text('Analysis Radius'), findsOneWidget);
      expect(find.text('Generate Report'), findsOneWidget);
    });
  }, skip: true);

  testWidgets('Input validation prevents search with less than 3 characters', (tester) async {
    await tester.runAsync(() async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      final inputFinder = find.byType(ValoraTextField);
      expect(inputFinder, findsOneWidget);

      await tester.enterText(inputFinder, 'ab');
      await tester.testTextInput.receiveAction(TextInputAction.search);
      await tester.pump();
      await tester.pumpAndSettle();

      verifyNever(mockApiService.getContextReport(any, radiusMeters: anyNamed('radiusMeters')));
      expect(find.text('Enter an address or Funda URL.'), findsOneWidget);
    });
  }, skip: true);

  testWidgets('Input validation allows search with 3 or more characters and generates report', (tester) async {
    await tester.runAsync(() async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      final inputFinder = find.byType(ValoraTextField);
      await tester.enterText(inputFinder, 'abc');
      await tester.testTextInput.receiveAction(TextInputAction.search);
      await tester.pump();
      await tester.pumpAndSettle();

      verify(mockApiService.getContextReport('abc', radiusMeters: anyNamed('radiusMeters'))).called(1);
      expect(find.byType(ContextReportView), findsOneWidget);
      expect(find.text('Test Address 123'), findsOneWidget);
    });
  }, skip: true);

  testWidgets('ContextReportScreen slider updates radius', (tester) async {
    await tester.runAsync(() async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      final sliderFinder = find.byType(Slider);
      expect(sliderFinder, findsOneWidget);

      expect(find.text('1000m'), findsOneWidget);

      await tester.drag(sliderFinder, const Offset(100, 0));
      await tester.pumpAndSettle();

      expect(find.text('1000m'), findsNothing);
      expect(find.textContaining('m'), findsOneWidget);
    });
  }, skip: true);

  testWidgets('ContextReportScreen displays report layout after successful generation', (tester) async {
    await tester.runAsync(() async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      final inputFinder = find.byType(SearchField);
      await tester.enterText(inputFinder, 'Valid Address');
      await tester.tap(find.text('Generate Report'));
      await tester.pumpAndSettle();

      expect(find.byType(ReportLayout), findsOneWidget);
      expect(find.byType(CompactSearchField), findsOneWidget);
      expect(find.byType(CompareButton), findsOneWidget);
      expect(find.byType(ContextReportView), findsOneWidget);
    });
  }, skip: true);

  testWidgets('ContextReportScreen displays skeleton during report generation', (tester) async {
    when(mockApiService.getContextReport(any, radiusMeters: anyNamed('radiusMeters'))).thenAnswer((_) async {
      await Future.delayed(const Duration(milliseconds: 500));
      return ContextReport(
        location: ContextLocation(
          query: 'Delayed Address',
          displayAddress: 'Delayed Address 123',
          municipalityName: 'Delayed Municipality',
          neighborhoodName: 'Delayed Neighborhood',
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
    });

    await tester.runAsync(() async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      final inputFinder = find.byType(SearchField);
      await tester.enterText(inputFinder, 'Delayed Address');
      await tester.tap(find.text('Generate Report'));
      await tester.pump();

      expect(find.byType(ContextReportSkeleton), findsOneWidget);
      expect(find.byType(ContextReportView), findsNothing);

      await tester.pumpAndSettle();

      expect(find.byType(ContextReportSkeleton), findsNothing);
      expect(find.byType(ContextReportView), findsOneWidget);
    });
  }, skip: true);

  testWidgets('ContextReportScreen displays error state when report generation fails', (tester) async {
    when(mockApiService.getContextReport(any, radiusMeters: anyNamed('radiusMeters')))
        .thenThrow(AppException('Failed to fetch report from API.'));

    await tester.runAsync(() async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      final inputFinder = find.byType(SearchField);
      await tester.enterText(inputFinder, 'Failing Address');
      await tester.tap(find.text('Generate Report'));
      await tester.pumpAndSettle();

      expect(find.byType(SearchLayout), findsOneWidget);
      expect(find.byType(ValoraEmptyState), findsOneWidget);
      expect(find.text('Analysis Failed'), findsOneWidget);
      expect(find.text('Failed to fetch report from API.'), findsOneWidget);
    });
  }, skip: true);

  testWidgets('ContextReportScreen FAB changes for comparison mode', (tester) async {
    await tester.runAsync(() async {
      await tester.pumpWidget(createWidget(onFabChanged: (fab) => fabNotifier.value = fab));
      await tester.pumpAndSettle();

      expect(fabNotifier.value, isNull);

      final inputFinder = find.byType(SearchField);
      await tester.enterText(inputFinder, 'Address 1');
      await tester.tap(find.text('Generate Report'));
      await tester.pumpAndSettle();

      await tester.tap(find.byType(CompareButton));
      await tester.pumpAndSettle();

      expect(fabNotifier.value, isA<FloatingActionButton>());
      expect(find.byWidget(fabNotifier.value!), findsOneWidget);
      expect(find.text('Compare (1)'), findsOneWidget);

      await tester.enterText(find.byType(CompactSearchField).first, 'Address 2');
      await tester.tap(find.byIcon(Icons.search_rounded).first);
      await tester.pumpAndSettle();

      await tester.tap(find.byType(CompareButton));
      await tester.pumpAndSettle();

      expect(fabNotifier.value, isA<FloatingActionButton>());
      expect(find.byWidget(fabNotifier.value!), findsOneWidget);
      expect(find.text('Compare (2)'), findsOneWidget);

      await tester.tap(find.byWidget(fabNotifier.value!));
      await tester.pumpAndSettle();

      expect(find.byType(ComparisonLayout), findsOneWidget);
    });
  }, skip: true);

  testWidgets('ComparisonLayout displays correctly and clears comparisons', (tester) async {
    await tester.runAsync(() async {
      await tester.pumpWidget(createWidget(onFabChanged: (fab) => fabNotifier.value = fab));
      await tester.pumpAndSettle();

      await tester.enterText(find.byType(SearchField), 'Address 1');
      await tester.tap(find.text('Generate Report'));
      await tester.pumpAndSettle();
      await tester.tap(find.byType(CompareButton));
      await tester.pumpAndSettle();

      await tester.enterText(find.byType(CompactSearchField).first, 'Address 2');
      await tester.tap(find.byIcon(Icons.search_rounded).first);
      await tester.pumpAndSettle();
      await tester.tap(find.byType(CompareButton));
      await tester.pumpAndSettle();

      await tester.tap(find.byWidget(fabNotifier.value!));
      await tester.pumpAndSettle();

      expect(find.byType(ComparisonLayout), findsOneWidget);
      expect(find.text('Compare Properties'), findsOneWidget);
      expect(find.byIcon(Icons.arrow_back_rounded), findsOneWidget);
      expect(find.byIcon(Icons.delete_sweep_rounded), findsOneWidget);
      expect(find.byType(ComparisonView), findsOneWidget);

      await tester.tap(find.byIcon(Icons.delete_sweep_rounded));
      await tester.pumpAndSettle();

      expect(find.byType(ComparisonLayout), findsOneWidget);

      await tester.tap(find.byIcon(Icons.arrow_back_rounded));
      await tester.pumpAndSettle();

      expect(find.byType(ReportLayout), findsOneWidget);
      expect(fabNotifier.value, isNull);
    });
  }, skip: true);
}
