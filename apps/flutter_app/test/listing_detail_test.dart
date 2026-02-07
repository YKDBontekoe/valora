import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/screens/listing_detail_screen.dart';

void main() {
  setUpAll(() {
    SharedPreferences.setMockInitialValues({});
  });

  testWidgets('ListingDetailScreen displays listing details', (
    WidgetTester tester,
  ) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address 123',
      city: 'Test City',
      postalCode: '1234 AB',
      price: 500000,
      bedrooms: 3,
      bathrooms: 2,
      livingAreaM2: 120,
      plotAreaM2: 200,
      propertyType: 'House',
      status: 'New',
      url: 'https://example.com',
      imageUrl: 'https://example.com/image.jpg',
    );

    // Provide a MediaQuery to ensure layout works (though MaterialApp provides it)
    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<FavoritesProvider>(
            create: (_) => FavoritesProvider(),
          ),
        ],
        child: MaterialApp(home: ListingDetailScreen(listing: listing)),
      ),
    );
    // Use finite pump instead of pumpAndSettle to avoid timeouts with infinite animations (like shimmer)
    await tester.pump();
    await tester.pump(const Duration(seconds: 2));

    // Verify address
    expect(find.text('Test Address 123'), findsOneWidget);

    // Verify city and postal code
    expect(find.text('Test City 1234 AB'), findsOneWidget);

    // Verify price (assuming ValoraPrice formatting '€ 500.000')
    expect(find.textContaining('€ 500.000'), findsOneWidget);

    // Verify specs
    expect(find.text('Bedrooms'), findsOneWidget);
    expect(find.text('3'), findsOneWidget);
    expect(find.text('Bathrooms'), findsOneWidget);
    expect(find.text('2'), findsOneWidget);
    expect(find.text('Living Area'), findsOneWidget);
    expect(find.text('120 m²'), findsOneWidget);

    // Verify button
    expect(find.text('View on Funda'), findsOneWidget);
  });

  testWidgets('Contact Broker triggers confirmation dialog', (
    WidgetTester tester,
  ) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address 123',
      brokerPhone: '+31612345678',
      agentName: 'Test Agent',
    );

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<FavoritesProvider>(
            create: (_) => FavoritesProvider(),
          ),
        ],
        child: MaterialApp(home: ListingDetailScreen(listing: listing)),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));

    // Ensure viewport is large enough or scrollable
    tester.view.physicalSize = const Size(1080, 2400);
    addTearDown(tester.view.resetPhysicalSize);

    // Scroll to bottom to ensure button is visible
    // ListingDetailScreen uses CustomScrollView
    await tester.drag(find.byType(CustomScrollView), const Offset(0, -1000));
    await tester.pumpAndSettle();

    final contactButton = find.text('Contact Broker');
    // Ensure button is visible before tapping
    await tester.scrollUntilVisible(
      contactButton,
      200,
      scrollable: find.byType(Scrollable).first,
    );
    expect(contactButton, findsOneWidget);

    await tester.tap(contactButton);
    await tester.pumpAndSettle();

    // Verify dialog appears
    expect(find.text('Call Broker?'), findsOneWidget);
    expect(
      find.text('Do you want to call Test Agent at +31612345678?'),
      findsOneWidget,
    );

    // Cancel
    await tester.tap(find.text('Cancel'));
    await tester.pumpAndSettle();

    // Verify dialog closed
    expect(find.text('Call Broker?'), findsNothing);
  });
}
