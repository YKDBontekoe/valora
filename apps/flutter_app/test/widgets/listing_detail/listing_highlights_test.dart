import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/widgets/listing_detail/listing_highlights.dart';
import 'package:valora_app/widgets/valora_widgets.dart';

void main() {
  Widget createWidgetUnderTest(Listing listing) {
    return MaterialApp(
      home: Scaffold(
        body: ListingHighlights(listing: listing),
      ),
    );
  }

  testWidgets('ListingHighlights renders nothing when no features present', (tester) async {
    final listing = Listing(id: '1', fundaId: '1', address: 'A');

    await tester.pumpWidget(createWidgetUnderTest(listing));

    expect(find.byType(ValoraTag), findsNothing);
  });

  testWidgets('ListingHighlights renders energy label', (tester) async {
    final listing = Listing(
      id: '1', fundaId: '1', address: 'A',
      energyLabel: 'A++',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));

    expect(find.widgetWithText(ValoraTag, 'Label A++'), findsOneWidget);
    expect(find.byIcon(Icons.energy_savings_leaf_rounded), findsOneWidget);
  });

  testWidgets('ListingHighlights renders year built', (tester) async {
    final listing = Listing(
      id: '1', fundaId: '1', address: 'A',
      yearBuilt: 2020,
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));

    expect(find.widgetWithText(ValoraTag, 'Built 2020'), findsOneWidget);
    expect(find.byIcon(Icons.calendar_today_rounded), findsOneWidget);
  });

  testWidgets('ListingHighlights renders ownership type', (tester) async {
    final listing = Listing(
      id: '1', fundaId: '1', address: 'A',
      ownershipType: 'Volle eigendom',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));

    expect(find.widgetWithText(ValoraTag, 'Volle eigendom'), findsOneWidget);
    expect(find.byIcon(Icons.gavel_rounded), findsOneWidget);
  });

  testWidgets('ListingHighlights renders heating type', (tester) async {
    final listing = Listing(
      id: '1', fundaId: '1', address: 'A',
      heatingType: 'Stadsverwarming',
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));

    expect(find.widgetWithText(ValoraTag, 'Stadsverwarming'), findsOneWidget);
    expect(find.byIcon(Icons.thermostat_rounded), findsOneWidget);
  });

  testWidgets('ListingHighlights renders garage', (tester) async {
    final listing = Listing(
      id: '1', fundaId: '1', address: 'A',
      hasGarage: true,
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));

    expect(find.widgetWithText(ValoraTag, 'Garage'), findsOneWidget);
    expect(find.byIcon(Icons.garage_rounded), findsOneWidget);
  });

  testWidgets('ListingHighlights renders multiple highlights', (tester) async {
    final listing = Listing(
      id: '1', fundaId: '1', address: 'A',
      energyLabel: 'A',
      hasGarage: true,
    );

    await tester.pumpWidget(createWidgetUnderTest(listing));

    expect(find.byType(ValoraTag), findsNWidgets(2));
  });
}
