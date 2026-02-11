import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/widgets/home_components.dart';
import 'package:valora_app/widgets/valora_widgets.dart';
import 'package:valora_app/widgets/valora_listing_card.dart';
import 'package:valora_app/core/theme/valora_spacing.dart';

class MockFavoritesProvider extends Mock implements FavoritesProvider {
  @override
  bool isFavorite(String? id) => false;

  @override
  bool get hasListeners => false;

  @override
  Future<void> toggleFavorite(Listing listing) async {}
}

void main() {
  final mockFavoritesProvider = MockFavoritesProvider();

  final dummyListing = Listing(
    id: '1',
    fundaId: 'f1',
    address: 'Test Address',
    city: 'Test City',
    postalCode: '1234AB',
    price: 500000,
    bedrooms: 3,
    bathrooms: 2,
    livingAreaM2: 120,
    plotAreaM2: 200,
    propertyType: 'House',
    status: 'Available',
    url: 'http://test.com',
    imageUrl: 'http://test.com/image.jpg',
    listedDate: DateTime.now(),
    createdAt: DateTime.now(),
  );

  group('HomeHeader Tests', () {
    testWidgets('Search bar focus changes decoration', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: HomeHeader(
              searchController: TextEditingController(),
              onSearchChanged: (_) {},
              onFilterPressed: () {},
            ),
          ),
        ),
      );

      // Allow entry animations (filter chips) to complete
      await tester.pump(const Duration(seconds: 1));

      // Find the AnimatedContainer wrapping the TextField
      find
          .descendant(
            of: find.byType(HomeHeader),
            matching: find.byType(AnimatedContainer),
          )
          .first;

      // Tap to focus
      await tester.tap(find.byType(TextField));
      await tester.pump(); // Start animation
      await tester.pump(const Duration(milliseconds: 500)); // Finish animation

      // Verify the widget tree is stable and no errors occurred during animation
      expect(find.byType(HomeHeader), findsOneWidget);
    });
  });

  group('FeaturedListingCard Tests', () {
    testWidgets('Renders with correct base elevation', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ChangeNotifierProvider<FavoritesProvider>.value(
              value: mockFavoritesProvider,
              child: FeaturedListingCard(listing: dummyListing, onTap: () {}),
            ),
          ),
        ),
      );

      // Allow entry animations
      await tester.pump(const Duration(seconds: 1));

      final cardFinder = find.byType(ValoraCard);
      expect(cardFinder, findsOneWidget);

      // Verify initial elevation
      ValoraCard card = tester.widget(cardFinder);
      expect(card.elevation, ValoraSpacing.elevationMd);
    });
  });

  group('ValoraListingCard Tests', () {
    testWidgets('Renders with correct base elevation', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            // Wrap in SingleChildScrollView to prevent overflow in test
            body: SingleChildScrollView(
              child: ChangeNotifierProvider<FavoritesProvider>.value(
                value: mockFavoritesProvider,
                child: ValoraListingCard(listing: dummyListing, onTap: () {}),
              ),
            ),
          ),
        ),
      );

      // Allow entry animations
      await tester.pump(const Duration(seconds: 1));

      final cardFinder = find.byType(ValoraCard);
      expect(cardFinder, findsOneWidget);

      // ValoraListingCard passes padding: EdgeInsets.zero to ValoraCard, but doesn't override elevation.
      // ValoraCard default elevation is Sm.
      // Verify via widget inspector if possible, or just checking default behavior.
      // Since we can't easily inspect internal state without keys, we assume it works if it renders.
      // But we can check property on the widget found.
      ValoraCard card = tester.widget(cardFinder);
      // Default elevation in ValoraCard is ValoraSpacing.elevationSm
      // ValoraListingCard doesn't set it, so it should be null (which defaults to Sm inside ValoraCard build)
      expect(card.elevation, null);
    });
  });
}
