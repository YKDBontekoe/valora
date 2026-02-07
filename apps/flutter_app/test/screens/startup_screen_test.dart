import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/screens/startup_screen.dart';

class DelayedAuthProvider extends ChangeNotifier implements AuthProvider {
  DelayedAuthProvider({required this.delay});

  final Duration delay;
  bool checkAuthCalled = false;

  @override
  bool get isAuthenticated => false;

  @override
  bool get isLoading => false;

  @override
  String? get token => null;

  @override
  String? get email => null;

  @override
  Future<void> checkAuth() async {
    checkAuthCalled = true;
    await Future<void>.delayed(delay);
  }

  @override
  Future<void> login(String email, String password) async {}

  @override
  Future<void> logout() async {}

  @override
  Future<void> register(
    String email,
    String password,
    String confirmPassword,
  ) async {}

  @override
  Future<String?> refreshSession() async => null;
}

void main() {
  testWidgets('Startup waits for auth check completion before navigating', (
    WidgetTester tester,
  ) async {
    final DelayedAuthProvider provider = DelayedAuthProvider(
      delay: const Duration(seconds: 3),
    );

    await tester.pumpWidget(
      ChangeNotifierProvider<AuthProvider>.value(
        value: provider,
        child: const MaterialApp(home: StartupScreen()),
      ),
    );

    expect(provider.checkAuthCalled, isTrue);

    await tester.pump(const Duration(milliseconds: 1400));
    expect(find.text('Find your dream home'), findsOneWidget);

    await tester.pump(const Duration(seconds: 2));
    await tester.pumpAndSettle();

    expect(find.text('Welcome Back'), findsOneWidget);
  });
}
