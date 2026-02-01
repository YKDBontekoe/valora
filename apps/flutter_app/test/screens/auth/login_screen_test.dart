import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/screens/auth/login_screen.dart';

class MockAuthProvider extends ChangeNotifier implements AuthProvider {
  @override
  bool get isAuthenticated => false;

  @override
  bool get isLoading => false;

  @override
  String? get token => null;

  @override
  Future<void> checkAuth() async {}

  @override
  Future<void> login(String email, String password) async {}

  @override
  Future<void> logout() async {}

  @override
  Future<void> register(String email, String password, String confirmPassword) async {}
}

void main() {
  testWidgets('LoginScreen has AutofillGroup and correct hints', (WidgetTester tester) async {
    final authProvider = MockAuthProvider();

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
      (widget) => widget is TextField &&
                  widget.autofillHints != null &&
                  widget.autofillHints!.contains(AutofillHints.email)
    );
    expect(emailFieldFinder, findsOneWidget);

    // Verify Password field hints
    final passwordFieldFinder = find.byWidgetPredicate(
      (widget) => widget is TextField &&
                  widget.autofillHints != null &&
                  widget.autofillHints!.contains(AutofillHints.password)
    );
    expect(passwordFieldFinder, findsOneWidget);
  });
}
