import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/testing.dart';
import 'package:http/http.dart' as http;
import 'package:valora_app/screens/home_screen.dart';
import 'package:valora_app/services/api_service.dart';

void main() {
  group('HomeScreen', () {
    testWidgets('Shows offline state when health check fails', (WidgetTester tester) async {
      final mockClient = MockClient((request) async {
        return http.Response('Error', 500); // Fail health check
      });
      final apiService = ApiService(client: mockClient);

      await tester.pumpWidget(MaterialApp(
        home: HomeScreen(apiService: apiService),
      ));

      // Initial pump
      await tester.pump(); // Start connection check
      await tester.pump(); // Finish connection check

      expect(find.text('Backend not connected'), findsOneWidget);
    });

    testWidgets('Shows listings when connection succeeds', (WidgetTester tester) async {
      final mockClient = MockClient((request) async {
        if (request.url.toString().contains('health')) {
          return http.Response('OK', 200);
        }
        if (request.url.toString().contains('listings')) {
           return http.Response(
              '''
              {
                "items": [{"id": "00000000-0000-0000-0000-000000000000", "fundaId": "1", "address": "Test Street 1", "city": "Test City", "postalCode": "1234AB", "price": 100000, "bedrooms": 2, "bathrooms": 1, "livingAreaM2": 100, "plotAreaM2": 100, "propertyType": "House", "status": "Available", "url": "http://test", "imageUrl": "http://test", "listedDate": "2023-01-01T00:00:00Z", "createdAt": "2023-01-01T00:00:00Z"}],
                "pageIndex": 1,
                "totalPages": 1,
                "totalCount": 1,
                "hasNextPage": false,
                "hasPreviousPage": false
              }
              ''',
              200);
        }
        return http.Response('Not Found', 404);
      });
      final apiService = ApiService(client: mockClient);

      await tester.pumpWidget(MaterialApp(
        home: HomeScreen(apiService: apiService),
      ));

      await tester.pump(); // Start connection check
      await tester.pump(); // Finish connection check and start loading listings
      await tester.pump(); // Finish loading listings

      expect(find.text('Test Street 1'), findsOneWidget);
    });

    testWidgets('Shows SnackBar on listing load error', (WidgetTester tester) async {
      final mockClient = MockClient((request) async {
        if (request.url.toString().contains('health')) {
          return http.Response('OK', 200);
        }
        if (request.url.toString().contains('listings')) {
           return http.Response('Server Error', 500);
        }
        return http.Response('Not Found', 404);
      });
      final apiService = ApiService(client: mockClient);

      await tester.pumpWidget(MaterialApp(
        home: HomeScreen(apiService: apiService),
      ));

      await tester.pump(); // Start connection check
      await tester.pump(); // Finish connection check and start loading listings
      await tester.pump(); // Finish loading listings (and trigger error)
      await tester.pumpAndSettle(); // Wait for SnackBar animation

      expect(find.byType(SnackBar), findsOneWidget);
      expect(find.text('Server error (500). Please try again later.'), findsOneWidget);
    });
  });
}
