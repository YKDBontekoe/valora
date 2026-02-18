import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/models/listing_response.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/providers/search_listings_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/property_photo_service.dart';
import 'package:valora_app/widgets/search/search_results_list.dart';
import 'package:valora_app/widgets/valora_listing_card_horizontal.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

@GenerateMocks([ApiService, PropertyPhotoService, FavoritesProvider])
import 'search_results_list_test.mocks.dart';

void main() {
  late MockApiService mockApiService;
  late MockPropertyPhotoService mockPropertyPhotoService;
  late MockFavoritesProvider mockFavoritesProvider;
  late SearchListingsProvider searchProvider;

  setUp(() {
    mockApiService = MockApiService();
    mockPropertyPhotoService = MockPropertyPhotoService();
    mockFavoritesProvider = MockFavoritesProvider();

    when(mockFavoritesProvider.favorites).thenReturn([]);
    when(mockFavoritesProvider.isFavorite(any)).thenReturn(false);

    searchProvider = SearchListingsProvider(
      apiService: mockApiService,
      propertyPhotoService: mockPropertyPhotoService,
    );
  });

  Widget createWidget({required Function(Listing) onListingTap}) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<SearchListingsProvider>.value(
          value: searchProvider,
        ),
        ChangeNotifierProvider<FavoritesProvider>.value(
          value: mockFavoritesProvider,
        ),
      ],
      child: MaterialApp(
        home: Scaffold(
          body: CustomScrollView(
            slivers: [SearchResultsList(onListingTap: onListingTap)],
          ),
        ),
      ),
    );
  }

  testWidgets('SearchResultsList renders nothing when empty', (
    WidgetTester tester,
  ) async {
    // Initial state is empty
    await tester.pumpWidget(createWidget(onListingTap: (_) {}));
    await tester.pumpAndSettle();

    // Should find no listing cards
    expect(find.byType(ValoraListingCardHorizontal), findsNothing);
  });

  testWidgets('SearchResultsList renders listings', (
    WidgetTester tester,
  ) async {
    final listing = Listing(
      id: '1',
      fundaId: '1',
      address: 'Test Address',
      city: 'Test City',
      price: 500000,
    );

    // Mock API to return a listing
    when(mockApiService.getListings(any)).thenAnswer((_) async => ListingResponse(
      items: [listing],
      pageIndex: 1,
      totalPages: 1,
      totalCount: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    ));

    await tester.pumpWidget(createWidget(onListingTap: (_) {}));

    // Trigger load
    searchProvider.setQuery('test');
    await searchProvider.refresh();
    await tester.pumpAndSettle();

    expect(find.byType(ValoraListingCardHorizontal), findsOneWidget);
    expect(find.text('Test Address'), findsOneWidget);

    await tester.pumpAndSettle();
  });

  testWidgets('SearchResultsList renders loading indicator when loading more', (
    WidgetTester tester,
  ) async {
    final listing = Listing(
      id: '1',
      fundaId: '1',
      address: 'Test Address',
      city: 'Test City',
      price: 500000,
    );

    // Mock API to return a listing and next page
    when(mockApiService.getListings(any)).thenAnswer((_) async => ListingResponse(
      items: [listing],
      pageIndex: 1,
      totalPages: 2,
      totalCount: 20,
      hasNextPage: true,
      hasPreviousPage: false,
    ));

    await tester.pumpWidget(createWidget(onListingTap: (_) {}));

    // Initial load
    searchProvider.setQuery('test');
    await searchProvider.refresh();
    await tester.pumpAndSettle();

    // Trigger load more but delay response
    when(mockApiService.getListings(any)).thenAnswer((_) async {
      await Future<void>.delayed(const Duration(seconds: 1));
      return ListingResponse(items: [], pageIndex: 2, totalPages: 2, totalCount: 20, hasNextPage: false, hasPreviousPage: true);
    });

    searchProvider.loadMore();
    await tester.pump(); // Start loading more

    // Should see loading indicator at bottom
    expect(find.byType(ValoraLoadingIndicator), findsOneWidget);

    // Finish
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();
  });

  testWidgets('SearchResultsList calls onListingTap when item tapped', (
    WidgetTester tester,
  ) async {
    final listing = Listing(
      id: '1',
      fundaId: '1',
      address: 'Tap Me',
      city: 'Test City',
      price: 500000,
    );

    when(mockApiService.getListings(any)).thenAnswer((_) async => ListingResponse(
      items: [listing],
      pageIndex: 1,
      totalPages: 1,
      totalCount: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    ));

    Listing? tappedListing;
    await tester.pumpWidget(createWidget(onListingTap: (l) => tappedListing = l));

    searchProvider.setQuery('test');
    await searchProvider.refresh();
    await tester.pumpAndSettle();

    await tester.tap(find.text('Tap Me'));

    expect(tappedListing, isNotNull);
    expect(tappedListing!.id, '1');

    await tester.pumpAndSettle();
  });
}
