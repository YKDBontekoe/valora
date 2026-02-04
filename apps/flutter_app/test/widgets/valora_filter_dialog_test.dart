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
      final chipFinder = find.widgetWithText(FilterChip, 'Price: Low to High');
      expect(chipFinder, findsOneWidget);
      final priceAscChip = tester.widget<FilterChip>(chipFinder);
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
      final chipFinder = find.widgetWithText(FilterChip, 'Newest');
      final newestChip = tester.widget<FilterChip>(chipFinder);
      expect(newestChip.selected, isTrue);
    });

    testWidgets('Updates sort selection', (WidgetTester tester) async {
      await tester.pumpWidget(const MaterialApp(
        home: Scaffold(body: ValoraFilterDialog()),
      ));
      await tester.pumpAndSettle(); // Wait for entry animations

      // Scroll to "Price: High to Low"
      final chipFinder = find.widgetWithText(FilterChip, 'Price: High to Low');
      await tester.ensureVisible(chipFinder);
      await tester.pumpAndSettle();

      // Tap "Price: High to Low"
      await tester.tap(chipFinder);
      await tester.pumpAndSettle(); // Wait for selection animation

      final chip = tester.widget<FilterChip>(chipFinder);
      expect(chip.selected, isTrue);
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
  });
}
