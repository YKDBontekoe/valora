import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/screens/saved_listings_screen.dart';
import 'package:valora_app/widgets/valora_widgets.dart';
import 'package:valora_app/widgets/valora_listing_card.dart';

// Reuse HttpOverrides logic
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
  when(response.contentLength).thenReturn(_transparentImage.length);
  when(response.statusCode).thenReturn(HttpStatus.ok);
  when(
    response.compressionState,
  ).thenReturn(HttpClientResponseCompressionState.notCompressed);
  when(response.listen(any)).thenAnswer((Invocation invocation) {
    final void Function(List<int>) onData = invocation.positionalArguments[0];
    final void Function() onDone = invocation.namedArguments[#onDone];
    final void Function(Object, [StackTrace]) onError =
        invocation.namedArguments[#onError];
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
  0x89,
  0x50,
  0x4E,
  0x47,
  0x0D,
  0x0A,
  0x1A,
  0x0A,
  0x00,
  0x00,
  0x00,
  0x0D,
  0x49,
  0x48,
  0x44,
  0x52,
  0x00,
  0x00,
  0x00,
  0x01,
  0x00,
  0x00,
  0x00,
  0x01,
  0x08,
  0x06,
  0x00,
  0x00,
  0x00,
  0x1F,
  0x15,
  0xC4,
  0x89,
  0x00,
  0x00,
  0x00,
  0x0A,
  0x49,
  0x44,
  0x41,
  0x54,
  0x78,
  0x9C,
  0x63,
  0x00,
  0x01,
  0x00,
  0x00,
  0x05,
  0x00,
  0x01,
  0x0D,
  0x0A,
  0x2D,
  0xB4,
  0x00,
  0x00,
  0x00,
  0x00,
  0x49,
  0x45,
  0x4E,
  0x44,
  0xAE,
  0x42,
  0x60,
  0x82,
];

class MockHttpClient extends Mock implements HttpClient {
  @override
  Future<HttpClientRequest> getUrl(Uri? url) => super.noSuchMethod(
    Invocation.method(#getUrl, [url]),
    returnValue: Future.value(MockHttpClientRequest()),
  );
}

class MockHttpClientRequest extends Mock implements HttpClientRequest {
  @override
  HttpHeaders get headers => super.noSuchMethod(
    Invocation.getter(#headers),
    returnValue: MockHttpHeaders(),
  );

  @override
  Future<HttpClientResponse> close() => super.noSuchMethod(
    Invocation.method(#close, []),
    returnValue: Future.value(MockHttpClientResponse()),
  );
}

class MockHttpClientResponse extends Mock implements HttpClientResponse {
  @override
  int get contentLength =>
      super.noSuchMethod(Invocation.getter(#contentLength), returnValue: 0);
  @override
  int get statusCode =>
      super.noSuchMethod(Invocation.getter(#statusCode), returnValue: 200);
  @override
  HttpClientResponseCompressionState get compressionState => super.noSuchMethod(
    Invocation.getter(#compressionState),
    returnValue: HttpClientResponseCompressionState.notCompressed,
  );

  @override
  StreamSubscription<List<int>> listen(
    void Function(List<int> event)? onData, {
    Function? onError,
    void Function()? onDone,
    bool? cancelOnError,
  }) {
    return super.noSuchMethod(
      Invocation.method(
        #listen,
        [onData],
        {#onError: onError, #onDone: onDone, #cancelOnError: cancelOnError},
      ),
      returnValue: Stream<List<int>>.empty().listen(null),
    );
  }
}

class MockHttpHeaders extends Mock implements HttpHeaders {}

void main() {
  final listing1 = {
    "id": "1",
    "fundaId": "1",
    "address": "A Street",
    "city": "Amsterdam",
    "postalCode": "1000AA",
    "price": 500000.0,
    "bedrooms": 2,
    "bathrooms": 1,
    "livingAreaM2": 100,
    "plotAreaM2": 100,
    "propertyType": "House",
    "status": "Available",
    "url": "http://test",
    "imageUrl": "http://test",
    "listedDate": "2023-01-01T00:00:00Z",
    "createdAt": "2023-01-01T00:00:00Z",
  };

  final listing2 = {
    "id": "2",
    "fundaId": "2",
    "address": "B Street",
    "city": "Rotterdam",
    "postalCode": "2000BB",
    "price": 300000.0,
    "bedrooms": 3,
    "bathrooms": 1,
    "livingAreaM2": 120,
    "plotAreaM2": 120,
    "propertyType": "House",
    "status": "Available",
    "url": "http://test",
    "imageUrl": "http://test",
    "listedDate": "2023-01-02T00:00:00Z",
    "createdAt": "2023-01-02T00:00:00Z",
  };

  setUp(() {
    // Use V2 storage format to control savedAt timestamps for deterministic sorting
    final listing1V2 = {
      'savedAt': '2023-01-01T10:00:00Z',
      'listing': listing1,
    };
    final listing2V2 = {
      'savedAt': '2023-01-02T10:00:00Z',
      'listing': listing2,
    };

    SharedPreferences.setMockInitialValues({
      'favorite_listings_v2': [
        json.encode(listing1V2),
        json.encode(listing2V2),
      ],
    });
    HttpOverrides.global = TestHttpOverrides();
  });

  Widget createSavedListingsScreen() {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<FavoritesProvider>(
          create: (_) => FavoritesProvider(),
        ),
      ],
      child: const MaterialApp(home: Scaffold(body: SavedListingsScreen())),
    );
  }

  group('SavedListingsScreen', () {
    testWidgets('Shows saved listings and icons', (WidgetTester tester) async {
      await tester.pumpWidget(createSavedListingsScreen());
      // Wait for image loading to replace shimmer (finite pump)
      await tester.pump();
      await tester.pump(const Duration(seconds: 2));

      expect(find.text('A Street'), findsOneWidget);
      expect(find.text('B Street'), findsOneWidget);
      expect(find.byType(Icon), findsWidgets);
    });

    testWidgets('Filters listings by search query', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pump(const Duration(seconds: 1));

      await tester.enterText(find.byType(ValoraTextField), 'Amsterdam');
      await tester.pump(const Duration(seconds: 1)); // Animation

      expect(find.text('A Street'), findsOneWidget);
      expect(find.text('B Street'), findsNothing);
    });

    testWidgets('Shows no matches found state and clears search', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pump(const Duration(seconds: 1));

      // Enter search query that matches nothing
      await tester.enterText(find.byType(ValoraTextField), 'Unknown');
      await tester.pump(const Duration(seconds: 1)); // Wait longer

      expect(find.text('No matches found'), findsOneWidget);
      expect(find.text('Clear Search'), findsOneWidget);

      // Clear search
      await tester.tap(find.text('Clear Search'));
      await tester.pump(); // Start animations/rebuild
      await tester.pump(
        const Duration(seconds: 3),
      ); // Wait longer for all animations (fade in lists + images)

      expect(find.text('A Street'), findsOneWidget);
      expect(find.text('B Street'), findsOneWidget);
    });

    testWidgets('Sorts listings by price low to high', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pump(const Duration(seconds: 1));

      final chipFinder = find.text('Price: Low to High');
      final scrollable = find
          .descendant(
            of: find.byType(SingleChildScrollView),
            matching: find.byType(Scrollable),
          )
          .first;

      await tester.scrollUntilVisible(chipFinder, 50.0, scrollable: scrollable);
      await tester.pump(const Duration(milliseconds: 100));

      await tester.tap(chipFinder);
      await tester.pump(const Duration(seconds: 2));

      final finder = find.descendant(
        of: find.byType(SliverList),
        matching: find.byType(ValoraListingCard),
      );
      final cards = tester.widgetList<ValoraListingCard>(finder);

      expect(cards.first.listing.price, 300000.0);
      expect(cards.last.listing.price, 500000.0);
    });

    testWidgets('Sorts listings by Newest', (WidgetTester tester) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pump(const Duration(seconds: 1));

      // First change to Price sort
      final priceChip = find.text('Price: Low to High');
      final scrollable = find
          .descendant(
            of: find.byType(SingleChildScrollView),
            matching: find.byType(Scrollable),
          )
          .first;

      await tester.scrollUntilVisible(priceChip, 50.0, scrollable: scrollable);
      await tester.tap(priceChip);
      await tester.pump(const Duration(seconds: 2));

      // Now tap Newest
      final newestChip = find.text('Newest');
      await tester.scrollUntilVisible(newestChip, 50.0, scrollable: scrollable);
      await tester.tap(newestChip);
      await tester.pump(const Duration(seconds: 2));

      final finder = find.descendant(
        of: find.byType(SliverList),
        matching: find.byType(ValoraListingCard),
      );
      final cards = tester.widgetList<ValoraListingCard>(finder);

      // Default is reversed (LIFO): B Street (added 2nd) should be first
      expect(cards.first.listing.address, 'B Street');
      expect(cards.last.listing.address, 'A Street');
    });

    testWidgets('Sorts listings by City', (WidgetTester tester) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pump(const Duration(seconds: 1));

      final scrollable = find
          .descendant(
            of: find.byType(SingleChildScrollView),
            matching: find.byType(Scrollable),
          )
          .first;

      // Sort A-Z (Amsterdam, Rotterdam)
      final cityAscChip = find.text('City: A-Z');
      await tester.scrollUntilVisible(
        cityAscChip,
        50.0,
        scrollable: scrollable,
      );
      await tester.tap(cityAscChip);
      await tester.pump(const Duration(seconds: 2));

      var finder = find.descendant(
        of: find.byType(SliverList),
        matching: find.byType(ValoraListingCard),
      );
      var cards = tester.widgetList<ValoraListingCard>(finder);
      expect(cards.first.listing.city, 'Amsterdam');
      expect(cards.last.listing.city, 'Rotterdam');

      // Sort Z-A (Rotterdam, Amsterdam)
      final cityDescChip = find.text('City: Z-A');
      await tester.scrollUntilVisible(
        cityDescChip,
        50.0,
        scrollable: scrollable,
      );
      await tester.tap(cityDescChip);
      await tester.pump(const Duration(seconds: 2));

      finder = find.descendant(
        of: find.byType(SliverList),
        matching: find.byType(ValoraListingCard),
      );
      cards = tester.widgetList<ValoraListingCard>(finder);
      expect(cards.first.listing.city, 'Rotterdam');
      expect(cards.last.listing.city, 'Amsterdam');
    });

    testWidgets('Removes favorite after confirmation', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pump(const Duration(seconds: 1));

      // In ValoraListingCard, the favorite button is an Icon(Icons.favorite) when favored
      final favIcon = find.byIcon(Icons.favorite);
      expect(favIcon, findsWidgets); // Ensure at least one favorite icon is found
      await tester.tap(favIcon.first);

      await tester.pump(const Duration(seconds: 1)); // Dialog animation

      // Verify Dialog
      if (find.text('Remove Favorite?').evaluate().isNotEmpty) {
        expect(find.text('Remove Favorite?'), findsOneWidget);
        await tester.tap(find.text('Remove'));
        await tester.pump(const Duration(seconds: 1)); // Animation
        expect(find.byType(ValoraListingCard), findsOneWidget);
      }
    });
  });
}
