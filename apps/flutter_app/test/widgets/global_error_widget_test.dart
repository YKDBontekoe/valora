import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/widgets/global_error_widget.dart';
import 'package:valora_app/screens/startup_screen.dart';
import 'package:valora_app/providers/auth_provider.dart';

// Fake AuthProvider to satisfy StartupScreen dependency
class FakeAuthProvider extends ChangeNotifier implements AuthProvider {
  @override
  Future<void> loginWithGoogle() async {}
  @override
  Future<void> checkAuth() async {
    // No-op for testing
  }

  @override
  bool get isAuthenticated => false;
  @override
  bool get isLoading => false;
  @override
  String? get token => null;
  @override
  String? get email => null;

  @override
  Future<void> login(String email, String password) async {}

  @override
  Future<void> register(
    String email,
    String password,
    String confirmPassword,
  ) async {}

  @override
  Future<void> logout() async {}

  @override
  Future<String?> refreshSession() async {
    return null;
  }
}

void main() {
  group('GlobalErrorWidget', () {
    testWidgets('displays user friendly error message', (tester) async {
      final details = FlutterErrorDetails(exception: Exception('Boom'));

      await tester.pumpWidget(
        MaterialApp(home: GlobalErrorWidget(details: details)),
      );

      expect(find.text("We're sorry, something went wrong"), findsOneWidget);
      expect(
        find.text(
          'Please restart the application. If the problem persists, contact support.',
        ),
        findsOneWidget,
      );
      expect(find.text('Restart'), findsOneWidget);
    });

    testWidgets('Restart button navigates to StartupScreen', (tester) async {
      await tester.pumpWidget(
        ChangeNotifierProvider<AuthProvider>(
          create: (_) => FakeAuthProvider(),
          child: MaterialApp(
            routes: {
              '/': (context) => const GlobalErrorWidgetWrapper(),
              '/home': (context) => const SizedBox(), // Stub route for StartupScreen navigation target
            },
          ),
        ),
      );

      // Trigger the restart
      await tester.tap(find.text('Restart'));

      // Pump to process navigation but don't settle animations (StartupScreen navigates away after animation)
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));

      // Verify we are on StartupScreen
      expect(find.byType(StartupScreen), findsOneWidget);

      // Let StartupScreen timer complete to avoid pending timers.
      await tester.pump(StartupScreen.splashDuration);
    });

    testWidgets('shows debug info in debug mode', (tester) async {
      final details = FlutterErrorDetails(
        exception: Exception('Secret Stack Trace'),
      );

      await tester.pumpWidget(
        MaterialApp(home: GlobalErrorWidget(details: details)),
      );

      // In test environment, kDebugMode is true
      expect(find.text('Exception: Secret Stack Trace'), findsOneWidget);
    });
  });
}

class GlobalErrorWidgetWrapper extends StatelessWidget {
  const GlobalErrorWidgetWrapper({super.key});

  @override
  Widget build(BuildContext context) {
    return GlobalErrorWidget(
      details: FlutterErrorDetails(exception: Exception('Test')),
    );
  }
}
