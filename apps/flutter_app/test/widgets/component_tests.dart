import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/widgets/common/valora_button.dart';
import 'package:valora_app/widgets/common/valora_chip.dart';
import 'package:valora_app/widgets/home/featured_listing_card.dart';
import 'package:valora_app/widgets/home/nearby_listing_card.dart';

void main() {
  setUpAll(() {
    SharedPreferences.setMockInitialValues({});
  });

  group('ValoraButton', () {
    testWidgets('renders correctly and handles taps', (WidgetTester tester) async {
      bool pressed = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraButton(
              label: 'Test Button',
              onPressed: () => pressed = true,
            ),
          ),
        ),
      );

      expect(find.text('Test Button'), findsOneWidget);
      await tester.tap(find.byType(ValoraButton));
      await tester.pumpAndSettle();
      expect(pressed, isTrue);
    });

    testWidgets('renders loading state', (WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: ValoraButton(
              label: 'Test Button',
              onPressed: () {},
              isLoading: true,
            ),
          ),
        ),
      );
      await tester.pump(); // Allow AnimatedSwitcher to start

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(find.text('Test Button'), findsNothing);

      // Dispose to stop infinite animation of CircularProgressIndicator
      await tester.pumpWidget(Container());
      await tester.pumpAndSettle();
    });
  });

  group('ValoraChip', () {
    testWidgets('renders correctly and handles selection', (WidgetTester tester) async {
      bool isSelected = false;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: StatefulBuilder(
              builder: (context, setState) {
                return ValoraChip(
                  label: 'Test Chip',
                  isSelected: isSelected,
                  onSelected: (val) {
                    setState(() {
                      isSelected = val;
                    });
                  },
                );
              },
            ),
          ),
        ),
      );

      expect(find.text('Test Chip'), findsOneWidget);

      // Tap to select
      await tester.tap(find.byType(ValoraChip), warnIfMissed: false);
      await tester.pumpAndSettle();
      expect(isSelected, isTrue);

      // Verify selected style (approximate via container decoration if we could inspect it, but integration test is mostly about logic here)
    });
  });

  group('FeaturedListingCard', () {
    testWidgets('renders listing info', (WidgetTester tester) async {
      final listing = Listing(
        id: '1',
        fundaId: '123',
        address: 'Test Address',
        price: 500000,
        bedrooms: 3,
        bathrooms: 2,
        livingAreaM2: 120,
      );

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider<FavoritesProvider>(
              create: (_) => FavoritesProvider(),
            ),
          ],
          child: MaterialApp(
            home: Scaffold(
              body: FeaturedListingCard(
                listing: listing,
                onTap: () {},
              ),
            ),
          ),
        ),
      );

      // Pump to settle animations/shimmers
      await tester.pump();
      await tester.pump(const Duration(seconds: 1));

      // The code uses toStringAsFixed(0) which doesn't add commas
      expect(find.text('\$500000'), findsOneWidget);
      expect(find.text('Test Address'), findsOneWidget);
      expect(find.text('3 Bd'), findsOneWidget);
      expect(find.text('2 Ba'), findsOneWidget);
      expect(find.text('120 m²'), findsOneWidget);
    });
  });

  group('NearbyListingCard', () {
    testWidgets('renders listing info', (WidgetTester tester) async {
      final listing = Listing(
        id: '1',
        fundaId: '123',
        address: 'Test Address',
        price: 500000,
        bedrooms: 3,
        bathrooms: 2,
        livingAreaM2: 120,
      );

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider<FavoritesProvider>(
              create: (_) => FavoritesProvider(),
            ),
          ],
          child: MaterialApp(
            home: Scaffold(
              body: NearbyListingCard(
                listing: listing,
                onTap: () {},
              ),
            ),
          ),
        ),
      );

      // Pump to settle animations/shimmers
      await tester.pump();
      await tester.pump(const Duration(seconds: 1));

      expect(find.text('\$500000'), findsOneWidget);
      expect(find.text('Test Address'), findsOneWidget);
      expect(find.text('Active'), findsOneWidget);
    });
  });
}
