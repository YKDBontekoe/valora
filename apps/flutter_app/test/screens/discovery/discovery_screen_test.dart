import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:valora_app/screens/discovery/discovery_screen.dart';
import 'package:valora_app/providers/discovery_provider.dart';
import 'package:valora_app/repositories/listing_repository.dart';
import 'package:valora_app/models/listing_search_request.dart';
import 'package:valora_app/models/listing.dart';

import 'discovery_screen_test.mocks.dart';

@GenerateMocks([ListingRepository])
void main() {
  testWidgets('DiscoveryScreen shows filters and triggers update', (WidgetTester tester) async {
    final mockRepo = MockListingRepository();
    when(mockRepo.searchListings(any)).thenAnswer((_) async => <Listing>[]);

    final provider = DiscoveryProvider(mockRepo);

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<DiscoveryProvider>.value(
          value: provider,
          child: const DiscoveryScreen(),
        ),
      ),
    );

    // Verify initial state
    expect(find.text('Discover Homes'), findsOneWidget);

    // Open filter drawer
    await tester.tap(find.byIcon(Icons.filter_list));
    await tester.pumpAndSettle();

    expect(find.text('Filters'), findsOneWidget);
    expect(find.text('Property Type'), findsOneWidget);

    // Tap a filter chip
    await tester.tap(find.text('Appartement'));
    await tester.pumpAndSettle();

    // Verify provider state updated
    expect(provider.propertyType, 'Appartement');

    // Tap to apply filters
    await tester.tap(find.text('Apply Filters'));
    await tester.pumpAndSettle();

    // Drawer should be closed
    expect(find.text('Filters'), findsNothing);
  });
}
