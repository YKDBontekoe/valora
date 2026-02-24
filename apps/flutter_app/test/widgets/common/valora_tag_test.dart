import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/common/valora_tag.dart';

void main() {
  group('ValoraTag', () {
    testWidgets('renders label and icon correctly', (WidgetTester tester) async {
      const String testLabel = 'Test Tag';
      const IconData testIcon = Icons.home;

      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: Center(
              child: ValoraTag(
                label: testLabel,
                icon: testIcon,
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text(testLabel), findsOneWidget);
      expect(find.byIcon(testIcon), findsOneWidget);
    });

    testWidgets('responds to tap when onTap is provided', (WidgetTester tester) async {
      bool tapped = false;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Center(
              child: ValoraTag(
                label: 'Tap Me',
                onTap: () {
                  tapped = true;
                },
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      // Ensure tap hits the widget center by finding the InkWell or the widget itself
      // Use warnIfMissed: false to suppress hit test warnings if the widget is small or partially obscured
      // But typically ensuring visible and tapping center works.
      // The previous error was "derived an Offset ... that would not hit test".
      // This often happens if the widget is too small or layout is weird in tests.
      // Tapping by text is often safer.
      await tester.tap(find.text('Tap Me'), warnIfMissed: false);
      await tester.pumpAndSettle();

      expect(tapped, isTrue);
    });

    testWidgets('does not wrap in GestureDetector when onTap is null', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: Center(
              child: ValoraTag(
                label: 'Static Tag',
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      final valoraTagFinder = find.byType(ValoraTag);

      final gestureDetectorFinder = find.descendant(
        of: valoraTagFinder,
        matching: find.byType(GestureDetector),
      );

      expect(gestureDetectorFinder, findsNothing);
    });

    testWidgets('applies custom background color', (WidgetTester tester) async {
      const Color customColor = Colors.red;

      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: Center(
              child: ValoraTag(
                label: 'Red Tag',
                backgroundColor: customColor,
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      // Find the Container within ValoraTag which comes from AnimatedContainer
      final containerFinder = find.descendant(
        of: find.byType(ValoraTag),
        matching: find.byType(Container),
      );

      final container = tester.widget<Container>(containerFinder);
      final decoration = container.decoration as BoxDecoration;

      expect(decoration.color, customColor);
    });
  });
}
