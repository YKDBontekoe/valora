import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

void main() {
  group('ValoraCard Tests', () {
    testWidgets('renders child content', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraCard(
              child: Text('Card Content'),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Card Content'), findsOneWidget);
    });

    testWidgets('calls onTap when tapped', (tester) async {
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
      await tester.pumpAndSettle();

      await tester.tap(find.text('Tap Me'));
      await tester.pumpAndSettle();

      expect(tapped, isTrue);
    });
  });

  group('ValoraButton Tests', () {
    testWidgets('renders label', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraButton(
              label: 'Click Me',
              onPressed: () {},
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Click Me'), findsOneWidget);
    });

    testWidgets('shows loading indicator when isLoading is true', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraButton(
              label: 'Loading...',
              isLoading: true,
              onPressed: () {},
            ),
          ),
        ),
      );
      // Don't use pumpAndSettle because CircularProgressIndicator never settles
      await tester.pump();

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(find.text('Loading...'), findsNothing);

      // Unmount the widget to dispose the infinite animation timer
      await tester.pumpWidget(const SizedBox());
      await tester.pumpAndSettle();
    });

    testWidgets('calls onPressed when tapped', (tester) async {
      bool pressed = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraButton(
              label: 'Press Me',
              onPressed: () => pressed = true,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.text('Press Me'));
      await tester.pumpAndSettle();

      expect(pressed, isTrue);
    });

    testWidgets('does not call onPressed when isLoading is true', (tester) async {
      bool pressed = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraButton(
              label: 'Loading...',
              isLoading: true,
              onPressed: () => pressed = true,
            ),
          ),
        ),
      );
      // Don't use pumpAndSettle because CircularProgressIndicator never settles
      await tester.pump();

      await tester.tap(find.byType(ValoraButton));
      await tester.pump();

      expect(pressed, isFalse);

      // Unmount the widget to dispose the infinite animation timer
      await tester.pumpWidget(const SizedBox());
      await tester.pumpAndSettle();
    });

    testWidgets('renders different variants', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Column(
              children: [
                ValoraButton(
                  label: 'Primary',
                  variant: ValoraButtonVariant.primary,
                  onPressed: () {},
                ),
                ValoraButton(
                  label: 'Secondary',
                  variant: ValoraButtonVariant.secondary,
                  onPressed: () {},
                ),
                ValoraButton(
                  label: 'Outline',
                  variant: ValoraButtonVariant.outline,
                  onPressed: () {},
                ),
                ValoraButton(
                  label: 'Ghost',
                  variant: ValoraButtonVariant.ghost,
                  onPressed: () {},
                ),
              ],
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Primary'), findsOneWidget);
      expect(find.text('Secondary'), findsOneWidget);
      expect(find.text('Outline'), findsOneWidget);
      expect(find.text('Ghost'), findsOneWidget);
    });
  });

  group('ValoraBadge Tests', () {
    testWidgets('renders label', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraBadge(
              label: 'New',
            ),
          ),
        ),
      );

      await tester.pumpAndSettle(); // Allow animations to finish

      expect(find.text('New'), findsOneWidget);
    });

    testWidgets('renders icon when provided', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraBadge(
              label: 'Verified',
              icon: Icons.check,
            ),
          ),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.byIcon(Icons.check), findsOneWidget);
    });
  });

  group('ValoraTextField Tests', () {
     testWidgets('renders label and hint', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraTextField(
              label: 'Email',
              hint: 'Enter your email',
            ),
          ),
        ),
      );

      expect(find.text('Email'), findsOneWidget);
      expect(find.text('Enter your email'), findsOneWidget);
    });

    testWidgets('updates focused state', (tester) async {
       await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraTextField(
              label: 'Email',
            ),
          ),
        ),
      );

      await tester.tap(find.byType(TextFormField));
      await tester.pump();

      // Verify visual feedback if possible, or just that it doesn't crash
      expect(find.text('Email'), findsOneWidget);
    });
  });
}
