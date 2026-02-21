import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/providers/insights_provider.dart';
import 'package:valora_app/providers/theme_provider.dart';
import 'package:valora_app/screens/home_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/auth_service.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/widgets/insights/insights_map.dart';
import 'settings_screen_test.mocks.dart';

class _TestHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    final client = MockHttpClient();
    final request = MockHttpClientRequest();
    final response = MockHttpClientResponse();
    final headers = MockHttpHeaders();

    when(client.getUrl(any)).thenAnswer((_) async => request);
    when(request.headers).thenReturn(headers);
    when(request.close()).thenAnswer((_) async => response);
    when(response.contentLength).thenReturn(_transparentImage.length);
    when(response.statusCode).thenReturn(HttpStatus.ok);
    when(response.compressionState)
        .thenReturn(HttpClientResponseCompressionState.notCompressed);
    when(response.listen(any)).thenAnswer((invocation) {
      final void Function(List<int>) onData = invocation.positionalArguments[0];
      final void Function()? onDone =
          invocation.namedArguments[#onDone] as void Function()?;
      final Function? onError =
          invocation.namedArguments[#onError] as Function?;
      final bool cancelOnError =
          (invocation.namedArguments[#cancelOnError] as bool?) ?? false;

      return Stream<List<int>>.fromIterable([_transparentImage]).listen(
        onData,
        onDone: onDone,
        onError: onError,
        cancelOnError: cancelOnError,
      );
    });

    return client;
  }
}

const List<int> _transparentImage = <int>[
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

class _TestNotificationService extends NotificationService {
  _TestNotificationService(super.apiService);

  @override
  void startPolling() {}
}

class _TestInsightsProvider extends InsightsProvider {
  _TestInsightsProvider(super.apiService);

  @override
  Future<void> loadInsights() async {
    // Keep tests deterministic and offline.
  }
}

void main() {
  setUp(() {
    HttpOverrides.global = _TestHttpOverrides();
  });

  tearDown(() {
    HttpOverrides.global = null;
  });

  testWidgets('HomeScreen navigation matches tab content', (tester) async {
    final apiService = ApiService();

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiService>.value(value: apiService),
          ChangeNotifierProvider<NotificationService>(
            create: (_) => _TestNotificationService(apiService),
          ),
          ChangeNotifierProvider<InsightsProvider>(
            create: (_) => _TestInsightsProvider(apiService),
          ),
          ChangeNotifierProvider<ThemeProvider>(create: (_) => ThemeProvider()),
          ChangeNotifierProvider<AuthProvider>(
            create: (_) => AuthProvider(authService: AuthService()),
          ),
        ],
        child: const MaterialApp(home: HomeScreen()),
      ),
    );

    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));
    expect(find.text('Search Property'), findsOneWidget);

    await tester.tap(find.byTooltip('Insights'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));
    expect(find.byType(InsightsMap), findsOneWidget);

    await tester.tap(find.byTooltip('Settings'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));
    expect(find.text('Log Out'), findsOneWidget);
  });
}
