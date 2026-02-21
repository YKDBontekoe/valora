import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/screens/property_detail_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/models/property_detail.dart';
import 'package:valora_app/core/theme/valora_theme.dart';

class MockApiService extends ApiService {
  MockApiService() : super();

  @override
  Future<PropertyDetail> getPropertyDetail(String id) async {
    return PropertyDetail(
      id: id,
      address: 'Test Street 1',
      price: 500000,
      bedrooms: 3,
      livingAreaM2: 120,
      imageUrls: ['https://example.com/image.jpg'],
    );
  }
}

void main() {
  testWidgets('PropertyDetailScreen shows property details', (WidgetTester tester) async {
    await tester.pumpWidget(MaterialApp(
      theme: ValoraTheme.light,
      home: PropertyDetailScreen(
        propertyId: '123',
        apiService: MockApiService(),
      ),
    ));

    await tester.pumpAndSettle();

    expect(find.text('Test Street 1'), findsOneWidget);
    // Use partial match or ensure non-breaking space matches
    expect(find.textContaining('3'), findsAtLeastNWidgets(1));
    expect(find.textContaining('120 m²'), findsOneWidget);
  });
}
