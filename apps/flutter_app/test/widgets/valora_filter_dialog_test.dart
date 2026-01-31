import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/valora_filter_dialog.dart';

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

      expect(find.widgetWithText(TextField, '1000'), findsOneWidget);
      expect(find.widgetWithText(TextField, '2000'), findsOneWidget);
      expect(find.widgetWithText(TextField, 'Amsterdam'), findsOneWidget);

      final priceAscChip = tester.widget<ChoiceChip>(find.widgetWithText(ChoiceChip, 'Price: Low to High'));
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

      await tester.tap(find.text('Clear All'));
      await tester.pump();

      expect(find.widgetWithText(TextField, '1000'), findsNothing);
      expect(find.widgetWithText(TextField, 'Amsterdam'), findsNothing);

      final newestChip = tester.widget<ChoiceChip>(find.widgetWithText(ChoiceChip, 'Newest'));
      expect(newestChip.selected, isTrue); // Defaults back to date desc
    });

    testWidgets('Updates sort selection', (WidgetTester tester) async {
      await tester.pumpWidget(const MaterialApp(
        home: Scaffold(body: ValoraFilterDialog()),
      ));

      await tester.tap(find.widgetWithText(ChoiceChip, 'Price: High to Low'));
      await tester.pump();

      final chip = tester.widget<ChoiceChip>(find.widgetWithText(ChoiceChip, 'Price: High to Low'));
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

      await tester.tap(find.text('Open'));
      await tester.pumpAndSettle();

      await tester.enterText(find.widgetWithText(TextField, 'Min'), '500');
      await tester.enterText(find.widgetWithText(TextField, 'City'), 'TestCity');

      await tester.tap(find.text('Apply'));
      await tester.pumpAndSettle();

      expect(result, isNotNull);
      expect(result!['minPrice'], 500.0);
      expect(result!['city'], 'TestCity');
    });
  });
}
