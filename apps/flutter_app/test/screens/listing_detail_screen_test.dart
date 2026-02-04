import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/screens/listing_detail_screen.dart';

@GenerateMocks([FavoritesProvider])
import 'listing_detail_screen_test.mocks.dart';

void main() {
  late MockFavoritesProvider mockFavoritesProvider;

  setUp(() {
    mockFavoritesProvider = MockFavoritesProvider();
    when(mockFavoritesProvider.isFavorite(any)).thenReturn(false);
    HttpOverrides.global = null;
  });

  Widget createWidgetUnderTest(Listing listing) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<FavoritesProvider>.value(value: mockFavoritesProvider),
      ],
      child: MaterialApp(
        home: ListingDetailScreen(listing: listing),
      ),
    );
  }

  testWidgets('ListingDetailScreen shows "Contact Broker" button when phone is present', (WidgetTester tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      brokerPhone: '+1234567890',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));

    expect(find.text('Contact Broker'), findsOneWidget);
    expect(find.byIcon(Icons.phone_rounded), findsOneWidget);
  });

  testWidgets('ListingDetailScreen hides "Contact Broker" button when phone is missing', (WidgetTester tester) async {
    final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      brokerPhone: null,
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));

    expect(find.text('Contact Broker'), findsNothing);
  });

  testWidgets('ListingDetailScreen shows broker section if logo present', (WidgetTester tester) async {
     final listing = Listing(
      id: '1',
      fundaId: '123',
      address: 'Test Address',
      brokerLogoUrl: 'http://example.com/logo.png',
      agentName: 'Test Agent',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));
    // Wait for any pending timers (like image loading retries) to settle
    await tester.pumpAndSettle();

    expect(find.text('Broker'), findsOneWidget);
    expect(find.text('Test Agent'), findsOneWidget);
  });
}
