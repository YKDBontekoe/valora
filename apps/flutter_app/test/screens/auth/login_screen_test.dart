import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/screens/auth/login_screen.dart';

class MockAuthProvider extends ChangeNotifier implements AuthProvider {
  @override
  Future<void> loginWithGoogle() async {}
  @override
  bool get isAuthenticated => false;

  @override
  bool get isLoading => false;

  @override
  String? get token => null;

  @override
  String? get email => null;

  @override
  Future<void> checkAuth() async {}

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
  Future<String?> refreshSession() async {
    return null;
  }
}

class SpyAuthProvider extends MockAuthProvider {
  bool googleLoginCalled = false;

  @override
  Future<void> loginWithGoogle() async {
    googleLoginCalled = true;
  }
}

void main() {
  testWidgets('LoginScreen has AutofillGroup and correct hints', (
    WidgetTester tester,
  ) async {
    final authProvider = MockAuthProvider();

    await tester.binding.setSurfaceSize(const Size(800, 2000));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<AuthProvider>.value(
          value: authProvider,
          child: const LoginScreen(),
        ),
      ),
    );

    // Verify AutofillGroup exists
    expect(find.byType(AutofillGroup), findsOneWidget);

    // Verify Email field hints
    final emailFieldFinder = find.byWidgetPredicate(
      (widget) =>
          widget is TextField &&
          widget.autofillHints != null &&
          widget.autofillHints!.contains(AutofillHints.email),
    );
    expect(emailFieldFinder, findsOneWidget);

    // Verify Password field hints
    final passwordFieldFinder = find.byWidgetPredicate(
      (widget) =>
          widget is TextField &&
          widget.autofillHints != null &&
          widget.autofillHints!.contains(AutofillHints.password),
    );
    expect(passwordFieldFinder, findsOneWidget);
  });

  testWidgets('Tapping Google button calls loginWithGoogle', (
    WidgetTester tester,
  ) async {
    final authProvider = SpyAuthProvider();

    await tester.binding.setSurfaceSize(const Size(800, 2000));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<AuthProvider>.value(
          value: authProvider,
          child: const LoginScreen(),
        ),
      ),
    );

    await tester.tap(find.text('Google'));
    await tester.pump();

    expect(authProvider.googleLoginCalled, isTrue);
  });
}
