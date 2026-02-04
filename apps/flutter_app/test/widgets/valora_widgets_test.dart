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

  group('ValoraEmptyState Tests', () {
    testWidgets('renders icon, title, and subtitle', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraEmptyState(
              icon: Icons.info,
              title: 'Empty Title',
              subtitle: 'Empty Subtitle',
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.byIcon(Icons.info), findsOneWidget);
      expect(find.text('Empty Title'), findsOneWidget);
      expect(find.text('Empty Subtitle'), findsOneWidget);
    });

    testWidgets('renders action button', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraEmptyState(
              icon: Icons.info,
              title: 'Empty Title',
              action: ElevatedButton(onPressed: () {}, child: const Text('Retry')),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Retry'), findsOneWidget);
    });
  });

  group('ValoraLoadingIndicator Tests', () {
    testWidgets('renders circular progress indicator', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraLoadingIndicator(),
          ),
        ),
      );
      await tester.pump(); // Infinite animation

      expect(find.byType(CircularProgressIndicator), findsOneWidget);

      await tester.pumpWidget(const SizedBox());
      await tester.pumpAndSettle();
    });

    testWidgets('renders message if provided', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraLoadingIndicator(message: 'Loading data...'),
          ),
        ),
      );
      await tester.pump();

      expect(find.text('Loading data...'), findsOneWidget);

      await tester.pumpWidget(const SizedBox());
      await tester.pumpAndSettle();
    });
  });

  group('ValoraPrice Tests', () {
    testWidgets('formats price correctly', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraPrice(price: 123456),
          ),
        ),
      );

      // Expected format based on code: € 123.456
      expect(find.textContaining('123.456'), findsOneWidget);
    });

    testWidgets('renders different sizes', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: Column(
              children: [
                ValoraPrice(price: 100, size: ValoraPriceSize.small),
                ValoraPrice(price: 200, size: ValoraPriceSize.medium),
                ValoraPrice(price: 300, size: ValoraPriceSize.large),
              ],
            ),
          ),
        ),
      );

      expect(find.textContaining('100'), findsOneWidget);
      expect(find.textContaining('200'), findsOneWidget);
      expect(find.textContaining('300'), findsOneWidget);
    });
  });

  group('ValoraShimmer Tests', () {
    testWidgets('renders container with correct size', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: ValoraShimmer(width: 100, height: 50),
          ),
        ),
      );
      await tester.pump();

      final container = find.byType(Container).first;
      final size = tester.getSize(container);
      expect(size.width, 100);
      expect(size.height, 50);

      await tester.pumpWidget(const SizedBox());
      await tester.pumpAndSettle();
    });
  });

  group('ValoraChip Tests', () {
    testWidgets('renders label and selection state', (tester) async {
      bool selected = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: StatefulBuilder(
              builder: (context, setState) {
                return ValoraChip(
                  label: 'Filter',
                  isSelected: selected,
                  onSelected: (val) => setState(() => selected = val),
                );
              },
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Filter'), findsOneWidget);

      await tester.tap(find.text('Filter'));
      await tester.pumpAndSettle();

      expect(selected, isTrue);
    });
  });

  group('ValoraDialog Tests', () {
    testWidgets('renders title, content, and actions', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraDialog(
              title: 'Dialog Title',
              actions: [
                TextButton(onPressed: () {}, child: const Text('OK')),
              ],
              child: const Text('Dialog Content'),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Dialog Title'), findsOneWidget);
      expect(find.text('Dialog Content'), findsOneWidget);
      expect(find.text('OK'), findsOneWidget);
    });
  });
}
