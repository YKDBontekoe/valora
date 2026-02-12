import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/common/valora_tag.dart';
import 'package:valora_app/core/theme/valora_colors.dart';
import 'package:valora_app/core/theme/valora_spacing.dart';

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

      await tester.tap(find.byType(ValoraTag));
      await tester.pumpAndSettle();

      expect(tapped, isTrue);
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
      // ValoraTag structure: MouseRegion -> GestureDetector -> AnimatedContainer

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
