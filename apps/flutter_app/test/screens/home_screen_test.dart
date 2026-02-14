import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/providers/favorites_provider.dart';
import 'package:valora_app/providers/insights_provider.dart';
import 'package:valora_app/providers/theme_provider.dart';
import 'package:valora_app/providers/user_profile_provider.dart';
import 'package:valora_app/screens/home_screen.dart';
import 'package:valora_app/screens/insights/insights_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/auth_service.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/widgets/home/home_bottom_nav_bar.dart';
import '../mocks/mock_notification_service.dart';
import '../mocks/mock_user_profile_provider.dart';
import '../mocks/mock_api_service.dart';

void main() {
  Widget createHomeScreen() {
    final apiService = MockApiService();
    return MultiProvider(
      providers: [
        Provider<ApiService>.value(value: apiService),
        ChangeNotifierProvider<ThemeProvider>(create: (_) => ThemeProvider()),
        ChangeNotifierProvider<FavoritesProvider>(create: (_) => FavoritesProvider()),
        ChangeNotifierProvider<AuthProvider>(create: (_) => AuthProvider(authService: AuthService())),
        ChangeNotifierProvider<NotificationService>.value(value: MockNotificationService()),
        ChangeNotifierProvider<UserProfileProvider>.value(value: MockUserProfileProvider()),
        ChangeNotifierProvider<InsightsProvider>(
          create: (_) => InsightsProvider(apiService),
        ),
      ],
      child: const MaterialApp(
        home: HomeScreen(),
      ),
    );
  }

  testWidgets('HomeScreen renders bottom navigation bar', (WidgetTester tester) async {
    await tester.pumpWidget(createHomeScreen());
    await tester.pump();

    expect(find.byType(HomeBottomNavBar), findsOneWidget);
    expect(find.text('Search'), findsAtLeastNWidgets(1));
    expect(find.text('Insights'), findsAtLeastNWidgets(1));
    expect(find.text('Saved'), findsAtLeastNWidgets(1));
    expect(find.text('Settings'), findsAtLeastNWidgets(1));
  }, skip: true);

  testWidgets('HomeScreen switches tabs when tapped', (WidgetTester tester) async {
    await tester.pumpWidget(createHomeScreen());
    await tester.pump();

    // Tap Insights tab
    await tester.tap(find.byIcon(Icons.map_rounded), warnIfMissed: false);
    await tester.pump();

    // Verify Insights screen is shown
    expect(find.byType(InsightsScreen), findsOneWidget);
  }, skip: true);
}
