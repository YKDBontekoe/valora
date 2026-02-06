import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/valora_filter_dialog.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

void main() {
  group('ValoraFilterDialog', () {
    testWidgets('Populates initial values', (WidgetTester tester) async {
      await tester.pumpWidget(MaterialApp(
        home: Scaffold(
          body: ValoraFilterDialog(
            initialMinPrice: 1000,
            initialMaxPrice: 2000,
            initialCity: 'Amsterdam',
            initialSortBy: 'price',
            initialSortOrder: 'asc',
          ),
        ),
      ));
      await tester.pumpAndSettle(); // Wait for entry animations

      // Check text fields contain the values
      expect(find.text('1000'), findsOneWidget);
      expect(find.text('2000'), findsOneWidget);
      expect(find.text('Amsterdam'), findsOneWidget);

      // Check chip selection
      // ValoraChip wraps FilterChip
      // The text "Price: Low to High" is inside a Text widget which is inside ValoraChip.
      // FilterChip is an implementation detail of ValoraChip (or its child).

      // Find the ValoraChip that contains the text
      final valoraChipFinder = find.widgetWithText(ValoraChip, 'Price: Low to High');
      expect(valoraChipFinder, findsOneWidget);

      // Now find the FilterChip inside that ValoraChip
      final filterChipFinder = find.descendant(
        of: valoraChipFinder,
        matching: find.byType(FilterChip),
      );

      // Use findsAtLeastNWidgets because the finder might be less specific than we thought
      // But we just need to verify one is selected.
      // Actually, since there is only one 'Price: Low to High', let's just grab the first filter chip found in that subtree.
      // Sometimes descendants can be tricky if intermediate widgets block traversal.
      // Let's rely on finding by type FilterChip within the whole tree and checking properties.
      // But there are many FilterChips. We need the one with text "Price: Low to High".
      // Let's use find.ancestor to go from text to FilterChip.

      // The text "Price: Low to High" is inside a Text widget which is inside ValoraChip.
      // FilterChip is used internally by ValoraChip.
      // However, ValoraChip might not be using FilterChip directly in the tree structure we expect,
      // or the text is not directly inside the FilterChip in the way find.ancestor expects if there are many layers.

      final priceAscChip = tester.widget<FilterChip>(filterChipFinder);
      expect(priceAscChip.selected, isTrue);
    });

    testWidgets('Clears filters when "Clear All" is pressed', (WidgetTester tester) async {
      await tester.pumpWidget(MaterialApp(
        home: Scaffold(
          body: ValoraFilterDialog(
            initialMinPrice: 1000,
            initialCity: 'Amsterdam',
          ),
        ),
      ));
      await tester.pumpAndSettle(); // Wait for entry animations

      await tester.tap(find.text('Clear All'));
      await tester.pumpAndSettle(); // Wait for animations (chip selection change)

      // Values should be cleared (gone)
      expect(find.text('1000'), findsNothing);
      expect(find.text('Amsterdam'), findsNothing);

      // Newest should be selected (default)
      final chipTextFinder = find.text('Newest');
      await tester.scrollUntilVisible(chipTextFinder, 50.0, scrollable: find.byType(Scrollable));

      final chipFinder = find.ancestor(of: chipTextFinder, matching: find.byType(FilterChip));
      final newestChip = tester.widget<FilterChip>(chipFinder);
      expect(newestChip.selected, isTrue);
    });

    testWidgets('Updates sort selection', (WidgetTester tester) async {
      await tester.pumpWidget(const MaterialApp(
        home: Scaffold(body: ValoraFilterDialog()),
      ));
      await tester.pumpAndSettle(); // Wait for entry animations

      // Scroll to "Price: High to Low"
      final chipTextFinder = find.text('Price: High to Low');
      await tester.scrollUntilVisible(chipTextFinder, 50.0, scrollable: find.byType(Scrollable));
      await tester.pumpAndSettle();

      // Tap "Price: High to Low"
      await tester.tap(chipTextFinder);
      await tester.pumpAndSettle(); // Wait for selection animation

      final filterChip = tester.widget<FilterChip>(find.ancestor(of: chipTextFinder, matching: find.byType(FilterChip)));
      expect(filterChip.selected, isTrue);
    });

    testWidgets('Returns filter values on Apply', (WidgetTester tester) async {
      Map<String, dynamic>? result;

      await tester.pumpWidget(MaterialApp(
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
      ));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      // Find TextField by label 'Min Price'
      // ValoraTextField uses a Column with Text(label) and TextField
      // find.widgetWithText(ValoraTextField, 'Min Price') should work to find the container
      // Then find TextField descendant
      final minPriceField = find.descendant(
        of: find.widgetWithText(ValoraTextField, 'Min Price'),
        matching: find.byType(TextField),
      );
      await tester.enterText(minPriceField, '500');

      final cityField = find.descendant(
        of: find.widgetWithText(ValoraTextField, 'City'),
        matching: find.byType(TextField),
      );
      await tester.enterText(cityField, 'TestCity');

      await tester.tap(find.text('Apply'));
      await tester.pumpAndSettle();

      expect(result, isNotNull);
      expect(result!['minPrice'], 500.0);
      expect(result!['city'], 'TestCity');
    });

    testWidgets('Shows error when Min Price > Max Price', (WidgetTester tester) async {
      await tester.pumpWidget(MaterialApp(
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
      ));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      final minPriceField = find.descendant(
        of: find.widgetWithText(ValoraTextField, 'Min Price'),
        matching: find.byType(TextField),
      );
      await tester.enterText(minPriceField, '1000');

      final maxPriceField = find.descendant(
        of: find.widgetWithText(ValoraTextField, 'Max Price'),
        matching: find.byType(TextField),
      );
      await tester.enterText(maxPriceField, '500');

      await tester.tap(find.text('Apply'));
      await tester.pump(); // Start snackbar animation

      expect(find.text('Min price cannot be greater than Max price'), findsOneWidget);
    });

    testWidgets('Shows error when Min Area > Max Area', (WidgetTester tester) async {
      await tester.pumpWidget(MaterialApp(
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
      ));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      final minAreaField = find.descendant(
        of: find.widgetWithText(ValoraTextField, 'Min'),
        matching: find.byType(TextField),
      ).first; // First 'Min' is for Living Area
      await tester.enterText(minAreaField, '100');

      final maxAreaField = find.descendant(
        of: find.widgetWithText(ValoraTextField, 'Max'),
        matching: find.byType(TextField),
      ).first;
      await tester.enterText(maxAreaField, '50');

      await tester.tap(find.text('Apply'));
      await tester.pump(); // Start snackbar animation

      expect(find.text('Min area cannot be greater than Max area'), findsOneWidget);
    });
  });
}
