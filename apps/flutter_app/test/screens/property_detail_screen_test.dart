import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/listing_detail.dart';
import 'package:valora_app/screens/property_detail_screen.dart';

void main() {
  testWidgets('PropertyDetailScreen displays listing facts and scores', (WidgetTester tester) async {
    final listing = ListingDetail(
      id: '1',
      address: '123 Main St',
      price: 500000,
      bedrooms: 3,
      bathrooms: 2,
      livingAreaM2: 120,
      contextCompositeScore: 8.5,
    );

    await tester.pumpWidget(MaterialApp(
      home: PropertyDetailScreen(listing: listing),
    ));

    expect(find.text('123 Main St'), findsOneWidget);
    expect(find.text('€500000'), findsOneWidget);
    expect(find.text('3 Beds'), findsOneWidget);
    expect(find.text('2 Baths'), findsOneWidget);
    expect(find.text('120 m²'), findsOneWidget);
    expect(find.text('Composite'), findsOneWidget);
    expect(find.text('8.5'), findsOneWidget);
  });
}
