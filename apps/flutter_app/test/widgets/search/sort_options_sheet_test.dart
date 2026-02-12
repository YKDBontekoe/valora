import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/providers/search_listings_provider.dart';
import 'package:valora_app/widgets/search/sort_options_sheet.dart';

@GenerateNiceMocks([MockSpec<SearchListingsProvider>()])
import 'sort_options_sheet_test.mocks.dart';

void main() {
  late MockSearchListingsProvider mockProvider;

  setUp(() {
    mockProvider = MockSearchListingsProvider();
  });

  Widget buildWidget() {
    return MaterialApp(
      home: Scaffold(
        body: SortOptionsSheet(provider: mockProvider, onClose: () {}),
      ),
    );
  }

  testWidgets('SortOptionsSheet renders all sort options', (tester) async {
    await tester.pumpWidget(buildWidget());

    expect(find.text('Sort By'), findsOneWidget);
    expect(find.text('Newest'), findsOneWidget);
    expect(find.text('Price: Low to High'), findsOneWidget);
    expect(find.text('Price: High to Low'), findsOneWidget);
    expect(find.text('Area: Small to Large'), findsOneWidget);
    expect(find.text('Area: Large to Small'), findsOneWidget);
    expect(find.text('Composite Score: High to Low'), findsOneWidget);
    expect(find.text('Safety Score: High to Low'), findsOneWidget);
  });

  testWidgets('SortOptionsSheet marks selected option', (tester) async {
    when(mockProvider.sortBy).thenReturn('price');
    when(mockProvider.sortOrder).thenReturn('asc');

    await tester.pumpWidget(buildWidget());

    // Selected option has check icon and bold style (simplified check for icon here)
    final selectedOption = find.widgetWithText(ListTile, 'Price: Low to High');
    expect(
      find.descendant(
        of: selectedOption,
        matching: find.byIcon(Icons.check_rounded),
      ),
      findsOneWidget,
    );
  });

  testWidgets('SortOptionsSheet applies filter on selection', (tester) async {
    await tester.pumpWidget(buildWidget());

    await tester.tap(find.text('Area: Large to Small'));

    verify(
      mockProvider.applyFilters(
        minPrice: anyNamed('minPrice'),
        maxPrice: anyNamed('maxPrice'),
        city: anyNamed('city'),
        minBedrooms: anyNamed('minBedrooms'),
        minLivingArea: anyNamed('minLivingArea'),
        maxLivingArea: anyNamed('maxLivingArea'),
        minSafetyScore: anyNamed('minSafetyScore'),
        minCompositeScore: anyNamed('minCompositeScore'),
        sortBy: 'livingarea',
        sortOrder: 'desc',
      ),
    ).called(1);
  });
}
