import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/providers/search_listings_provider.dart';
import 'package:valora_app/widgets/search/active_filters_list.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

@GenerateNiceMocks([MockSpec<SearchListingsProvider>()])
import 'active_filters_list_test.mocks.dart';

void main() {
  late MockSearchListingsProvider mockProvider;

  setUp(() {
    mockProvider = MockSearchListingsProvider();
  });

  Widget buildWidget() {
    return MaterialApp(
      home: Scaffold(
        body: ActiveFiltersList(
          provider: mockProvider,
          onFilterTap: () {},
          onSortTap: () {},
        ),
      ),
    );
  }

  testWidgets('ActiveFiltersList renders nothing when no filters active', (
    tester,
  ) async {
    when(mockProvider.hasActiveFiltersOrSort).thenReturn(false);

    await tester.pumpWidget(buildWidget());

    expect(find.byType(ListView), findsNothing);
  });

  testWidgets('ActiveFiltersList renders active filters', (tester) async {
    when(mockProvider.hasActiveFiltersOrSort).thenReturn(true);
    when(mockProvider.minPrice).thenReturn(100000);
    when(mockProvider.maxPrice).thenReturn(500000);
    when(mockProvider.city).thenReturn('Amsterdam');

    await tester.pumpWidget(buildWidget());
    await tester.pumpAndSettle(); // Wait for any entrance animations

    // Note: Currency formatting depends on locale and implementation.
    // Assuming standard format: €100.000, €500.000 based on nl_NL locale in CurrencyFormatter
    // The test failure showed it couldn't find "Price: €100k - €500k", likely because the formatter uses standard locale.
    // Let's use a more flexible matcher or check the actual implementation.
    // The implementation uses NumberFormat.currency(locale: 'nl_NL', symbol: '€', decimalDigits: 0).
    // So 100000 -> €100.000
    // 500000 -> €500.000
    // The previous test expectation '€100k' was incorrect based on the read file.

    expect(find.text('Price: €100.000 - €500.000'), findsOneWidget);
    expect(find.text('City: Amsterdam'), findsOneWidget);
  });

  testWidgets('ActiveFiltersList calls clear filter methods on delete', (
    tester,
  ) async {
    when(mockProvider.hasActiveFiltersOrSort).thenReturn(true);
    when(mockProvider.minBedrooms).thenReturn(2);

    await tester.pumpWidget(buildWidget());

    final chip = find.widgetWithText(ValoraChip, '2+ Beds');
    expect(chip, findsOneWidget);

    await tester.tap(
      find.descendant(of: chip, matching: find.byIcon(Icons.close_rounded)),
    );

    await tester.pumpAndSettle(); // Wait for animations

    verify(mockProvider.clearBedroomsFilter()).called(1);
  });

  testWidgets('ActiveFiltersList handles sort chip correctly', (tester) async {
    when(mockProvider.hasActiveFiltersOrSort).thenReturn(true);
    when(mockProvider.isSortActive).thenReturn(true);
    when(mockProvider.sortBy).thenReturn('price');
    when(mockProvider.sortOrder).thenReturn('asc');

    bool sortTapped = false;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ActiveFiltersList(
            provider: mockProvider,
            onFilterTap: () {},
            onSortTap: () => sortTapped = true,
          ),
        ),
      ),
    );

    expect(find.text('Price: Low to High'), findsOneWidget);

    await tester.tap(find.text('Price: Low to High'));
    await tester.pumpAndSettle(); // Wait for ink splash/animations

    expect(sortTapped, isTrue);
  });

  testWidgets('ActiveFiltersList clear all button works', (tester) async {
    when(mockProvider.hasActiveFiltersOrSort).thenReturn(true);

    await tester.pumpWidget(buildWidget());

    final clearAllBtn = find.byTooltip('Clear Filters');
    expect(clearAllBtn, findsOneWidget);

    await tester.tap(clearAllBtn);

    verify(mockProvider.clearFilters()).called(1);
  });
}
