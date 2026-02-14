import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/screens/saved_listings_screen.dart';

class TestHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) => MockHttpClient();
}

class MockHttpClient extends Mock implements HttpClient {
  @override
  Future<HttpClientRequest> getUrl(Uri url) => super.noSuchMethod(
    Invocation.method(#getUrl, [url]),
    returnValue: Future.value(MockHttpClientRequest()),
  );
}

class MockHttpClientRequest extends Mock implements HttpClientRequest {
  @override
  HttpHeaders get headers => super.noSuchMethod(
    Invocation.getter(#headers),
    returnValue: MockHttpHeaders(),
  );

  @override
  Future<HttpClientResponse> close() => super.noSuchMethod(
    Invocation.method(#close, []),
    returnValue: Future.value(MockHttpClientResponse()),
  );
}

class MockHttpClientResponse extends Mock implements HttpClientResponse {
  @override
  int get contentLength =>
      super.noSuchMethod(Invocation.getter(#contentLength), returnValue: 0);
  @override
  int get statusCode =>
      super.noSuchMethod(Invocation.getter(#statusCode), returnValue: 200);
  @override
  HttpClientResponseCompressionState get compressionState => super.noSuchMethod(
    Invocation.getter(#compressionState),
    returnValue: HttpClientResponseCompressionState.notCompressed,
  );

  @override
  StreamSubscription<List<int>> listen(
    void Function(List<int> event)? onData, {
    Function? onError,
    void Function()? onDone,
    bool? cancelOnError,
  }) {
    return super.noSuchMethod(
      Invocation.method(
        #listen,
        [onData],
        {#onError: onError, #onDone: onDone, #cancelOnError: cancelOnError},
      ),
      returnValue: Stream<List<int>>.empty().listen(null),
    );
  }
}

class MockHttpHeaders extends Mock implements HttpHeaders {}

void main() {
  final listing1 = {
    "id": "1",
    "fundaId": "1",
    "address": "A Street",
    "city": "Amsterdam",
    "postalCode": "1000AA",
    "price": 500000.0,
    "bedrooms": 2,
    "bathrooms": 1,
    "livingAreaM2": 100,
    "plotAreaM2": 100,
    "propertyType": "House",
    "status": "Available",
    "url": "http://test",
    "imageUrl": "http://test",
    "listedDate": "2023-01-01T00:00:00Z",
    "createdAt": "2023-01-01T00:00:00Z",
  };

  setUp(() {
    SharedPreferences.setMockInitialValues({
      'favorite_listings': [json.encode(listing1)],
    });
    HttpOverrides.global = TestHttpOverrides();
  });

  Widget createSavedListingsScreen() {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<FavoritesProvider>(
          create: (_) => FavoritesProvider(),
        ),
      ],
      child: const MaterialApp(home: Scaffold(body: SavedListingsScreen())),
    );
  }

  group('SavedListingsScreen', () {
    testWidgets('Shows saved listings', (WidgetTester tester) async {
      // Use runAsync to handle timers from animations
      await tester.runAsync(() async {
        await tester.pumpWidget(createSavedListingsScreen());
        await tester.pump(const Duration(seconds: 1));
      });

      expect(find.text('A Street'), findsOneWidget);
    });
  });
}
