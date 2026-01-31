import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/testing.dart';
import 'package:http/http.dart' as http;
import 'package:valora_app/screens/home_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/widgets/valora_filter_dialog.dart';

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

    testWidgets('Shows error state on listing load error', (WidgetTester tester) async {
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
      await tester.pumpAndSettle(); // Wait for animation

      expect(find.text('Server Error'), findsOneWidget);
      expect(find.text('Server error (500). Please try again later.'), findsOneWidget);
    });

    testWidgets('Shows SnackBar on pagination load error', (WidgetTester tester) async {
      int callCount = 0;
      final mockClient = MockClient((request) async {
        if (request.url.toString().contains('health')) {
          return http.Response('OK', 200);
        }
        if (request.url.toString().contains('listings')) {
           callCount++;
           if (callCount == 1) {
             // First page success
             return http.Response(
                '''
                {
                  "items": [{"id": "00000000-0000-0000-0000-000000000000", "fundaId": "1", "address": "Test Street 1", "city": "Test City", "postalCode": "1234AB", "price": 100000, "bedrooms": 2, "bathrooms": 1, "livingAreaM2": 100, "plotAreaM2": 100, "propertyType": "House", "status": "Available", "url": "http://test", "imageUrl": "http://test", "listedDate": "2023-01-01T00:00:00Z", "createdAt": "2023-01-01T00:00:00Z"}],
                  "pageIndex": 1,
                  "totalPages": 2,
                  "totalCount": 2,
                  "hasNextPage": true,
                  "hasPreviousPage": false
                }
                ''',
                200);
           } else {
             // Second page failure
             return http.Response('Server Error', 500);
           }
        }
        return http.Response('Not Found', 404);
      });
      final apiService = ApiService(client: mockClient);

      await tester.pumpWidget(MaterialApp(
        home: HomeScreen(apiService: apiService),
      ));

      // Use pump with duration instead of pumpAndSettle because CircularProgressIndicator is visible (hasNextPage=true)
      await tester.pump(); // Start connection check
      await tester.pump(); // Finish connection check and start loading listings
      await tester.pump(const Duration(seconds: 1)); // Wait for animations (SlideInItem)

      expect(find.text('Test Street 1'), findsOneWidget);

      // Scroll to bottom
      await tester.drag(find.byType(ListView), const Offset(0, -500));
      await tester.pump(); // Start loading more
      await tester.pump(); // Trigger load
      await tester.pump(); // Finish load (error)
      await tester.pump(); // Start SnackBar animation

      expect(find.byType(SnackBar), findsOneWidget);
      expect(find.text('Server error (500). Please try again later.'), findsOneWidget);
    });

    testWidgets('Filter dialog interactions', (WidgetTester tester) async {
      final mockClient = MockClient((request) async {
        if (request.url.toString().contains('health')) {
          return http.Response('OK', 200);
        }
        if (request.url.toString().contains('listings')) {
           return http.Response(
              '''
              {
                "items": [],
                "pageIndex": 1,
                "totalPages": 1,
                "totalCount": 0,
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

      await tester.pumpAndSettle();

      // Open filter dialog
      await tester.tap(find.byIcon(Icons.filter_list));
      await tester.pumpAndSettle();

      expect(find.byType(ValoraFilterDialog), findsOneWidget);

      // Enter price
      await tester.enterText(find.widgetWithText(TextField, 'Min'), '100000');
      await tester.enterText(find.widgetWithText(TextField, 'Max'), '500000');

      // Apply
      await tester.tap(find.text('Apply'));
      await tester.pumpAndSettle();

      expect(find.byType(ValoraFilterDialog), findsNothing);
    });

    testWidgets('Search bar interaction', (WidgetTester tester) async {
      final mockClient = MockClient((request) async {
        if (request.url.toString().contains('health')) return http.Response('OK', 200);
        return http.Response(
            '''
            {
              "items": [], "pageIndex": 1, "totalPages": 1, "totalCount": 0, "hasNextPage": false, "hasPreviousPage": false
            }
            ''', 200);
      });
      final apiService = ApiService(client: mockClient);

      await tester.pumpWidget(MaterialApp(home: HomeScreen(apiService: apiService)));
      await tester.pumpAndSettle();

      // Tap search icon
      await tester.tap(find.byIcon(Icons.search));
      await tester.pump();

      // Verify search field appears
      expect(find.byType(TextField), findsOneWidget);

      // Type in search
      await tester.enterText(find.byType(TextField), 'Amsterdam');
      await tester.pump(const Duration(milliseconds: 600)); // Wait for debounce
    });

    testWidgets('Clears filters via empty state action', (WidgetTester tester) async {
      final mockClient = MockClient((request) async {
        if (request.url.toString().contains('health')) return http.Response('OK', 200);
        // Return empty list
        return http.Response(
            '''
            {
              "items": [], "pageIndex": 1, "totalPages": 1, "totalCount": 0, "hasNextPage": false, "hasPreviousPage": false
            }
            ''', 200);
      });
      final apiService = ApiService(client: mockClient);

      await tester.pumpWidget(MaterialApp(home: HomeScreen(apiService: apiService)));
      await tester.pumpAndSettle();

      // Verify empty state
      expect(find.text('No listings found'), findsOneWidget);
      expect(find.text('Clear Filters'), findsOneWidget);

      // Tap clear
      await tester.tap(find.text('Clear Filters'));
      await tester.pumpAndSettle();
    });
  });
}
