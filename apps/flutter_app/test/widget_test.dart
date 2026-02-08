import 'dart:io';
import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:valora_app/main.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/providers/theme_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/auth_service.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'mocks/mock_notification_service.dart';
import 'package:valora_app/services/notification_service.dart';

class MockAuthService extends AuthService {
  @override
  Future<String?> getToken() async => "fake_token";

  @override
  Future<Map<String, dynamic>> login(String email, String password) async => {
    'token': 'fake_token',
  };
}

void main() {
  setUpAll(() async {
    TestWidgetsFlutterBinding.ensureInitialized();
    await dotenv.load(fileName: ".env.example");
    SharedPreferences.setMockInitialValues({});
    HttpOverrides.global = MockHttpOverrides();
  });

  testWidgets('App renders home screen', (WidgetTester tester) async {
    // Save the original builder
    final originalBuilder = ErrorWidget.builder;

    try {
      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider<ThemeProvider>(
              create: (_) => ThemeProvider(),
            ),
            ChangeNotifierProvider<FavoritesProvider>(
              create: (_) => FavoritesProvider(),
            ),
            Provider<AuthService>(create: (_) => MockAuthService()),
            ChangeNotifierProvider<AuthProvider>(
              create: (_) => AuthProvider(authService: MockAuthService()),
            ),
            ProxyProvider<AuthProvider, ApiService>(
              update: (context, auth, _) => ApiService(authToken: auth.token),
            ),
            ChangeNotifierProvider<NotificationService>.value(
              value: MockNotificationService(),
            ),
          ],
          child: const ValoraApp(),
        ),
      );
      // Pump to allow animations and auth check to complete
      await tester.pumpAndSettle();

      // With a token, app should navigate to Home and show the default Report tab.
      expect(find.text('Location Context'), findsOneWidget);
    } finally {
      // Restore the original builder to avoid polluting other tests
      ErrorWidget.builder = originalBuilder;
    }
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
  HttpClientResponseCompressionState get compressionState =>
      HttpClientResponseCompressionState.notCompressed;

  @override
  StreamSubscription<List<int>> listen(
    void Function(List<int> event)? onData, {
    Function? onError,
    void Function()? onDone,
    bool? cancelOnError,
  }) {
    return Stream.value(kTransparentImage).listen(
      onData,
      onError: onError,
      onDone: onDone,
      cancelOnError: cancelOnError,
    );
  }
}

const List<int> kTransparentImage = <int>[
  0x89,
  0x50,
  0x4E,
  0x47,
  0x0D,
  0x0A,
  0x1A,
  0x0A,
  0x00,
  0x00,
  0x00,
  0x0D,
  0x49,
  0x48,
  0x44,
  0x52,
  0x00,
  0x00,
  0x00,
  0x01,
  0x00,
  0x00,
  0x00,
  0x01,
  0x08,
  0x06,
  0x00,
  0x00,
  0x00,
  0x1F,
  0x15,
  0xC4,
  0x89,
  0x00,
  0x00,
  0x00,
  0x0A,
  0x49,
  0x44,
  0x41,
  0x54,
  0x78,
  0x9C,
  0x63,
  0x00,
  0x01,
  0x00,
  0x00,
  0x05,
  0x00,
  0x01,
  0x0D,
  0x0A,
  0x2D,
  0xB4,
  0x00,
  0x00,
  0x00,
  0x00,
  0x49,
  0x45,
  0x4E,
  0x44,
  0xAE,
  0x42,
  0x60,
  0x82,
];
