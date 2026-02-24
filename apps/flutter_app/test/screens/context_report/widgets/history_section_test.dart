import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/screens/context_report/widgets/history_section.dart';
import 'package:valora_app/models/search_history_item.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

class MockContextReportProvider extends Mock implements ContextReportProvider {
  @override
  bool get isLoading => super.noSuchMethod(
        Invocation.getter(#isLoading),
        returnValue: false,
      );

  @override
  List<SearchHistoryItem> get history => super.noSuchMethod(
        Invocation.getter(#history),
        returnValue: <SearchHistoryItem>[],
      );

  @override
  bool isComparing(String? query, int? radius) => super.noSuchMethod(
        Invocation.method(#isComparing, [query, radius]),
        returnValue: false,
      );

  @override
  Future<void> generate(String? query) => super.noSuchMethod(
        Invocation.method(#generate, [query]),
        returnValue: Future.value(),
        returnValueForMissingStub: Future.value(),
      );

  @override
  Future<void> clearHistory() => super.noSuchMethod(
        Invocation.method(#clearHistory, []),
        returnValue: Future.value(),
        returnValueForMissingStub: Future.value(),
      );

  @override
  int get radiusMeters => 1000;
}

void main() {
  testWidgets('HistorySection renders items and handles tap', (WidgetTester tester) async {
    final controller = TextEditingController();
    final mockProvider = MockContextReportProvider();

    final historyItems = [
      SearchHistoryItem(query: 'Amsterdam', timestamp: DateTime.now()),
      SearchHistoryItem(query: 'Rotterdam', timestamp: DateTime.now().subtract(const Duration(days: 1))),
    ];

    when(mockProvider.history).thenReturn(historyItems);
    when(mockProvider.isLoading).thenReturn(false);
    when(mockProvider.isComparing(any, any)).thenReturn(false);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ChangeNotifierProvider<ContextReportProvider>.value(
            value: mockProvider,
            child: HistorySection(
              controller: controller,
              provider: mockProvider,
            ),
          ),
        ),
      ),
    );

    // Verify items are rendered
    expect(find.text('Amsterdam'), findsOneWidget);
    expect(find.text('Rotterdam'), findsOneWidget);

    // Tap an item
    await tester.tap(find.text('Amsterdam'));
    await tester.pumpAndSettle();

    // Verify controller updated and generate called
    expect(controller.text, 'Amsterdam');
    verify(mockProvider.generate('Amsterdam')).called(1);
  });

  testWidgets('HistorySection handles clear history', (WidgetTester tester) async {
    final controller = TextEditingController();
    final mockProvider = MockContextReportProvider();

    final historyItems = [
      SearchHistoryItem(query: 'Amsterdam', timestamp: DateTime.now()),
    ];

    when(mockProvider.history).thenReturn(historyItems);
    when(mockProvider.isLoading).thenReturn(false);
    when(mockProvider.isComparing(any, any)).thenReturn(false);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ChangeNotifierProvider<ContextReportProvider>.value(
            value: mockProvider,
            child: HistorySection(
              controller: controller,
              provider: mockProvider,
            ),
          ),
        ),
      ),
    );

    // Tap clear button
    // "Clear" might appear twice (in header and dialog), so tap the visible one (header)
    final clearButton = find.text('Clear');
    expect(clearButton, findsOneWidget); // Should only be one visible initially
    await tester.tap(clearButton);
    await tester.pumpAndSettle(); // Dialog animation

    // Verify dialog appears
    // "Clear History?" is the title
    expect(find.text('Clear History?'), findsOneWidget);

    // Confirm clear. The dialog has a 'Clear' button.
    // Now there are two 'Clear' texts potentially. The one in header is covered by modal.
    // But find.text finds all, even offscreen/obscured sometimes depending on finder.
    // But "Clear History?" confirms dialog is open.
    // Let's find the button in the dialog.
    // We can assume the last "Clear" is the button in the dialog (top of stack).
    await tester.tap(find.text('Clear').last);
    await tester.pumpAndSettle();

    verify(mockProvider.clearHistory()).called(1);
  });
}
