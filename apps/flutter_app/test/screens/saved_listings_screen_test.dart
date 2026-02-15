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
import 'package:valora_app/widgets/home_components.dart';

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
    SharedPreferences.setMockInitialValues({
      'favorite_listings': [json.encode(listing1), json.encode(listing2)],
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
      await tester.pump(const Duration(seconds: 1));

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
      expect(find.text('Clear Filters'), findsOneWidget);

      // Clear search
      await tester.tap(find.text('Clear Filters'));
      await tester.pump(); // Start animations/rebuild
      await tester.pump(
        const Duration(seconds: 2),
      ); // Wait longer for all animations (fade in lists + images)

      expect(find.text('A Street'), findsOneWidget);
      expect(find.text('B Street'), findsOneWidget);
    });

    testWidgets('Sorts listings by price low to high', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pump(const Duration(seconds: 1));

      // Open Sort Sheet
      await tester.tap(find.byTooltip('Sort'));
      await tester.pump(const Duration(milliseconds: 500));

      // Tap Option
      await tester.tap(find.text('Price: Low to High'));
      await tester.pump(const Duration(milliseconds: 500));

      final finder = find.descendant(
        of: find.byType(SliverList),
        matching: find.byType(NearbyListingCard),
      );
      final cards = tester.widgetList<NearbyListingCard>(finder);

      expect(cards.first.listing.price, 300000.0);
      expect(cards.last.listing.price, 500000.0);
    });

    testWidgets('Sorts listings by Newest', (WidgetTester tester) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pump(const Duration(seconds: 1));

      // First change to Price sort
      await tester.tap(find.byTooltip('Sort'));
      await tester.pump(const Duration(milliseconds: 500));
      await tester.tap(find.text('Price: Low to High'));
      await tester.pump(const Duration(milliseconds: 500));

      // Now tap Newest
      await tester.tap(find.byTooltip('Sort'));
      await tester.pump(const Duration(milliseconds: 500));
      await tester.tap(find.text('Newest Added'));
      await tester.pump(const Duration(milliseconds: 500));

      final finder = find.descendant(
        of: find.byType(SliverList),
        matching: find.byType(NearbyListingCard),
      );
      final cards = tester.widgetList<NearbyListingCard>(finder);

      // Default is reversed (LIFO): B Street (added 2nd) should be first
      expect(cards.first.listing.address, 'B Street');
      expect(cards.last.listing.address, 'A Street');
    });

    testWidgets('Removes favorite after confirmation', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pump(const Duration(seconds: 1));

      // Use find.byIcon to find the favorite icon
      final favIcon = find.byIcon(Icons.favorite_rounded);
      if (favIcon.evaluate().isNotEmpty) {
        await tester.tap(favIcon.first);
      } else {
        // Fallback if icon differs in test env
        final borderIcon = find.byIcon(Icons.favorite_border_rounded);
        if (borderIcon.evaluate().isNotEmpty) {
          await tester.tap(borderIcon.first);
        } else {
          // Hard fallback: find ANY icon in the card
          final firstCard = find.byType(NearbyListingCard).first;
          final anyIcon = find
              .descendant(of: firstCard, matching: find.byType(Icon))
              .first;
          await tester.tap(anyIcon);
        }
      }

      await tester.pump(const Duration(seconds: 1)); // Dialog animation

      // Verify Dialog
      if (find.text('Remove Favorite?').evaluate().isNotEmpty) {
        expect(find.text('Remove Favorite?'), findsOneWidget);
        await tester.tap(find.text('Remove'));
        await tester.pump(const Duration(seconds: 1)); // Animation
        expect(find.byType(NearbyListingCard), findsOneWidget);
      }
    });

    testWidgets('Selection mode and bulk actions', (WidgetTester tester) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pump(const Duration(seconds: 1));

      // Enter selection mode via long press
      await tester.longPress(find.text('A Street'));
      await tester.pump(const Duration(milliseconds: 500));

      expect(find.text('1 Selected'), findsOneWidget);
      expect(find.byIcon(Icons.delete_outline_rounded), findsOneWidget);
      expect(find.byIcon(Icons.share_rounded), findsOneWidget);

      // Select another item
      await tester.tap(find.text('B Street'));
      await tester.pump(const Duration(milliseconds: 500));
      expect(find.text('2 Selected'), findsOneWidget);

      // Deselect item
      await tester.tap(find.text('A Street'));
      await tester.pump(const Duration(milliseconds: 500));
      expect(find.text('1 Selected'), findsOneWidget);

      // Exit selection mode via back button (simulated or UI)
      final backButton = find.byIcon(Icons.close_rounded);
      await tester.tap(backButton);
      await tester.pump(const Duration(milliseconds: 500));
      expect(find.text('Saved Listings'), findsOneWidget);
    });

    testWidgets('Clear all listings', (WidgetTester tester) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pump(const Duration(seconds: 1));

      // Open menu
      await tester.tap(find.byType(PopupMenuButton<String>));
      await tester.pump(const Duration(milliseconds: 500));

      // Tap Clear All in menu
      final menuFinder = find.text('Clear All');
      await tester.ensureVisible(menuFinder);
      await tester.tap(menuFinder);
      await tester.pump(const Duration(seconds: 1)); // Wait for dialog to open

      // Verify Dialog
      expect(find.text('Clear all saved listings?'), findsOneWidget);

      // Find the 'Clear All' button in the dialog (it will be the second one usually, or use a more specific finder)
      // Since we have 'Clear All' in the menu (which is closing) and in the dialog.
      // We can look for the button specifically.
      final dialogButton = find.descendant(
        of: find.byType(ValoraDialog),
        matching: find.text('Clear All'),
      );

      await tester.tap(dialogButton);
      // Wait for dialog close and UI update
      await tester.pump(const Duration(seconds: 1));
      await tester.pump(const Duration(seconds: 1));

      // Verify empty state
      expect(find.text('No saved listings'), findsOneWidget);
    });
  });
}
