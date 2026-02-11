import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/widgets/valora_listing_card.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

// Mock HttpOverrides to intercept network calls
class MockHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return super.createHttpClient(context)
      ..badCertificateCallback =
          (X509Certificate cert, String host, int port) => true;
  }
}

void main() {
  setUpAll(() {
    HttpOverrides.global = MockHttpOverrides();
  });

  final testListing = Listing(
    id: '1',
    fundaId: '123',
    address: 'Test Address 1',
    city: 'Amsterdam',
    postalCode: '1000 AA',
    price: 500000,
    bedrooms: 2,
    livingAreaM2: 100,
    imageUrl: 'https://example.com/image.jpg', // Dummy URL
    status: 'new',
  );

  testWidgets('ValoraListingCard renders listing details', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView(
            child: ValoraListingCard(listing: testListing),
          ),
        ),
      ),
    );

    // Allow network image to "load" (it will likely fail or show placeholder, which is fine)
    // We just want to ensure widgets are built. Avoid pumpAndSettle due to infinite shimmer.
    await tester.pump();

    expect(find.text('Test Address 1'), findsOneWidget);
    expect(find.text('Amsterdam 1000 AA'), findsOneWidget);
    expect(find.textContaining('500.000'), findsOneWidget);
    expect(find.text('NEW'), findsOneWidget); // Status badge
    expect(find.text('2'), findsOneWidget); // Bedrooms
    expect(find.text('100 m²'), findsOneWidget); // Living area

    // Unmount to stop shimmer timer
    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });

  testWidgets('ValoraListingCard handles favorite toggle', (tester) async {
    bool favorited = false;

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView(
            child: StatefulBuilder(
              builder: (context, setState) {
                return ValoraListingCard(
                  listing: testListing,
                  isFavorite: favorited,
                  onFavorite: () => setState(() => favorited = !favorited),
                );
              },
            ),
          ),
        ),
      ),
    );
    // Avoid pumpAndSettle due to shimmer
    await tester.pump();

    // Initially not favorite
    expect(find.byIcon(Icons.favorite_border), findsOneWidget);
    expect(find.byIcon(Icons.favorite), findsNothing);

    // Tap favorite
    await tester.tap(find.byIcon(Icons.favorite_border));
    await tester.pump(); // Start animation
    await tester.pump(const Duration(milliseconds: 500)); // Finish animation

    // Now favorite
    expect(favorited, isTrue);
    expect(find.byIcon(Icons.favorite), findsOneWidget);

    // Unmount to stop shimmer timer
    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });

  testWidgets('ValoraListingCard calls onTap', (tester) async {
    bool tapped = false;
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView(
            child: ValoraListingCard(
              listing: testListing,
              onTap: () => tapped = true,
            ),
          ),
        ),
      ),
    );
    // Avoid pumpAndSettle due to shimmer
    await tester.pump();

    // Find visible widget
    final cardFinder = find.byType(ValoraCard);
    await tester.ensureVisible(cardFinder);

    await tester.tap(cardFinder, warnIfMissed: false);
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300)); // Wait for animation

    expect(tapped, isTrue);

    // Unmount to stop shimmer timer
    await tester.pumpWidget(const SizedBox());
    await tester.pumpAndSettle();
  });
}
