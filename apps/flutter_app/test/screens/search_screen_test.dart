import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/providers/search_provider.dart';
import 'package:valora_app/screens/search_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

@GenerateMocks([ApiService, FavoritesProvider, SearchProvider])
@GenerateNiceMocks([
  MockSpec<HttpClient>(),
  MockSpec<HttpClientRequest>(),
  MockSpec<HttpClientResponse>(),
  MockSpec<HttpHeaders>(),
])
import 'search_screen_test.mocks.dart';

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

  when(client.getUrl(any)).thenAnswer((_) async => request);
  when(request.headers).thenReturn(headers);
  when(request.close()).thenAnswer((_) async => response);
  when(response.contentLength).thenReturn(0);
  when(response.statusCode).thenReturn(HttpStatus.ok);
  when(response.compressionState).thenReturn(HttpClientResponseCompressionState.notCompressed);
  when(response.listen(any)).thenAnswer((Invocation invocation) {
    return const Stream<List<int>>.empty().listen(invocation.positionalArguments[0]);
  });

  return client;
}

void main() {
  late MockApiService mockApiService;
  late MockFavoritesProvider mockFavoritesProvider;
  late MockSearchProvider mockSearchProvider;

  setUp(() {
    mockApiService = MockApiService();
    mockFavoritesProvider = MockFavoritesProvider();
    mockSearchProvider = MockSearchProvider();

    when(mockFavoritesProvider.favorites).thenReturn([]);
    when(mockFavoritesProvider.isFavorite(any)).thenReturn(false);

    when(mockSearchProvider.listings).thenReturn([]);
    when(mockSearchProvider.isLoading).thenReturn(false);
    when(mockSearchProvider.isLoadingMore).thenReturn(false);
    when(mockSearchProvider.error).thenReturn(null);
    when(mockSearchProvider.currentQuery).thenReturn('');
    when(mockSearchProvider.hasNextPage).thenReturn(false);
    when(mockSearchProvider.hasActiveFilters).thenReturn(false);

    when(mockSearchProvider.minPrice).thenReturn(null);
    when(mockSearchProvider.maxPrice).thenReturn(null);
    when(mockSearchProvider.city).thenReturn(null);
    when(mockSearchProvider.minBedrooms).thenReturn(null);
    when(mockSearchProvider.minLivingArea).thenReturn(null);
    when(mockSearchProvider.maxLivingArea).thenReturn(null);
    when(mockSearchProvider.sortBy).thenReturn(null);
    when(mockSearchProvider.sortOrder).thenReturn(null);

    HttpOverrides.global = TestHttpOverrides();
  });

  Widget createWidgetUnderTest() {
    return MultiProvider(
      providers: [
        Provider<ApiService>.value(value: mockApiService),
        ChangeNotifierProvider<FavoritesProvider>.value(value: mockFavoritesProvider),
        ChangeNotifierProvider<SearchProvider>.value(value: mockSearchProvider),
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

  testWidgets('SearchScreen triggers search on text change', (WidgetTester tester) async {
    await tester.pumpWidget(createWidgetUnderTest());

    await tester.enterText(find.byType(TextField), 'Test');
    await tester.pump();

    verify(mockSearchProvider.setQuery('Test')).called(1);
  });

  testWidgets('SearchScreen displays listings from provider', (WidgetTester tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      city: 'Test City',
      price: 500000,
      imageUrl: 'http://example.com/image.jpg',
    );

    when(mockSearchProvider.listings).thenReturn([listing]);
    when(mockSearchProvider.currentQuery).thenReturn('Test');

    await tester.pumpWidget(createWidgetUnderTest());

    expect(find.text('Test Address'), findsOneWidget);
  });

  testWidgets('SearchScreen opens filter dialog and updates filters', (WidgetTester tester) async {
    await tester.pumpWidget(createWidgetUnderTest());

    await tester.tap(find.byIcon(Icons.tune_rounded));
    await tester.pumpAndSettle();

    expect(find.text('Filter & Sort'), findsOneWidget);

    await tester.enterText(
        find.descendant(
            of: find.widgetWithText(ValoraTextField, 'Min Price'),
            matching: find.byType(TextField)),
        '100000');

    await tester.tap(find.text('Apply'));
    await tester.pumpAndSettle();

    verify(mockSearchProvider.updateFilters(
      minPrice: 100000.0,
      maxPrice: null,
      city: null,
      minBedrooms: null,
      minLivingArea: null,
      maxLivingArea: null,
      sortBy: 'date',
      sortOrder: 'desc', // Filter dialog defaults
    )).called(1);
  });

  testWidgets('SearchScreen handles error state from provider', (WidgetTester tester) async {
    when(mockSearchProvider.error).thenReturn('Network error');

    await tester.pumpWidget(createWidgetUnderTest());

    expect(find.text('Search Failed'), findsOneWidget);
    expect(find.text('Network error'), findsOneWidget);
    expect(find.text('Retry'), findsOneWidget);
  });

  testWidgets('SearchScreen calls loadMoreListings on scroll', (WidgetTester tester) async {
     final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      city: 'Test City',
      price: 500000,
      imageUrl: 'http://example.com/image.jpg',
    );

    when(mockSearchProvider.listings).thenReturn(List.generate(10, (i) => listing));
    when(mockSearchProvider.hasNextPage).thenReturn(true);
    when(mockSearchProvider.currentQuery).thenReturn('Test');

    await tester.pumpWidget(createWidgetUnderTest());

    await tester.drag(find.byType(CustomScrollView), const Offset(0, -2000));
    await tester.pump();

    verify(mockSearchProvider.loadMoreListings()).called(1);
  });
}
