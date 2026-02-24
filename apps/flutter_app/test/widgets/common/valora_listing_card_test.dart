import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/widgets/common/valora_listing_card.dart';
import 'package:valora_app/models/saved_listing.dart';
import 'package:cached_network_image/cached_network_image.dart';

// Helper to mock network images
Widget createMockImageWidget() {
  return const SizedBox(width: 100, height: 100);
}

void main() {
  testWidgets('ValoraListingCard renders correctly', (WidgetTester tester) async {
    final listing = ListingSummary(
      id: '1',
      address: '123 Test St',
      city: 'Test City',
      price: 500000,
      imageUrl: 'https://example.com/image.jpg',
      bedrooms: 3,
      livingAreaM2: 100,
    );

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView( // Added scroll view to avoid overflow
            child: ValoraListingCard(
              listing: listing,
              commentCount: 5,
              notes: 'Great house',
            ),
          ),
        ),
      ),
    );

    // Pump once to trigger build
    await tester.pump();
    // Do NOT pumpAndSettle here because CachedNetworkImage might keep retrying or animating forever
    // Just settle a bit
    await tester.pump(const Duration(milliseconds: 500));

    expect(find.text('123 Test St'), findsOneWidget);
    expect(find.text('Test City'), findsOneWidget);
    expect(find.text('3 bed'), findsOneWidget);
    expect(find.text('100 m²'), findsOneWidget);
    expect(find.text('5'), findsOneWidget); // Comment count
    expect(find.text('Great house'), findsOneWidget);
  });

  testWidgets('ValoraListingCard handles nulls gracefully', (WidgetTester tester) async {
    final listing = ListingSummary(
      id: '2',
      address: '456 Null Ave',
      // Null city, price, image, specs
    );

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView(
            child: ValoraListingCard(
              listing: listing,
            ),
          ),
        ),
      ),
    );

    await tester.pumpAndSettle();

    expect(find.text('456 Null Ave'), findsOneWidget);
    expect(find.byIcon(Icons.home_rounded), findsOneWidget); // Placeholder icon
    expect(find.byIcon(Icons.bed_rounded), findsNothing);
  });

  testWidgets('ValoraListingCard onTap works', (WidgetTester tester) async {
    bool tapped = false;
    final listing = ListingSummary(id: '3', address: '789 Tap Blvd');

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: Center( // Center it to ensure it's on screen and hit-testable
            child: ValoraListingCard(
              listing: listing,
              onTap: () => tapped = true,
            ),
          ),
        ),
      ),
    );

    await tester.pump(); // Initial build
    await tester.pump(const Duration(milliseconds: 100)); // Settle slightly

    await tester.tap(find.byType(ValoraListingCard));
    await tester.pump(); // Process tap

    expect(tapped, true);
  });
}
