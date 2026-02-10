import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/providers/theme_provider.dart';
import 'package:valora_app/screens/home_screen.dart';
import 'package:valora_app/screens/search_screen.dart';
import 'package:valora_app/screens/settings_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/auth_service.dart';
import 'package:valora_app/services/notification_service.dart';

void main() {
  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  Widget createWidget() {
    return MultiProvider(
      providers: [
        Provider<AuthService>(create: (_) => AuthService()),
        ChangeNotifierProvider<ThemeProvider>(create: (_) => ThemeProvider()),
        ChangeNotifierProvider<FavoritesProvider>(create: (_) => FavoritesProvider()),
        ChangeNotifierProvider<AuthProvider>(create: (_) => AuthProvider(authService: AuthService())),
        Provider<ApiService>(create: (_) => ApiService()),
        ChangeNotifierProvider<NotificationService>(
          create: (_) => NotificationService(ApiService()),
        ),
      ],
      child: const MaterialApp(home: HomeScreen()),
    );
  }

  testWidgets('shows search screen by default', (WidgetTester tester) async {
    await tester.pumpWidget(createWidget());
    await tester.pumpAndSettle();

    expect(find.byType(SearchScreen), findsOneWidget);
  });

  testWidgets('navigates to settings tab', (WidgetTester tester) async {
    await tester.pumpWidget(createWidget());
    await tester.pumpAndSettle();

    await tester.tap(find.byTooltip('Settings'));
    await tester.pumpAndSettle();

    expect(find.byType(SettingsScreen), findsOneWidget);
  });
}
