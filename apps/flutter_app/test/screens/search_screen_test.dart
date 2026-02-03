import 'dart:io';
import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:http/testing.dart';
import 'package:http/http.dart' as http;
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/screens/search_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/widgets/valora_widgets.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:retry/retry.dart';

void main() {
  setUpAll(() async {
    await dotenv.load(fileName: ".env.example");
    SharedPreferences.setMockInitialValues({});
    HttpOverrides.global = MockHttpOverrides();
  });

  Widget createSearchScreen(ApiService apiService) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<FavoritesProvider>(
          create: (_) => FavoritesProvider(),
        ),
        Provider<ApiService>.value(value: apiService),
      ],
      child: const MaterialApp(
        home: SearchScreen(),
      ),
    );
  }

  group('SearchScreen', () {
    testWidgets('Shows initial state', (WidgetTester tester) async {
      final mockClient = MockClient((request) async {
         return http.Response('OK', 200);
      });
      final apiService = ApiService(
          client: mockClient, retryOptions: const RetryOptions(maxAttempts: 1));

      await tester.pumpWidget(createSearchScreen(apiService));
      await tester.pumpAndSettle();

      expect(find.text('Find your home'), findsOneWidget);
    });

    testWidgets('Performs search and shows results', (WidgetTester tester) async {
      final mockClient = MockClient((request) async {
        if (request.url.toString().contains('listings')) {
          return http.Response(
              '''
              {
                "items": [{"id": "1", "fundaId": "1", "address": "Search Result 1", "city": "Test City", "postalCode": "1234AB", "price": 100000, "bedrooms": 2, "bathrooms": 1, "livingAreaM2": 100, "plotAreaM2": 100, "propertyType": "House", "status": "Available", "url": "http://test", "imageUrl": "http://test", "listedDate": "2023-01-01T00:00:00Z", "createdAt": "2023-01-01T00:00:00Z"}],
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
      final apiService = ApiService(
          client: mockClient, retryOptions: const RetryOptions(maxAttempts: 1));

      await tester.pumpWidget(createSearchScreen(apiService));
      await tester.pumpAndSettle();

      // Enter text
      await tester.enterText(find.byType(ValoraTextField), 'Test');

      // Wait for debounce (500ms) + buffer
      await tester.pump(const Duration(milliseconds: 600));

      // Wait for API call and UI update
      await tester.pump();

      expect(find.text('Search Result 1'), findsOneWidget);
    });

    testWidgets('Shows empty state for no results', (WidgetTester tester) async {
       final mockClient = MockClient((request) async {
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
      final apiService = ApiService(
          client: mockClient, retryOptions: const RetryOptions(maxAttempts: 1));

      await tester.pumpWidget(createSearchScreen(apiService));
      await tester.pumpAndSettle();

      // Enter text
      await tester.enterText(find.byType(ValoraTextField), 'Nothing');
      await tester.pump(const Duration(milliseconds: 600));
      await tester.pump();

      expect(find.text('No results found'), findsOneWidget);
    });

    testWidgets('Shows error state on API failure', (WidgetTester tester) async {
       final mockClient = MockClient((request) async {
        if (request.url.toString().contains('listings')) {
          return http.Response('Server Error', 500);
        }
        return http.Response('Not Found', 404);
      });
      final apiService = ApiService(
          client: mockClient, retryOptions: const RetryOptions(maxAttempts: 1));

      await tester.pumpWidget(createSearchScreen(apiService));
      await tester.pumpAndSettle();

      // Enter text
      await tester.enterText(find.byType(ValoraTextField), 'Error');
      await tester.pump(const Duration(milliseconds: 600));
      await tester.pump();

      expect(find.text('Search Failed'), findsOneWidget);
    });
  });
}

class MockHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return MockHttpClient();
  }
}

class MockHttpClient extends Fake implements HttpClient {
  @override
  bool autoUncompress = true;

  @override
  Future<HttpClientRequest> getUrl(Uri url) async {
    return MockHttpClientRequest();
  }
}

class MockHttpClientRequest extends Fake implements HttpClientRequest {
  @override
  Future<HttpClientResponse> close() async {
    return MockHttpClientResponse();
  }
}

class MockHttpClientResponse extends Fake implements HttpClientResponse {
  @override
  int get statusCode => 200;

  @override
  int get contentLength => kTransparentImage.length;

  @override
  HttpClientResponseCompressionState get compressionState => HttpClientResponseCompressionState.notCompressed;

  @override
  StreamSubscription<List<int>> listen(void Function(List<int> event)? onData, {Function? onError, void Function()? onDone, bool? cancelOnError}) {
    return Stream.value(kTransparentImage).listen(onData, onError: onError, onDone: onDone, cancelOnError: cancelOnError);
  }
}

const List<int> kTransparentImage = <int>[
  0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49,
  0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06,
  0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44,
  0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01, 0x0D,
  0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42,
  0x60, 0x82,
];
