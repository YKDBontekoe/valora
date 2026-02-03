import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/screens/property_details_screen.dart';

void main() {
  testWidgets('PropertyDetailsScreen displays new design elements',
      (WidgetTester tester) async {
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
      imageUrl: null, // Use null to avoid network image issues
    );

    // Set a large surface size to ensure all widgets are rendered (scrolling)
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 1.0;

    await tester.pumpWidget(
      MaterialApp(
        home: PropertyDetailsScreen(listing: listing),
      ),
    );

    // Verify header
    expect(find.text('Test Address 123'), findsOneWidget);
    // Note: The UI constructs 'City, PostalCode'
    expect(find.text('Test City, 1234 AB'), findsOneWidget);

    // Verify AI Sentiment
    expect(find.text('AI Neighborhood Sentiment'), findsOneWidget);
    expect(find.text('Safety'), findsOneWidget);
    expect(find.text('Quiet'), findsOneWidget);

    // Verify Price History
    expect(find.text('Price History & Forecast'), findsOneWidget);
    expect(find.text('Estimated Value in 2025'), findsOneWidget);

    // Verify Market Comparison
    expect(find.text('Local Market Comparison'), findsOneWidget);
    expect(find.text('Price per SqFt'), findsOneWidget);

    // Verify Buttons
    expect(find.text('AI Chat'), findsOneWidget);
    expect(find.text('Book Viewing'), findsOneWidget);

    // Reset size
    addTearDown(tester.view.resetPhysicalSize);
  });
}
