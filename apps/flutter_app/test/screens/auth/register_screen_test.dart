import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/screens/auth/register_screen.dart';

class MockAuthProvider extends ChangeNotifier implements AuthProvider {
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

void main() {
  testWidgets('RegisterScreen has AutofillGroup and correct hints', (
    WidgetTester tester,
  ) async {
    final authProvider = MockAuthProvider();

    await tester.pumpWidget(
      MaterialApp(
        home: ChangeNotifierProvider<AuthProvider>.value(
          value: authProvider,
          child: const RegisterScreen(),
        ),
      ),
    );

    // Verify AutofillGroup exists
    expect(find.byType(AutofillGroup), findsOneWidget);

    // Verify Email field hints
    final emailFieldFinder = find.descendant(
      of: find.widgetWithText(TextFormField, 'Email'),
      matching: find.byType(TextField),
    );
    expect(emailFieldFinder, findsOneWidget);
    final emailTextField = tester.widget<TextField>(emailFieldFinder);
    expect(emailTextField.autofillHints, contains(AutofillHints.email));

    // Verify Password field hints
    final passwordFieldFinder = find.descendant(
      of: find.widgetWithText(TextFormField, 'Password'),
      matching: find.byType(TextField),
    );
    expect(passwordFieldFinder, findsOneWidget);
    final passwordTextField = tester.widget<TextField>(passwordFieldFinder);
    expect(
      passwordTextField.autofillHints,
      contains(AutofillHints.newPassword),
    );

    // Verify Confirm Password field hints
    final confirmPasswordFieldFinder = find.descendant(
      of: find.widgetWithText(TextFormField, 'Confirm Password'),
      matching: find.byType(TextField),
    );
    expect(confirmPasswordFieldFinder, findsOneWidget);
    final confirmPasswordTextField = tester.widget<TextField>(
      confirmPasswordFieldFinder,
    );
    expect(
      confirmPasswordTextField.autofillHints,
      contains(AutofillHints.newPassword),
    );
  });
}
