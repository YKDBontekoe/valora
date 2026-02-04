import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/models/listing_filter.dart';
import 'package:valora_app/models/listing_response.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/screens/search_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/widgets/valora_filter_dialog.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

@GenerateMocks([ApiService, FavoritesProvider])
import 'search_screen_test.mocks.dart';

void main() {
  late MockApiService mockApiService;
  late MockFavoritesProvider mockFavoritesProvider;

  setUp(() {
    mockApiService = MockApiService();
    mockFavoritesProvider = MockFavoritesProvider();
    when(mockFavoritesProvider.isFavorite(any)).thenReturn(false);
  });

  Widget createWidgetUnderTest() {
    return MultiProvider(
      providers: [
        Provider<ApiService>.value(value: mockApiService),
        ChangeNotifierProvider<FavoritesProvider>.value(value: mockFavoritesProvider),
      ],
      child: const MaterialApp(
        home: SearchScreen(),
      ),
    );
  }

  testWidgets('SearchScreen shows empty state initially', (WidgetTester tester) async {
    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pump();
    expect(find.text('Find your home'), findsOneWidget);

    // Cleanup timers
    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });

  testWidgets('SearchScreen shows loading and results on search', (WidgetTester tester) async {
    final listings = [
      Listing(id: '1', fundaId: 'f1', address: 'Test St 1', price: 100000, bedrooms: 2, livingAreaM2: 50),
    ];
    when(mockApiService.getListings(any)).thenAnswer((_) async {
      await Future.delayed(const Duration(milliseconds: 100));
      return ListingResponse(items: listings, totalCount: 1, pageIndex: 1, totalPages: 1, hasPreviousPage: false, hasNextPage: false);
    });

    await tester.pumpWidget(createWidgetUnderTest());

    // Enter search term
    await tester.enterText(find.byType(TextFormField), 'Amsterdam');
    await tester.pump(); // Start debounce timer
    await tester.pump(const Duration(milliseconds: 500)); // Trigger search

    expect(find.text('Searching...'), findsOneWidget);
    await tester.pumpAndSettle();

    expect(find.text('Test St 1'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });

  testWidgets('SearchScreen handles empty results with query', (WidgetTester tester) async {
    when(mockApiService.getListings(any)).thenAnswer((_) async =>
      ListingResponse(items: [], totalCount: 0, pageIndex: 1, totalPages: 0, hasPreviousPage: false, hasNextPage: false));

    await tester.pumpWidget(createWidgetUnderTest());

    await tester.enterText(find.byType(TextFormField), 'Nowhere');
    await tester.pump(const Duration(milliseconds: 500));
    await tester.pumpAndSettle();

    expect(find.text('No results found'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });

  testWidgets('SearchScreen handles API error and retry', (WidgetTester tester) async {
    when(mockApiService.getListings(any)).thenThrow(ServerException('Failed'));

    await tester.pumpWidget(createWidgetUnderTest());

    await tester.enterText(find.byType(TextFormField), 'Error');
    await tester.pump(const Duration(milliseconds: 500));
    await tester.pumpAndSettle();

    expect(find.text('Search Failed'), findsOneWidget);
    expect(find.text('Failed'), findsOneWidget);

    // Test retry
    when(mockApiService.getListings(any)).thenAnswer((_) async =>
      ListingResponse(items: [], totalCount: 0, pageIndex: 1, totalPages: 0, hasPreviousPage: false, hasNextPage: false));

    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();

    expect(find.text('No results found'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });

  testWidgets('SearchScreen handles pagination', (WidgetTester tester) async {
    final page1 = List.generate(20, (i) => Listing(id: '$i', fundaId: 'f$i', address: 'Addr $i', price: 100000));
    final page2 = [Listing(id: '20', fundaId: 'f20', address: 'Addr 20', price: 100000)];

    when(mockApiService.getListings(argThat(predicate<ListingFilter>((f) => f.page == 1))))
      .thenAnswer((_) async => ListingResponse(items: page1, totalCount: 21, pageIndex: 1, totalPages: 2, hasPreviousPage: false, hasNextPage: true));

    when(mockApiService.getListings(argThat(predicate<ListingFilter>((f) => f.page == 2))))
      .thenAnswer((_) async => ListingResponse(items: page2, totalCount: 21, pageIndex: 2, totalPages: 2, hasPreviousPage: true, hasNextPage: false));

    await tester.pumpWidget(createWidgetUnderTest());

    await tester.enterText(find.byType(TextFormField), 'Paged');
    await tester.pump(const Duration(milliseconds: 500));
    await tester.pumpAndSettle();

    expect(find.text('Addr 0'), findsOneWidget);

    // Scroll to bottom
    await tester.drag(find.byType(CustomScrollView), const Offset(0, -3000));
    await tester.pump(); // Trigger scroll notification logic
    await tester.pump(); // Process load more future
    await tester.pumpAndSettle();

    expect(find.text('Addr 20'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });

  testWidgets('SearchScreen handles pagination error gracefully', (WidgetTester tester) async {
    final page1 = List.generate(20, (i) => Listing(id: '$i', fundaId: 'f$i', address: 'Addr $i', price: 100000));

    when(mockApiService.getListings(argThat(predicate<ListingFilter>((f) => f.page == 1))))
      .thenAnswer((_) async => ListingResponse(items: page1, totalCount: 21, pageIndex: 1, totalPages: 2, hasPreviousPage: false, hasNextPage: true));

    when(mockApiService.getListings(argThat(predicate<ListingFilter>((f) => f.page == 2))))
      .thenThrow(ServerException('Load more failed'));

    await tester.pumpWidget(createWidgetUnderTest());

    await tester.enterText(find.byType(TextFormField), 'PagedError');
    await tester.pump(const Duration(milliseconds: 500));
    await tester.pumpAndSettle();

    // Scroll to bottom
    await tester.drag(find.byType(CustomScrollView), const Offset(0, -3000));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));
    await tester.pump();

    expect(find.byType(SnackBar), findsOneWidget);
    expect(find.text('Failed to load more items'), findsOneWidget);
    // Items should still be there (check one that is likely visible at bottom or scroll up)
    // Addr 19 should be near bottom
    expect(find.text('Addr 19'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    // Use pump with duration instead of settle to avoid infinite timer issues during teardown
    await tester.pump(const Duration(seconds: 1));
  });

  testWidgets('SearchScreen uses filter dialog results', (WidgetTester tester) async {
    when(mockApiService.getListings(any)).thenAnswer((_) async =>
      ListingResponse(items: [], totalCount: 0, pageIndex: 1, totalPages: 0, hasPreviousPage: false, hasNextPage: false));

    await tester.pumpWidget(createWidgetUnderTest());

    await tester.tap(find.byIcon(Icons.tune_rounded));
    await tester.pumpAndSettle();

    expect(find.byType(ValoraFilterDialog), findsOneWidget);

    // Find the text field inside the dialog that corresponds to City
    // ValoraTextField structure is Column(Text(label), TextFormField)
    // We can find the TextFormField that is a descendant of ValoraTextField with label 'City'
    final cityField = find.descendant(
      of: find.widgetWithText(Column, 'City'), // ValoraTextField uses Column
      matching: find.byType(TextFormField),
    );

    // Fallback: If structure is different, find by hint or just type into the first one if unique type
    // Assuming City is the only text field or distinct
    // Better: ValoraFilterDialog likely has keys or specific structure.
    // Let's assume finding by Type inside the dialog is safer if we target the specific field.
    // Given the difficulty, let's use key if possible, but we can't change source easily now.
    // Let's try finding ValoraTextField by label text, then finding child TextFormField.

    // Try to find the TextField directly if it has a key or distinctive property.
    // Or just find by hint if available. ValoraFilterDialog source isn't visible here but usually has hints.
    // Assuming standard layout:
    await tester.enterText(find.byType(TextFormField).at(2), 'Utrecht'); // Index 0 is search bar (behind modal?), 1 is min price?, 2 is city? Risky.

    // Actually, let's rely on finding the widget that contains 'City' text and is a TextField? No.
    // Let's verify with `flutter analyze` locally if ValoraFilterDialog exposes keys.
    // Safe bet: The dialog is on top. Find TextFormField inside ValoraFilterDialog.
    // ValoraFilterDialog has Min Price, Max Price, City.
    // City is likely the 3rd or specific one.
    // Use last found text form field as a heuristic if specific finder fails,
    // or better: iterate to find the one with 'City' label.
    // The previous error 'Too many elements' means find.widgetWithText(Column, 'City') matched multiple.
    // ValoraTextField wraps content in Column.

    // Find the ValoraTextField with label 'City'
    final cityValoraField = find.byWidgetPredicate(
      (w) => w is ValoraTextField && w.label == 'City'
    );
    final cityTextField = find.descendant(
      of: cityValoraField,
      matching: find.byType(TextFormField),
    );
    await tester.enterText(cityTextField, 'Utrecht');

    await tester.tap(find.text('Apply'));
    await tester.pumpAndSettle();

    // Verify call with specific filter
    // Relaxed verification to debug logic or catch partial match
    final captured = verify(mockApiService.getListings(captureAny)).captured;
    expect(captured.length, 1);
    final filter = captured.first as ListingFilter;
    expect(filter.city, 'Utrecht');

    // Check filter chip appears
    expect(find.text('City: Utrecht'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });

  testWidgets('SearchScreen clears query', (WidgetTester tester) async {
    // This tests the logic where query is cleared and filters are also empty -> return to initial state
    when(mockApiService.getListings(any)).thenAnswer((_) async =>
      ListingResponse(items: [], totalCount: 0, pageIndex: 1, totalPages: 0, hasPreviousPage: false, hasNextPage: false));

    await tester.pumpWidget(createWidgetUnderTest());

    await tester.enterText(find.byType(TextFormField), 'Query');
    await tester.pump(const Duration(milliseconds: 500));
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextFormField), ''); // Clear
    await tester.pump(const Duration(milliseconds: 500));
    await tester.pumpAndSettle();

    expect(find.text('Find your home'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });
}
