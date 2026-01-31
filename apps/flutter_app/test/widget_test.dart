import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:valora_app/main.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/auth_service.dart';

class MockAuthService extends AuthService {
  @override
  Future<String?> getToken() async => "fake_token";

  @override
  Future<Map<String, dynamic>> login(String email, String password) async => {'token': 'fake_token'};
}

void main() {
  testWidgets('App renders home screen', (WidgetTester tester) async {
    // Save the original builder
    final originalBuilder = ErrorWidget.builder;

    try {
      await tester.pumpWidget(
        MultiProvider(
          providers: [
            Provider<AuthService>(create: (_) => MockAuthService()),
            ChangeNotifierProvider<AuthProvider>(create: (_) => AuthProvider(authService: MockAuthService())),
            ProxyProvider<AuthProvider, ApiService>(
              update: (context, auth, _) => ApiService(authToken: auth.token),
            ),
          ],
          child: const ValoraApp(),
        ),
      );
      // Pump to allow animations and auth check to complete
      await tester.pumpAndSettle();

      // Should find Valora text (on Home Screen or Startup Screen)
      // Since token is returned, it should go to Home Screen.
      expect(find.text('Valora'), findsOneWidget);
    } finally {
      // Restore the original builder to avoid polluting other tests
      ErrorWidget.builder = originalBuilder;
    }
  });
}
