import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/valora_filter_dialog.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

void main() {
  group('ValoraFilterDialog', () {
    testWidgets('Populates initial values', (WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraFilterDialog(
              initialMinPrice: 1000,
              initialMaxPrice: 2000,
              initialCity: 'Amsterdam',
              initialSortBy: 'price',
              initialSortOrder: 'asc',
            ),
          ),
        ),
      );
      await tester.pumpAndSettle(); // Wait for entry animations

      // Check text fields contain the values
      expect(find.text('1000'), findsOneWidget);
      expect(find.text('2000'), findsOneWidget);
      expect(find.text('Amsterdam'), findsOneWidget);

      // Check chip selection
      final chipFinder = find
          .widgetWithText(ValoraChip, 'Price: Low to High')
          .first;

      // Scroll to ensure visibility before asserting
      final scrollableFinder = find
          .descendant(
            of: find.byType(ValoraFilterDialog),
            matching: find.byType(Scrollable),
          )
          .first;

      await tester.scrollUntilVisible(chipFinder, 50.0, scrollable: scrollableFinder);
      await tester.pumpAndSettle();

      expect(chipFinder, findsOneWidget);
      final priceAscChip = tester.widget<ValoraChip>(chipFinder);
      expect(priceAscChip.isSelected, isTrue);
    });

    testWidgets('Clears filters when "Clear All" is pressed', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraFilterDialog(
              initialMinPrice: 1000,
              initialCity: 'Amsterdam',
            ),
          ),
        ),
      );
      await tester.pumpAndSettle(); // Wait for entry animations

      await tester.tap(find.text('Clear All'));
      await tester
          .pumpAndSettle(); // Wait for animations (chip selection change)

      // Values should be cleared (gone)
      expect(find.text('1000'), findsNothing);
      expect(find.text('Amsterdam'), findsNothing);

      // Newest should be selected (default)
      // Use .first to handle potential duplicates
      final chipFinder = find.widgetWithText(ValoraChip, 'Newest').first;

      // Find the Main Scrollable (first one inside ValoraFilterDialog)
      // This avoids matching Scrollables inside TextFields
      final scrollableFinder = find
          .descendant(
            of: find.byType(ValoraFilterDialog),
            matching: find.byType(Scrollable),
          )
          .first;

      await tester.scrollUntilVisible(
        chipFinder,
        50.0,
        scrollable: scrollableFinder,
      );
      await tester.pumpAndSettle();

      final newestChip = tester.widget<ValoraChip>(chipFinder);
      expect(newestChip.isSelected, isTrue);
    });

    testWidgets('Updates sort selection', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(home: Scaffold(body: ValoraFilterDialog())),
      );
      await tester.pumpAndSettle(); // Wait for entry animations

      // Scroll to "Price: High to Low"
      final chipFinder = find
          .widgetWithText(ValoraChip, 'Price: High to Low')
          .first;

      final scrollableFinder = find
          .descendant(
            of: find.byType(ValoraFilterDialog),
            matching: find.byType(Scrollable),
          )
          .first;

      await tester.scrollUntilVisible(
        chipFinder,
        50.0,
        scrollable: scrollableFinder,
      );
      await tester.pumpAndSettle();

      // Tap "Price: High to Low"
      await tester.ensureVisible(chipFinder); // Ensure visible before tap
      await tester.tap(chipFinder, warnIfMissed: false);
      await tester.pumpAndSettle(); // Wait for selection animation

      final valoraChip = tester.widget<ValoraChip>(chipFinder);
      expect(valoraChip.isSelected, isTrue);
    });

    testWidgets('Returns filter values on Apply', (WidgetTester tester) async {
      Map<String, dynamic>? result;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () async {
                  result = await showDialog(
                    context: context,
                    builder: (_) => const ValoraFilterDialog(),
                  );
                },
                child: const Text('Open'),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      // Find TextField by label 'Min Price'
      final minPriceContainer = find.widgetWithText(
        ValoraTextField,
        'Min Price',
      );
      final minPriceField = find.descendant(
        of: minPriceContainer,
        matching: find.byType(TextField),
      );
      await tester.enterText(minPriceField, '500');

      final cityContainer = find.widgetWithText(ValoraTextField, 'City');
      final cityField = find.descendant(
        of: cityContainer,
        matching: find.byType(TextField),
      );
      await tester.enterText(cityField, 'TestCity');

      await tester.tap(find.text('Apply'));
      await tester.pumpAndSettle();

      expect(result, isNotNull);
      expect(result!['minPrice'], 500.0);
      expect(result!['city'], 'TestCity');
    });

    testWidgets('Shows error when Min Price > Max Price', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () => showDialog(
                  context: context,
                  builder: (_) => const ValoraFilterDialog(),
                ),
                child: const Text('Open'),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      final minPriceContainer = find.widgetWithText(
        ValoraTextField,
        'Min Price',
      );
      final minPriceField = find.descendant(
        of: minPriceContainer,
        matching: find.byType(TextField),
      );
      await tester.enterText(minPriceField, '1000');

      final maxPriceContainer = find.widgetWithText(
        ValoraTextField,
        'Max Price',
      );
      final maxPriceField = find.descendant(
        of: maxPriceContainer,
        matching: find.byType(TextField),
      );
      await tester.enterText(maxPriceField, '500');

      await tester.tap(find.text('Apply'));
      await tester.pump(); // Start snackbar animation

      expect(
        find.text('Min price cannot be greater than Max price'),
        findsOneWidget,
      );
    });

    testWidgets('Shows error when Min Area > Max Area', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => ElevatedButton(
                onPressed: () => showDialog(
                  context: context,
                  builder: (_) => const ValoraFilterDialog(),
                ),
                child: const Text('Open'),
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      final minFields = find.widgetWithText(ValoraTextField, 'Min');
      final minAreaContainer = minFields.last;
      final minAreaField = find.descendant(
        of: minAreaContainer,
        matching: find.byType(TextField),
      );
      await tester.enterText(minAreaField, '100');

      final maxFields = find.widgetWithText(ValoraTextField, 'Max');
      final maxAreaContainer = maxFields.last;
      final maxAreaField = find.descendant(
        of: maxAreaContainer,
        matching: find.byType(TextField),
      );
      await tester.enterText(maxAreaField, '50');

      await tester.tap(find.text('Apply'));
      await tester.pump(); // Start snackbar animation

      expect(
        find.text('Min area cannot be greater than Max area'),
        findsOneWidget,
      );
    });
  });
}
