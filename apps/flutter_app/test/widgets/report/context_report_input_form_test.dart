import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/services/pdok_service.dart';
import 'package:valora_app/widgets/report/context_report_input_form.dart';
import 'package:valora_app/widgets/valora_widgets.dart';
import 'package:valora_app/models/search_history_item.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';

import 'context_report_input_form_test.mocks.dart';

// Generate mocks
@GenerateMocks([ContextReportProvider, PdokService])
void main() {
  late MockContextReportProvider mockProvider;
  late MockPdokService mockPdokService;
  late TextEditingController controller;

  setUp(() {
    mockProvider = MockContextReportProvider();
    mockPdokService = MockPdokService();
    controller = TextEditingController();

    // Default mock behavior
    when(mockProvider.isLoading).thenReturn(false);
    when(mockProvider.error).thenReturn(null);
    when(mockProvider.radiusMeters).thenReturn(1000);
    when(mockProvider.history).thenReturn([]);
    when(mockPdokService.search(any)).thenAnswer((_) async => []);
  });

  tearDown(() {
    controller.dispose();
  });

  Widget createSubject() {
    return MaterialApp(
      home: Scaffold(
        body: ChangeNotifierProvider<ContextReportProvider>.value(
          value: mockProvider,
          child: ContextReportInputForm(
            controller: controller,
            provider: mockProvider,
            pdokService: mockPdokService,
          ),
        ),
      ),
    );
  }

  group('ContextReportInputForm', () {
    testWidgets('renders initial state correctly', (tester) async {
      await tester.pumpWidget(createSubject());
      await tester.pumpAndSettle(); // settle animations

      expect(find.text('Property Analytics'), findsOneWidget);
      expect(find.byType(ValoraTextField), findsOneWidget);
      expect(find.byType(Slider), findsOneWidget);
      expect(find.text('Generate Full Report'), findsOneWidget);
    });

    testWidgets('shows validation error for short input', (tester) async {
      await tester.pumpWidget(createSubject());
      await tester.pumpAndSettle();

      await tester.enterText(find.byType(ValoraTextField), 'ab');
      await tester.pump(); // Rebuild button state

      await tester.tap(find.text('Generate Full Report'));
      await tester.pumpAndSettle(); // Allow SnackBar to animate

      expect(find.text('Please enter at least 3 characters.'), findsOneWidget);
      verifyNever(mockProvider.generate(any));
    });

    testWidgets('calls generate for valid input', (tester) async {
      await tester.pumpWidget(createSubject());
      await tester.pumpAndSettle();

      await tester.enterText(find.byType(ValoraTextField), 'Amsterdam');
      await tester.pump(); // Update button state

      await tester.tap(find.text('Generate Full Report'));

      verify(mockProvider.generate('Amsterdam')).called(1);
    });

    testWidgets('updates radius on slider change', (tester) async {
      await tester.pumpWidget(createSubject());
      await tester.pumpAndSettle();

      await tester.drag(find.byType(Slider), const Offset(50, 0));
      // Dragging might trigger multiple updates
      verify(mockProvider.setRadiusMeters(any)).called(greaterThan(0));
    });

    testWidgets('displays error state and retry logic', (tester) async {
      when(mockProvider.error).thenReturn('Network Error');
      controller.text = 'Rotterdam';

      await tester.pumpWidget(createSubject());
      await tester.pumpAndSettle();

      // Scroll to ensure the error state is visible
      await tester.scrollUntilVisible(
        find.byType(ValoraEmptyState),
        500.0,
        scrollable: find.byType(Scrollable).first,
      );

      expect(find.byType(ValoraEmptyState), findsOneWidget);
      // We check for the generic message now
      expect(find.text('Something went wrong. Please try again.'), findsOneWidget);
      // And ensure the raw error is NOT shown
      expect(find.text('Network Error'), findsNothing);

      await tester.tap(find.text('Try Again'));
      // Note: The onAction logic calls _handleSubmit, which then calls provider.generate
      // provided input validation passes. 'Rotterdam' is > 3 chars.
      verify(mockProvider.generate('Rotterdam')).called(1);
    });

    testWidgets('handles history selection', (tester) async {
      final historyItem = SearchHistoryItem(
        query: 'Utrecht',
        timestamp: DateTime.now(),
      );
      when(mockProvider.history).thenReturn([historyItem]);

      await tester.pumpWidget(createSubject());
      await tester.pumpAndSettle();

      // Scroll to recent searches
      await tester.scrollUntilVisible(
        find.text('Recent Searches'),
        500.0,
        scrollable: find.byType(Scrollable).first,
      );

      expect(find.text('Recent Searches'), findsOneWidget);

      // History items are in a horizontal list inside the vertical list.
      // We need to ensure the item is visible.
      await tester.scrollUntilVisible(
        find.text('Utrecht'),
        500.0,
        scrollable: find.byType(Scrollable).last, // The horizontal list
      );

      await tester.tap(find.text('Utrecht'));
      expect(controller.text, 'Utrecht');
      verify(mockProvider.generate('Utrecht')).called(1);
    });

    testWidgets('handles clear history dialog - confirm', (tester) async {
      when(mockProvider.history).thenReturn([
        SearchHistoryItem(query: 'Test', timestamp: DateTime.now())
      ]);

      await tester.pumpWidget(createSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Clear All'),
        500.0,
        scrollable: find.byType(Scrollable).first,
      );

      await tester.tap(find.text('Clear All'));
      await tester.pumpAndSettle();

      expect(find.text('Clear History?'), findsOneWidget);

      await tester.tap(find.text('Clear'));
      verify(mockProvider.clearHistory()).called(1);
    });

    testWidgets('handles clear history dialog - cancel', (tester) async {
      when(mockProvider.history).thenReturn([
        SearchHistoryItem(query: 'Test', timestamp: DateTime.now())
      ]);

      await tester.pumpWidget(createSubject());
      await tester.pumpAndSettle();

      await tester.scrollUntilVisible(
        find.text('Clear All'),
        500.0,
        scrollable: find.byType(Scrollable).first,
      );

      await tester.tap(find.text('Clear All'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Cancel'));
      verifyNever(mockProvider.clearHistory());
    });
  });
}
