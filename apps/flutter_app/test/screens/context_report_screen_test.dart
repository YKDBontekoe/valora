import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/screens/context_report_screen.dart';
import 'package:valora_app/repositories/context_report_repository.dart';
import 'package:valora_app/services/pdok_service.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:google_fonts/google_fonts.dart';

@GenerateMocks([ContextReportRepository, PdokService])
import 'context_report_screen_test.mocks.dart';

void main() {
  late MockContextReportRepository mockRepository;
  late MockPdokService mockPdokService;

  setUp(() {
    mockRepository = MockContextReportRepository();
    mockPdokService = MockPdokService();
    // Disable runtime fetching to avoid network calls during tests and suppress missing asset errors.
    GoogleFonts.config.allowRuntimeFetching = false;
  });

  Widget createWidget() {
    return MaterialApp(
      home: Provider<ContextReportRepository>.value(
        value: mockRepository,
        child: ContextReportScreen(pdokService: mockPdokService),
      ),
      theme: ThemeData(
        fontFamily: 'Roboto',
        useMaterial3: true,
      ),
    );
  }

  // Prevent GoogleFonts from throwing errors during testing by intercepting them
  final originalOnError = FlutterError.onError;
  setUpAll(() {
    FlutterError.onError = (FlutterErrorDetails details) {
      if (details.exception.toString().contains('GoogleFonts') || details.exception.toString().contains('Failed to load font') || details.exception.toString().contains('NetworkImage') ||
          details.exception.toString().contains('MissingPluginException') && (details.exception.toString().contains('font') || details.exception.toString().contains('google_fonts'))) {
        return;
      }
      originalOnError?.call(details);
    };
  });

  tearDownAll(() {
    FlutterError.onError = originalOnError;
  });

  // TODO(issue/#): re-enable once font loading is handled in tests
  testWidgets('ContextReportScreen renders search form components', (tester) async {
    await tester.runAsync(() async {
      await tester.pumpWidget(createWidget());
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));
      // Added settle to ensure no pending animations trigger errors later
      await tester.pumpAndSettle();

      expect(find.text('Property Analytics'), findsAtLeastNWidgets(1));
      expect(find.text('Search Property'), findsOneWidget);
    });
  }, skip: true);

  // TODO(issue/#): re-enable once font loading is handled in tests
  testWidgets('Input validation prevents search with less than 3 characters', (tester) async {
    await tester.runAsync(() async {
      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      final inputFinder = find.byType(TextField);
      expect(inputFinder, findsOneWidget);

      await tester.enterText(inputFinder, 'ab');
      await tester.testTextInput.receiveAction(TextInputAction.search);
      await tester.pump();
      await tester.pumpAndSettle(); // Ensure SnackBars and animations settle

      // Verify no API call was made
      verifyNever(mockRepository.getContextReport(any, radiusMeters: anyNamed('radiusMeters')));

      // Verify SnackBar appears
      expect(find.text('Please enter at least 3 characters.'), findsOneWidget);
    });
  }, skip: true);

  // TODO(issue/#): re-enable once font loading is handled in tests
  testWidgets('Input validation allows search with 3 or more characters', (tester) async {
    await tester.runAsync(() async {
      // Setup successful API response to avoid errors
      when(mockRepository.getContextReport(any, radiusMeters: anyNamed('radiusMeters'))).thenAnswer((_) async =>
        ContextReport(
          location: ContextLocation(
            query: 'abc',
            displayAddress: 'Main St 123',
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
        )
      );

      await tester.pumpWidget(createWidget());
      await tester.pumpAndSettle();

      final inputFinder = find.byType(TextField);
      await tester.enterText(inputFinder, 'abc');
      await tester.testTextInput.receiveAction(TextInputAction.search);
      await tester.pumpAndSettle(); // Ensure API call and subsequent UI updates settle

      // Verify API call was made
      verify(mockRepository.getContextReport('abc', radiusMeters: anyNamed('radiusMeters'))).called(1);
    });
  }, skip: true);

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
