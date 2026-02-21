import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/screens/startup_screen.dart';

class DelayedAuthProvider extends ChangeNotifier implements AuthProvider {
  @override
  Future<void> loginWithGoogle() async {}
  DelayedAuthProvider({required this.delay, this.shouldThrow = false});

  final Duration delay;
  final bool shouldThrow;
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
    if (shouldThrow) {
      throw Exception('Auth failed');
    }
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

  testWidgets('Startup keeps splash visible briefly when auth is immediate', (
    WidgetTester tester,
  ) async {
    final DelayedAuthProvider provider = DelayedAuthProvider(
      delay: Duration.zero,
    );

    await tester.pumpWidget(
      ChangeNotifierProvider<AuthProvider>.value(
        value: provider,
        child: const MaterialApp(home: StartupScreen()),
      ),
    );

    await tester.pump(const Duration(milliseconds: 600));
    expect(find.text('Find your dream home'), findsOneWidget);

    await tester.pump(const Duration(milliseconds: 800));
    await tester.pumpAndSettle();
    expect(find.text('Welcome Back'), findsOneWidget);
  });

  testWidgets('Startup handles auth check failure gracefully', (
    WidgetTester tester,
  ) async {
    final DelayedAuthProvider provider = DelayedAuthProvider(
      delay: Duration.zero,
      shouldThrow: true,
    );

    await tester.pumpWidget(
      ChangeNotifierProvider<AuthProvider>.value(
        value: provider,
        child: const MaterialApp(home: StartupScreen()),
      ),
    );

    // Pump to trigger auth check and subsequent animation/navigation
    // The exception is caught internally, so no crash should occur.
    await tester.pumpAndSettle(StartupScreen.splashDuration + const Duration(seconds: 1));

    // Should still navigate to home (or login, represented by Welcome Back placeholde in test app)
    expect(find.text('Welcome Back'), findsOneWidget);
  });
}
