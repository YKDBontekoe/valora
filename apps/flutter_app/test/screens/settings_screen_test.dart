import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/providers/theme_provider.dart';
import 'package:valora_app/screens/settings_screen.dart';

@GenerateMocks([AuthProvider, ThemeProvider])
import 'settings_screen_test.mocks.dart';

void main() {
  late MockAuthProvider mockAuthProvider;
  late MockThemeProvider mockThemeProvider;

  setUp(() {
    mockAuthProvider = MockAuthProvider();
    mockThemeProvider = MockThemeProvider();

    when(mockAuthProvider.email).thenReturn('test@example.com');
    when(mockThemeProvider.isDarkMode).thenReturn(false);

    // Override HTTP overrides to avoid 400 bad request on image loading
    HttpOverrides.global = null;
  });

  Widget createWidgetUnderTest() {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider<AuthProvider>.value(value: mockAuthProvider),
        ChangeNotifierProvider<ThemeProvider>.value(value: mockThemeProvider),
      ],
      child: const MaterialApp(
        home: SettingsScreen(),
      ),
    );
  }

  testWidgets('SettingsScreen displays user email', (WidgetTester tester) async {
    await tester.pumpWidget(createWidgetUnderTest());

    expect(find.text('test@example.com'), findsOneWidget);
  });

  testWidgets('SettingsScreen shows logout confirmation dialog', (WidgetTester tester) async {
    await tester.pumpWidget(createWidgetUnderTest());

    // Scroll to logout button if necessary (it's at the bottom)
    final logoutButtonFinder = find.widgetWithText(TextButton, 'Log Out');
    await tester.scrollUntilVisible(logoutButtonFinder, 500);

    await tester.tap(logoutButtonFinder);
    await tester.pumpAndSettle();

    expect(find.text('Log Out?'), findsOneWidget);
    expect(find.text('Are you sure you want to log out?'), findsOneWidget);
  });

  testWidgets('SettingsScreen calls logout on confirmation', (WidgetTester tester) async {
    await tester.pumpWidget(createWidgetUnderTest());

    final logoutButtonFinder = find.widgetWithText(TextButton, 'Log Out');
    await tester.scrollUntilVisible(logoutButtonFinder, 500);

    await tester.tap(logoutButtonFinder);
    await tester.pumpAndSettle();

    // Tap confirm in dialog
    // The dialog title is 'Log Out?' (with question mark)
    // The button is 'Log Out' (no question mark)
    final confirmButton = find.widgetWithText(ElevatedButton, 'Log Out');

    await tester.tap(confirmButton);
    await tester.pumpAndSettle();

    verify(mockAuthProvider.logout()).called(1);
  });

  testWidgets('SettingsScreen cancels logout', (WidgetTester tester) async {
    await tester.pumpWidget(createWidgetUnderTest());

    final logoutButtonFinder = find.widgetWithText(TextButton, 'Log Out');
    await tester.scrollUntilVisible(logoutButtonFinder, 500);

    await tester.tap(logoutButtonFinder);
    await tester.pumpAndSettle();

    // Verify dialog is open
    expect(find.text('Log Out?'), findsOneWidget);

    // Tap Cancel
    await tester.tap(find.text('Cancel'));
    await tester.pumpAndSettle();

    verifyNever(mockAuthProvider.logout());
    expect(find.text('Log Out?'), findsNothing);
  });
}
