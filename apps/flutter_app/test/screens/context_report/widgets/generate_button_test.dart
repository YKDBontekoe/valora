import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/providers/context_report_provider.dart';
import 'package:valora_app/screens/context_report/widgets/generate_button.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

// Create a mock for ContextReportProvider manually since Mockito isn't generating one easily here
class MockContextReportProvider extends Mock implements ContextReportProvider {
  @override
  bool get isLoading => super.noSuchMethod(
        Invocation.getter(#isLoading),
        returnValue: false,
      );

  @override
  Future<void> generate(String? query) => super.noSuchMethod(
        Invocation.method(#generate, [query]),
        returnValue: Future.value(),
        returnValueForMissingStub: Future.value(),
      );
}

void main() {
  testWidgets('GenerateButton enables when text is entered', (WidgetTester tester) async {
    final controller = TextEditingController();
    final mockProvider = MockContextReportProvider();

    // Stub methods
    when(mockProvider.isLoading).thenReturn(false);

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: GenerateButton(
            controller: controller,
            provider: mockProvider,
          ),
        ),
      ),
    );

    // Initial state: empty text -> disabled button
    // Find ValoraButton
    final buttonFinder = find.byType(ValoraButton);
    expect(buttonFinder, findsOneWidget);

    // Tap it (should do nothing as it's disabled)
    await tester.tap(buttonFinder);
    await tester.pumpAndSettle(); // Allow tap animations to settle
    verifyNever(mockProvider.generate(any));

    // Enter text directly into controller (simulate typing)
    controller.text = "Amsterdam";
    // Trigger a frame. The button should rebuild if it was listening.
    await tester.pumpAndSettle(); // Rebuild and settle animations

    // Tap again
    await tester.tap(buttonFinder);
    await tester.pumpAndSettle(); // Allow tap animations to settle

    // EXPECTATION: generate() should be called once with "Amsterdam"
    verify(mockProvider.generate('Amsterdam')).called(1);
  });
}
