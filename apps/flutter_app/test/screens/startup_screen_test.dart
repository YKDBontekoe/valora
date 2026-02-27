import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/screens/startup_screen.dart';
import 'package:valora_app/widgets/valora_error_state.dart';

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
  Future<void> loginWithGoogle() async {}

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

class FailingAuthProvider extends DelayedAuthProvider {
  FailingAuthProvider({required super.delay});

  @override
  Future<void> checkAuth() async {
    checkAuthCalled = true;
    await Future<void>.delayed(delay);
    throw Exception('Simulated Auth Failure');
  }
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

    await tester.pump(const Duration(milliseconds: 300));
    expect(find.text('Find your dream home'), findsOneWidget);

    await tester.pump(const Duration(milliseconds: 300));
    await tester.pumpAndSettle();
    expect(find.text('Welcome Back'), findsOneWidget);
  });

  testWidgets('Startup shows error state when auth check fails', (
    WidgetTester tester,
  ) async {
    final FailingAuthProvider provider = FailingAuthProvider(
      delay: const Duration(milliseconds: 100),
    );

    await tester.pumpWidget(
      ChangeNotifierProvider<AuthProvider>.value(
        value: provider,
        child: const MaterialApp(home: StartupScreen()),
      ),
    );

    expect(provider.checkAuthCalled, isTrue);

    // Pump past the delay and animation
    await tester.pumpAndSettle();

    // Verify error state is shown
    expect(find.byType(ValoraErrorState), findsOneWidget);
    expect(find.text('Something went wrong'), findsOneWidget);
    expect(find.text('Try Again'), findsOneWidget);

    // Test Retry Logic
    provider.checkAuthCalled = false; // Reset flag

    // Tap Retry
    await tester.tap(find.text('Try Again'));
    await tester.pump();

    // Verify loading/splash state returns (animations restart)
    expect(find.text('Find your dream home'), findsOneWidget);
    expect(provider.checkAuthCalled, isTrue);

    // Wait for the simulated delay to complete so the timer is disposed
    await tester.pump(const Duration(milliseconds: 200)); // > 100ms delay

    // Pump and settle to handle the completion of the future
    await tester.pumpAndSettle();
  });
}
