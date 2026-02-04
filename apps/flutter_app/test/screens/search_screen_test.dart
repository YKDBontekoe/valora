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

    // Enter text to load listings
    await tester.enterText(find.byType(TextField), 'Test');
    await tester.pump(const Duration(milliseconds: 600));
    await tester.pumpAndSettle();

    expect(find.text('Test Address'), findsOneWidget);

    // Clear text
    await tester.enterText(find.byType(TextField), '');
    await tester.pump(const Duration(milliseconds: 600));
    await tester.pumpAndSettle();

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

    await tester.enterText(find.byType(TextField), 'Test');
    await tester.pump(const Duration(milliseconds: 600)); // Debounce
    await tester.pumpAndSettle();

    expect(find.text('Test Address'), findsOneWidget);
  });

  testWidgets('SearchScreen opens filter dialog and updates filters', (WidgetTester tester) async {
     when(mockApiService.getListings(any)).thenAnswer((_) async {
      return ListingResponse(items: [], pageIndex: 1, totalPages: 1, totalCount: 0, hasNextPage: false, hasPreviousPage: false);
    });

    await tester.pumpWidget(createWidgetUnderTest());

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

    await tester.enterText(find.byType(TextField), 'Error');
    await tester.pump(const Duration(milliseconds: 600));
    await tester.pumpAndSettle();

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

    // Load first page
    await tester.enterText(find.byType(TextField), 'Test');
    await tester.pump(const Duration(milliseconds: 600));
    await tester.pumpAndSettle();

    // Verify first page loaded
    expect(find.byType(NearbyListingCard), findsWidgets);

    // Scroll to bottom
    await tester.drag(find.byType(CustomScrollView), const Offset(0, -2000));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    await tester.pumpAndSettle();

    // Verify error snackbar
    expect(find.text('Failed to load more items'), findsOneWidget);
  });
}
