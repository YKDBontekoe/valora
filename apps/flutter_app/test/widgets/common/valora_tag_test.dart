import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/common/valora_tag.dart';

void main() {
  group('ValoraTag', () {
    testWidgets('renders label and icon correctly', (WidgetTester tester) async {
      const String testLabel = 'Test Tag';
      const IconData testIcon = Icons.home;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraTag(
              label: testLabel,
              icon: testIcon,
            ),
          ),
        ),
      );
      // Wait for animations to complete
      await tester.pumpAndSettle();

      expect(find.text(testLabel), findsOneWidget);
      expect(find.byIcon(testIcon), findsOneWidget);
    });

    testWidgets('responds to tap when onTap is provided', (WidgetTester tester) async {
      bool tapped = false;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraTag(
              label: 'Tap Me',
              onTap: () {
                tapped = true;
              },
            ),
          ),
        ),
      );
      // Wait for initial animations
      await tester.pumpAndSettle();

      // Tap the text to ensure we hit a specific target inside the gesture detector
      // avoiding hit test warnings on the outermost animated widget.
      await tester.tap(find.text('Tap Me'));
      await tester.pumpAndSettle();

      expect(tapped, isTrue);
    });

    testWidgets('does not wrap in GestureDetector when onTap is null', (WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraTag(
              label: 'Static Tag',
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byType(GestureDetector), findsNothing);

      final valoraTagFinder = find.byType(ValoraTag);
      final animatedContainerFinder = find.descendant(
        of: valoraTagFinder,
        matching: find.byType(AnimatedContainer),
      );

      expect(animatedContainerFinder, findsOneWidget);

      // Ensure no GestureDetector is an ancestor of the AnimatedContainer *within* ValoraTag
      final gestureDetectorFinder = find.ancestor(
        of: animatedContainerFinder,
        matching: find.byType(GestureDetector),
      );

      expect(gestureDetectorFinder, findsNothing);
    });

    testWidgets('applies custom background color', (WidgetTester tester) async {
      const Color customColor = Colors.red;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraTag(
              label: 'Red Tag',
              backgroundColor: customColor,
            ),
          ),
        ),
      );
      // Wait for animations to complete
      await tester.pumpAndSettle();

      // We need to inspect the AnimatedContainer's decoration.
      // With onTap=null, ValoraTag structure is directly AnimatedContainer

      final animatedContainer = tester.widget<AnimatedContainer>(
        find.descendant(
          of: find.byType(ValoraTag),
          matching: find.byType(AnimatedContainer),
        ),
      );

      final decoration = animatedContainer.decoration as BoxDecoration;
      expect(decoration.color, customColor);
    });
  });
}
