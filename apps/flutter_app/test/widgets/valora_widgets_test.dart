import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/core/theme/valora_shadows.dart';
import 'package:valora_app/core/theme/valora_spacing.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

void main() {
  group('ValoraCard Tests', () {
    testWidgets('renders child content', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(body: ValoraCard(child: Text('Test Child'))),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.text('Test Child'), findsOneWidget);
    });

    testWidgets('calls onTap when tapped', (WidgetTester tester) async {
      bool tapped = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraCard(
              onTap: () => tapped = true,
              child: const Text('Tap Me'),
            ),
          ),
        ),
      );

      await tester.tap(find.text('Tap Me'));
      await tester.pumpAndSettle();

      expect(tapped, isTrue);
    });

    testWidgets('handles mouse hover for default elevation (Sm)', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraCard(
              onTap: () {},
              child: const SizedBox(width: 100, height: 100),
            ),
          ),
        ),
      );

      final cardFinder = find.byType(AnimatedContainer);
      final container = tester.widget<AnimatedContainer>(cardFinder);
      final decoration = container.decoration as BoxDecoration;
      // Default elevation is Sm
      expect(decoration.boxShadow, ValoraShadows.sm);

      final gesture = await tester.createGesture(kind: PointerDeviceKind.mouse);
      await gesture.addPointer(location: Offset.zero);
      addTearDown(gesture.removePointer);
      await tester.pump();
      await gesture.moveTo(tester.getCenter(find.byType(ValoraCard)));
      await tester.pumpAndSettle();

      final hoveredContainer = tester.widget<AnimatedContainer>(cardFinder);
      final hoveredDecoration = hoveredContainer.decoration as BoxDecoration;
      expect(hoveredDecoration.boxShadow, ValoraShadows.md);
    });

    testWidgets('handles elevationNone (no shadows)', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraCard(
              elevation: ValoraSpacing.elevationNone,
              child: SizedBox(width: 100, height: 100),
            ),
          ),
        ),
      );

      await tester.pumpAndSettle();

      final cardFinder = find.byType(AnimatedContainer);
      final container = tester.widget<AnimatedContainer>(cardFinder);
      final decoration = container.decoration as BoxDecoration;
      expect(decoration.boxShadow, isEmpty);
    });

    testWidgets('handles elevationMd logic', (WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraCard(
              onTap: () {},
              elevation: ValoraSpacing.elevationMd,
              child: const SizedBox(width: 100, height: 100),
            ),
          ),
        ),
      );

      final cardFinder = find.byType(AnimatedContainer);
      final container = tester.widget<AnimatedContainer>(cardFinder);
      final decoration = container.decoration as BoxDecoration;
      expect(decoration.boxShadow, ValoraShadows.md);

      // Hover
      final gesture = await tester.createGesture(kind: PointerDeviceKind.mouse);
      await gesture.addPointer(location: Offset.zero);
      addTearDown(gesture.removePointer);
      await tester.pump();
      await gesture.moveTo(tester.getCenter(find.byType(ValoraCard)));
      await tester.pumpAndSettle();

      final hoveredContainer = tester.widget<AnimatedContainer>(cardFinder);
      final hoveredDecoration = hoveredContainer.decoration as BoxDecoration;
      expect(hoveredDecoration.boxShadow, ValoraShadows.lg);
    });

    testWidgets('handles elevationLg (fallthrough else case)', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraCard(
              onTap: () {},
              elevation: ValoraSpacing.elevationLg, // Or any value > Md
              child: const SizedBox(width: 100, height: 100),
            ),
          ),
        ),
      );

      final cardFinder = find.byType(AnimatedContainer);
      final container = tester.widget<AnimatedContainer>(cardFinder);
      final decoration = container.decoration as BoxDecoration;
      expect(decoration.boxShadow, ValoraShadows.lg);

      // Hover
      final gesture = await tester.createGesture(kind: PointerDeviceKind.mouse);
      await gesture.addPointer(location: Offset.zero);
      addTearDown(gesture.removePointer);
      await tester.pump();
      await gesture.moveTo(tester.getCenter(find.byType(ValoraCard)));
      await tester.pumpAndSettle();

      final hoveredContainer = tester.widget<AnimatedContainer>(cardFinder);
      final hoveredDecoration = hoveredContainer.decoration as BoxDecoration;
      expect(hoveredDecoration.boxShadow, ValoraShadows.xl);
    });

    testWidgets('handles press state (reverts to base shadow)', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraCard(
              onTap: () {},
              child: const SizedBox(width: 100, height: 100),
            ),
          ),
        ),
      );

      final cardFinder = find.byType(AnimatedContainer);

      // Hover first to lift it
      final gesture = await tester.createGesture(kind: PointerDeviceKind.mouse);
      await gesture.addPointer(location: Offset.zero);
      addTearDown(gesture.removePointer);
      await tester.pump();
      await gesture.moveTo(tester.getCenter(find.byType(ValoraCard)));
      await tester.pumpAndSettle();

      var container = tester.widget<AnimatedContainer>(cardFinder);
      var decoration = container.decoration as BoxDecoration;
      expect(decoration.boxShadow, ValoraShadows.md); // Lifted

      // Now press down
      // Use getCenter of ValoraCard to ensure we target it,
      // but interactions like 'startGesture' might also trigger the hit-test warning if looking up via finder.
      // However, startGesture takes a point, not a finder. getCenter takes a finder.
      // We'll target the SizedBox child if we can find it, or just ignore warning here as it's not a 'tap'.
      // Actually, let's find the SizedBox inside.
      await tester.startGesture(tester.getCenter(find.byType(SizedBox)));
      await tester.pumpAndSettle();

      container = tester.widget<AnimatedContainer>(cardFinder);
      decoration = container.decoration as BoxDecoration;
      // Should revert to base (Sm)
      expect(decoration.boxShadow, ValoraShadows.sm);
    });

    testWidgets('uses dark mode shadows', (WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: ThemeData.dark(),
          home: Scaffold(
            body: ValoraCard(
              onTap: () {},
              child: const SizedBox(width: 100, height: 100),
            ),
          ),
        ),
      );

      await tester.pumpAndSettle(); // Allow entrance animations to settle

      final cardFinder = find.byType(AnimatedContainer);
      final container = tester.widget<AnimatedContainer>(cardFinder);
      final decoration = container.decoration as BoxDecoration;
      // Default dark shadow
      expect(decoration.boxShadow, ValoraShadows.smDark);

      // Force disposal and pump to clear animations
      await tester.pumpWidget(const SizedBox());
      await tester.pump();
    });
  });

  group('ValoraButton Tests', () {
    testWidgets('renders label and icon', (WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraButton(
              label: 'Click Me',
              icon: Icons.add,
              onPressed: () {},
            ),
          ),
        ),
      );

      await tester.pumpAndSettle(); // Resolve animations

      expect(find.text('Click Me'), findsOneWidget);
      expect(find.byIcon(Icons.add), findsOneWidget);
    });

    testWidgets('shows loading indicator when isLoading is true', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraButton(
              label: 'Click Me',
              onPressed: () {},
              isLoading: true,
            ),
          ),
        ),
      );

      // Allow button entrance animation (scale) to complete
      // but do not wait for the infinite spinner
      await tester.pump(const Duration(milliseconds: 500));

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(find.text('Click Me'), findsNothing);

      // Force disposal to stop infinite animation and pump to clear
      await tester.pumpWidget(const SizedBox());
      await tester.pump();
    });

    testWidgets('does not call onPressed when isLoading is true', (
      WidgetTester tester,
    ) async {
      bool pressed = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraButton(
              label: 'Click Me',
              onPressed: () => pressed = true,
              isLoading: true,
            ),
          ),
        ),
      );

      await tester.pump(); // Advance one frame

      // Tap the loading indicator key to avoid hit test warning on the animated wrapper
      await tester.tap(find.byKey(const ValueKey('loading')));

      // Pump enough time for the button press animation (scale) to complete
      // but do not settle (as spinner is infinite)
      await tester.pump(const Duration(milliseconds: 500));

      expect(pressed, isFalse);

      // Force disposal and pump to clear
      await tester.pumpWidget(const SizedBox());
      await tester.pump();
    });
  });

  group('ValoraBadge Tests', () {
    testWidgets('renders label and icon', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraBadge(label: 'New', icon: Icons.star),
          ),
        ),
      );

      await tester.pumpAndSettle(); // Allow animations to complete

      expect(find.text('New'), findsOneWidget);
      expect(find.byIcon(Icons.star), findsOneWidget);
    });
  });

  group('ValoraEmptyState Tests', () {
    testWidgets('renders icon, title, and subtitle', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraEmptyState(
              icon: Icons.error,
              title: 'Nothing here',
              subtitle: 'Try adjusting filters',
            ),
          ),
        ),
      );

      await tester.pumpAndSettle(); // Allow animations to complete

      expect(find.byIcon(Icons.error), findsOneWidget);
      expect(find.text('Nothing here'), findsOneWidget);
      expect(find.text('Try adjusting filters'), findsOneWidget);
    });
  });

  group('ValoraChip Tests', () {
    testWidgets('renders label and handles selection', (WidgetTester tester) async {
      bool selected = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraChip(
              label: 'Test Chip',
              isSelected: selected,
              onSelected: (val) => selected = val,
            ),
          ),
        ),
      );

      expect(find.text('Test Chip'), findsOneWidget);
      expect(find.byIcon(Icons.close_rounded), findsNothing);

      await tester.tap(find.text('Test Chip'));
      await tester.pumpAndSettle();
      expect(selected, isTrue);
    });

    testWidgets('shows delete icon and handles deletion', (WidgetTester tester) async {
      bool deleted = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraChip(
              label: 'Filter',
              isSelected: true,
              onSelected: (_) {},
              onDeleted: () => deleted = true,
            ),
          ),
        ),
      );

      expect(find.byIcon(Icons.close_rounded), findsOneWidget);

      await tester.tap(find.byIcon(Icons.close_rounded));
      await tester.pumpAndSettle();
      expect(deleted, isTrue);
    });
  });

  group('ValoraAvatar Tests', () {
    testWidgets('renders initials', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraAvatar(initials: 'JD'),
          ),
        ),
      );

      expect(find.text('JD'), findsOneWidget);
    });

    testWidgets('shows online indicator', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraAvatar(initials: 'JD', showOnlineIndicator: true),
          ),
        ),
      );

      expect(
        find.descendant(
          of: find.byType(ValoraAvatar),
          matching: find.byType(Stack),
        ),
        findsOneWidget,
      );
    });
  });

  group('ValoraSearchField Tests', () {
    testWidgets('renders hint text', (WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraSearchField(controller: TextEditingController()),
          ),
        ),
      );

      expect(find.text('Search...'), findsOneWidget);
    });

    testWidgets('shows clear button when text is entered', (WidgetTester tester) async {
      final controller = TextEditingController();
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraSearchField(controller: controller),
          ),
        ),
      );

      expect(find.byIcon(Icons.close_rounded), findsNothing);

      await tester.enterText(find.byType(TextField), 'Test');
      await tester.pump();

      expect(find.byIcon(Icons.close_rounded), findsOneWidget);
    });

    testWidgets('clears text when clear button is pressed', (WidgetTester tester) async {
      final controller = TextEditingController();
      bool cleared = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraSearchField(
              controller: controller,
              onClear: () => cleared = true,
            ),
          ),
        ),
      );

      await tester.enterText(find.byType(TextField), 'Test');
      await tester.pump();

      await tester.tap(find.byIcon(Icons.close_rounded));
      await tester.pump();

      expect(controller.text, isEmpty);
      expect(cleared, isTrue);
    });
  });

  group('ValoraSlider Tests', () {
    testWidgets('renders slider and handles changes', (WidgetTester tester) async {
      double value = 0.2;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: StatefulBuilder(
              builder: (context, setState) {
                return ValoraSlider(
                  value: value,
                  onChanged: (val) {
                    setState(() => value = val);
                  },
                );
              },
            ),
          ),
        ),
      );

      expect(find.byType(Slider), findsOneWidget);

      await tester.tap(find.byType(Slider));
      await tester.pumpAndSettle();

      expect(value, closeTo(0.5, 0.05));
    });
  });

  group('ValoraSectionHeader Tests', () {
    testWidgets('renders title uppercase', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraSectionHeader(title: 'Settings'),
          ),
        ),
      );

      expect(find.text('SETTINGS'), findsOneWidget);
    });
  });
}
