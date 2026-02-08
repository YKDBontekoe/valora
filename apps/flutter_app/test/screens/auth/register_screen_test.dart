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
    final emailFieldFinder = find.widgetWithText(
      TextField,
      'hello@example.com',
    );
    expect(emailFieldFinder, findsOneWidget);
    final emailTextField = tester.widget<TextField>(emailFieldFinder);
    expect(emailTextField.autofillHints, contains(AutofillHints.email));

    // Verify Password field hints
    final passwordFieldFinder = find
        .widgetWithText(TextField, '••••••••')
        .first;
    expect(passwordFieldFinder, findsOneWidget);
    final passwordTextField = tester.widget<TextField>(passwordFieldFinder);
    expect(
      passwordTextField.autofillHints,
      contains(AutofillHints.newPassword),
    );

    // Verify Confirm Password field hints
    final confirmPasswordFieldFinder = find
        .widgetWithText(TextField, '••••••••')
        .last;
    expect(confirmPasswordFieldFinder, findsOneWidget);
    final confirmPasswordTextField = tester.widget<TextField>(
      confirmPasswordFieldFinder,
    );
    expect(
      confirmPasswordTextField.autofillHints,
      contains(AutofillHints.newPassword),
    );
  });

  testWidgets('RegisterScreen toggles password visibility', (
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

    final Finder passwordTextFields = find.widgetWithText(
      TextField,
      '••••••••',
    );
    expect(passwordTextFields, findsNWidgets(2));

    final TextField passwordFieldBefore = tester.widget<TextField>(
      passwordTextFields.first,
    );
    expect(passwordFieldBefore.obscureText, isTrue);

    await tester.tap(find.byIcon(Icons.visibility_off_outlined).first);
    await tester.pumpAndSettle();

    final TextField passwordFieldAfter = tester.widget<TextField>(
      find.widgetWithText(TextField, '••••••••').first,
    );
    expect(passwordFieldAfter.obscureText, isFalse);
  });
}
