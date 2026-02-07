import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/models/listing_filter.dart';
import 'package:valora_app/models/listing_response.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/screens/search_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/widgets/home_components.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

@GenerateMocks([ApiService, FavoritesProvider])
@GenerateNiceMocks([
  MockSpec<HttpClient>(),
  MockSpec<HttpClientRequest>(),
  MockSpec<HttpClientResponse>(),
  MockSpec<HttpHeaders>(),
])
import 'search_screen_test.mocks.dart';

// Mock HTTP overrides to return a valid 1x1 transparent PNG
class TestHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return _createMockImageHttpClient(context);
  }
}

HttpClient _createMockImageHttpClient(SecurityContext? context) {
  final client = MockHttpClient();
  final request = MockHttpClientRequest();
  final response = MockHttpClientResponse();
  final headers = MockHttpHeaders();

  // Use a catch-all matcher for the URL
  when(client.getUrl(any)).thenAnswer((_) async => request);
  when(request.headers).thenReturn(headers);
  when(request.close()).thenAnswer((_) async => response);
  when(response.contentLength).thenReturn(_transparentImage.length);
  when(response.statusCode).thenReturn(HttpStatus.ok);
  when(response.compressionState).thenReturn(HttpClientResponseCompressionState.notCompressed);
  when(response.listen(any)).thenAnswer((Invocation invocation) {
    final void Function(List<int>) onData = invocation.positionalArguments[0];
    final void Function() onDone = invocation.namedArguments[#onDone];
    final void Function(Object, [StackTrace]) onError = invocation.namedArguments[#onError];
    final bool cancelOnError = invocation.namedArguments[#cancelOnError];

    return Stream<List<int>>.fromIterable([_transparentImage]).listen(
      onData,
      onDone: onDone,
      onError: onError,
      cancelOnError: cancelOnError,
    );
  });

  return client;
}

const List<int> _transparentImage = <int>[
  0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49,
  0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06,
  0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44,
  0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01, 0x0D,
  0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42,
  0x60, 0x82,
];

void main() {
  late MockApiService mockApiService;
  late MockFavoritesProvider mockFavoritesProvider;

  setUp(() {
    mockApiService = MockApiService();
    mockFavoritesProvider = MockFavoritesProvider();

    // Default favorites provider behavior
    when(mockFavoritesProvider.favorites).thenReturn([]);
    when(mockFavoritesProvider.isFavorite(any)).thenReturn(false);

    // Install HTTP overrides
    HttpOverrides.global = TestHttpOverrides();
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
    await tester.pumpAndSettle(); // Wait for entry animations

    expect(find.text('Search'), findsOneWidget);
    expect(find.text('Find your home'), findsOneWidget);
    expect(find.byType(ValoraTextField), findsOneWidget);
  });

  testWidgets('SearchScreen clears listings when query is cleared', (WidgetTester tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      city: 'Test City',
      price: 500000,
      imageUrl: 'http://example.com/image.jpg',
    );

    when(mockApiService.getListings(any)).thenAnswer((_) async {
      return ListingResponse(
        items: [listing],
        pageIndex: 1,
        totalPages: 1,
        totalCount: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      );
    });

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle(); // Initial animations

    // Enter text to load listings
    await tester.enterText(find.byType(TextField), 'Test');
    await tester.pump(const Duration(milliseconds: 600));
    await tester.pump(); // Start fetching
    await tester.pump(const Duration(milliseconds: 500)); // Animations

    expect(find.text('Test Address'), findsOneWidget);

    // Clear text
    await tester.enterText(find.byType(TextField), '');
    await tester.pump(const Duration(milliseconds: 600));
    await tester.pump(); // Clear state
    await tester.pump(const Duration(milliseconds: 500)); // Animations

    // Verify listings are cleared (should show "Find your home" empty state)
    expect(find.text('Find your home'), findsOneWidget);
    expect(find.text('Test Address'), findsNothing);
  });

  testWidgets('SearchScreen displays listings after successful search', (WidgetTester tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      city: 'Test City',
      price: 500000,
      imageUrl: 'http://example.com/image.jpg',
    );

    when(mockApiService.getListings(any)).thenAnswer((_) async {
      return ListingResponse(
        items: [listing],
        pageIndex: 1,
        totalPages: 1,
        totalCount: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      );
    });

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextField), 'Test');
    await tester.pump(const Duration(milliseconds: 600)); // Debounce
    await tester.pump(); // Start fetch
    await tester.pump(const Duration(milliseconds: 500)); // Wait for animations

    expect(find.text('Test Address'), findsOneWidget);
  });

  testWidgets('SearchScreen opens filter dialog and updates filters', (WidgetTester tester) async {
     when(mockApiService.getListings(any)).thenAnswer((_) async {
      return ListingResponse(items: [], pageIndex: 1, totalPages: 1, totalCount: 0, hasNextPage: false, hasPreviousPage: false);
    });

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    // Open filter dialog
    await tester.tap(find.byIcon(Icons.tune_rounded));
    await tester.pumpAndSettle();

    expect(find.text('Filter & Sort'), findsOneWidget);

    // Enter Min Price
    await tester.enterText(
        find.descendant(
            of: find.widgetWithText(ValoraTextField, 'Min Price'),
            matching: find.byType(TextField)),
        '100000');

    // Tap Apply
    await tester.tap(find.text('Apply'));
    await tester.pumpAndSettle();

    // Verify filters applied (should trigger search with new filter)
    verify(mockApiService.getListings(argThat(predicate((filter) {
        if (filter is! ListingFilter) return false;
        return filter.minPrice == 100000.0;
    })))).called(1);
  });

  testWidgets('SearchScreen handles error state with generic exception', (WidgetTester tester) async {
    when(mockApiService.getListings(any)).thenThrow(Exception('Network error'));

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextField), 'Error');
    await tester.pump(const Duration(milliseconds: 600));
    await tester.pump(); // Start fetch
    await tester.pump(const Duration(milliseconds: 500)); // Animations

    // Verify error UI
    expect(find.text('Failed to search listings'), findsOneWidget);
    expect(find.text('Retry'), findsOneWidget);
  });

  testWidgets('SearchScreen handles pagination error', (WidgetTester tester) async {
     final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      city: 'Test City',
      price: 500000,
      imageUrl: 'http://example.com/image.jpg',
    );

    // First page success
    when(mockApiService.getListings(argThat(predicate((f) => (f as ListingFilter).page == 1))))
        .thenAnswer((_) async => ListingResponse(
          items: List.generate(10, (i) => listing),
          pageIndex: 1,
          totalPages: 2,
          totalCount: 20,
          hasNextPage: true,
          hasPreviousPage: false,
        ));

    // Second page fails
    when(mockApiService.getListings(argThat(predicate((f) => (f as ListingFilter).page == 2))))
        .thenThrow(Exception('Pagination Error'));

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    // Load first page
    await tester.enterText(find.byType(TextField), 'Test');
    await tester.pump(const Duration(milliseconds: 600));
    await tester.pump(); // Fetch
    await tester.pump(const Duration(milliseconds: 500)); // Animations

    // Verify first page loaded
    expect(find.byType(NearbyListingCard), findsWidgets);

    // Scroll to bottom
    await tester.drag(find.byType(CustomScrollView), const Offset(0, -2000));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100)); // Debounce scroll? Or just wait for listener

    // Wait for error handling. Using pump instead of pumpAndSettle to avoid timeout if spinner persists.
    await tester.pump(const Duration(seconds: 1));
    await tester.pump();

    // Verify error snackbar
    expect(find.text('Failed to load more items'), findsOneWidget);
  });

  testWidgets('SearchScreen clears filters via Clear button', (WidgetTester tester) async {
    when(mockApiService.getListings(any)).thenAnswer((_) async {
      return ListingResponse(
        items: [],
        pageIndex: 1,
        totalPages: 1,
        totalCount: 0,
        hasNextPage: false,
        hasPreviousPage: false,
      );
    });

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    // Enter query to ensure API is called
    await tester.enterText(find.byType(TextField), 'test');
    await tester.pump(const Duration(milliseconds: 600));
    await tester.pump();

    // Set a filter (e.g. city)
    await tester.tap(find.byIcon(Icons.tune_rounded));
    await tester.pumpAndSettle();

    await tester.enterText(
        find.descendant(
            of: find.widgetWithText(ValoraTextField, 'City'),
            matching: find.byType(TextField)),
        'FilterCity');
    await tester.tap(find.text('Apply'));
    await tester.pumpAndSettle();

    // Verify filter chip appears
    expect(find.text('City: FilterCity'), findsOneWidget);

    // Verify Clear button appears
    final clearButton = find.byIcon(Icons.clear_all_rounded);
    expect(clearButton, findsOneWidget);

    // Tap Clear
    await tester.tap(clearButton);
    await tester.pumpAndSettle();

    // Verify filter chip gone
    expect(find.text('City: FilterCity'), findsNothing);

    // Verify reload triggered with empty filters
    verify(mockApiService.getListings(argThat(predicate((filter) {
        if (filter is! ListingFilter) return false;
        return filter.city == null;
    })))).called(greaterThan(1)); // Initial load + apply filter + clear filter
  });

  testWidgets('SearchScreen refreshes on pull down', (WidgetTester tester) async {
    when(mockApiService.getListings(any)).thenAnswer((_) async {
      return ListingResponse(
        items: [],
        pageIndex: 1,
        totalPages: 1,
        totalCount: 0,
        hasNextPage: false,
        hasPreviousPage: false,
      );
    });

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    // Enter query
    await tester.enterText(find.byType(TextField), 'test');
    await tester.pump(const Duration(milliseconds: 600));
    await tester.pump();

    // Pull down
    await tester.drag(find.byType(CustomScrollView), const Offset(0, 300));
    await tester.pump(const Duration(seconds: 1)); // Trigger refresh
    await tester.pump(const Duration(seconds: 1)); // Finish refresh

    // Verify reload triggered
    verify(mockApiService.getListings(any)).called(greaterThan(1));
  });

  testWidgets('SearchScreen shows sort options bottom sheet', (WidgetTester tester) async {
    // Increase surface size to avoid overflow in bottom sheet
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    when(mockApiService.getListings(any)).thenAnswer((_) async {
      return ListingResponse(
        items: [],
        pageIndex: 1,
        totalPages: 1,
        totalCount: 0,
        hasNextPage: false,
        hasPreviousPage: false,
      );
    });

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    // Tap Sort button
    await tester.tap(find.byTooltip('Sort'));
    await tester.pumpAndSettle();

    expect(find.text('Sort By'), findsOneWidget);
    expect(find.text('Newest'), findsOneWidget);
    expect(find.text('Price: Low to High'), findsOneWidget);
  });

  testWidgets('SearchScreen applies sort option', (WidgetTester tester) async {
    // Increase surface size to avoid overflow in bottom sheet
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    when(mockApiService.getListings(any)).thenAnswer((_) async {
      return ListingResponse(
        items: [],
        pageIndex: 1,
        totalPages: 1,
        totalCount: 0,
        hasNextPage: false,
        hasPreviousPage: false,
      );
    });

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    // Tap Sort button
    await tester.tap(find.byTooltip('Sort'));
    await tester.pumpAndSettle();

    // Tap a sort option
    await tester.tap(find.text('Price: Low to High'));
    await tester.pumpAndSettle();

    // Verify API called with sort params
    verify(mockApiService.getListings(argThat(predicate((filter) {
        if (filter is! ListingFilter) return false;
        return filter.sortBy == 'price' && filter.sortOrder == 'asc';
    })))).called(greaterThan(0));
  });

  testWidgets('SearchScreen removes filter via chip delete icon', (WidgetTester tester) async {
    when(mockApiService.getListings(any)).thenAnswer((_) async {
      return ListingResponse(
        items: [],
        pageIndex: 1,
        totalPages: 1,
        totalCount: 0,
        hasNextPage: false,
        hasPreviousPage: false,
      );
    });

    await tester.pumpWidget(createWidgetUnderTest());
    await tester.pumpAndSettle();

    // Enter a query so that removing filter still triggers search
    await tester.enterText(find.byType(TextField), 'test');
    await tester.pump(const Duration(milliseconds: 600));
    await tester.pump();

    // Set a filter
    await tester.tap(find.byTooltip('Filters'));
    await tester.pumpAndSettle();

    await tester.enterText(
        find.descendant(
            of: find.widgetWithText(ValoraTextField, 'City'),
            matching: find.byType(TextField)),
        'FilterCity');
    await tester.tap(find.text('Apply'));
    await tester.pumpAndSettle();

    // Verify chip exists with delete icon
    expect(find.text('City: FilterCity'), findsOneWidget);
    expect(find.byIcon(Icons.close_rounded), findsOneWidget);

    // Tap delete icon
    await tester.tap(find.byIcon(Icons.close_rounded));
    await tester.pumpAndSettle();

    // Verify chip removed
    expect(find.text('City: FilterCity'), findsNothing);

    // Verify API called with null city AND search term
    verify(mockApiService.getListings(argThat(predicate((filter) {
        if (filter is! ListingFilter) return false;
        // The last call should have city null and searchTerm 'test'
        return filter.city == null && filter.searchTerm == 'test';
    })))).called(greaterThan(0));
  });
}
