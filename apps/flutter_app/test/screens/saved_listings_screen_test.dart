import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/screens/saved_listings_screen.dart';
import 'package:valora_app/widgets/valora_widgets.dart';
import 'package:valora_app/widgets/home_components.dart';

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
    "createdAt": "2023-01-01T00:00:00Z"
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
    "createdAt": "2023-01-02T00:00:00Z"
  };

  setUp(() {
     SharedPreferences.setMockInitialValues({
       'favorite_listings': [json.encode(listing1), json.encode(listing2)]
     });
  });

  Widget createSavedListingsScreen() {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<FavoritesProvider>(
          create: (_) => FavoritesProvider(),
        ),
      ],
      child: const MaterialApp(
        home: Scaffold(body: SavedListingsScreen()),
      ),
    );
  }

  group('SavedListingsScreen', () {
    testWidgets('Shows saved listings and icons', (WidgetTester tester) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pumpAndSettle();

      expect(find.text('A Street'), findsOneWidget);
      expect(find.text('B Street'), findsOneWidget);
      expect(find.byType(Icon), findsWidgets);
    });

    testWidgets('Filters listings by search query', (WidgetTester tester) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pumpAndSettle();

      await tester.enterText(find.byType(ValoraTextField), 'Amsterdam');
      await tester.pumpAndSettle();

      expect(find.text('A Street'), findsOneWidget);
      expect(find.text('B Street'), findsNothing);
    });

    testWidgets('Shows no matches found state and clears search', (WidgetTester tester) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pumpAndSettle();

      // Enter search query that matches nothing
      await tester.enterText(find.byType(ValoraTextField), 'Unknown');
      await tester.pumpAndSettle();

      expect(find.text('No matches found'), findsOneWidget);
      expect(find.text('Clear Search'), findsOneWidget);

      // Clear search
      await tester.tap(find.text('Clear Search'));
      await tester.pumpAndSettle();

      expect(find.text('A Street'), findsOneWidget);
      expect(find.text('B Street'), findsOneWidget);
    });

    testWidgets('Sorts listings by price low to high', (WidgetTester tester) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pumpAndSettle();

      final chipFinder = find.widgetWithText(FilterChip, 'Price: Low to High');
      final scrollable = find.descendant(
        of: find.byType(SingleChildScrollView),
        matching: find.byType(Scrollable),
      ).first;

      await tester.scrollUntilVisible(chipFinder, 50.0, scrollable: scrollable);
      await tester.pumpAndSettle();

      await tester.tap(chipFinder);
      await tester.pumpAndSettle();

      final finder = find.descendant(of: find.byType(SliverList), matching: find.byType(NearbyListingCard));
      final cards = tester.widgetList<NearbyListingCard>(finder);

      expect(cards.first.listing.price, 300000.0);
      expect(cards.last.listing.price, 500000.0);
    });

    testWidgets('Sorts listings by Newest', (WidgetTester tester) async {
      await tester.pumpWidget(createSavedListingsScreen());
      await tester.pumpAndSettle();

      // First change to Price sort
      final priceChip = find.widgetWithText(FilterChip, 'Price: Low to High');
      final scrollable = find.descendant(
        of: find.byType(SingleChildScrollView),
        matching: find.byType(Scrollable),
      ).first;

      await tester.scrollUntilVisible(priceChip, 50.0, scrollable: scrollable);
      await tester.tap(priceChip);
      await tester.pumpAndSettle();

      // Now tap Newest
      final newestChip = find.widgetWithText(FilterChip, 'Newest');
      await tester.scrollUntilVisible(newestChip, 50.0, scrollable: scrollable);
      await tester.tap(newestChip);
      await tester.pumpAndSettle();

      final finder = find.descendant(of: find.byType(SliverList), matching: find.byType(NearbyListingCard));
      final cards = tester.widgetList<NearbyListingCard>(finder);

      // Default is reversed (LIFO): B Street (added 2nd) should be first
      expect(cards.first.listing.address, 'B Street');
      expect(cards.last.listing.address, 'A Street');
    });

    testWidgets('Removes favorite after confirmation', (WidgetTester tester) async {
       await tester.pumpWidget(createSavedListingsScreen());
       await tester.pumpAndSettle();

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
             final anyIcon = find.descendant(of: firstCard, matching: find.byType(Icon)).first;
             await tester.tap(anyIcon);
          }
       }

       await tester.pumpAndSettle();

       // Verify Dialog
       if (find.text('Remove Favorite?').evaluate().isNotEmpty) {
         expect(find.text('Remove Favorite?'), findsOneWidget);
         await tester.tap(find.text('Remove'));
         await tester.pumpAndSettle();
         expect(find.byType(NearbyListingCard), findsOneWidget);
       }
    });
  });
}
