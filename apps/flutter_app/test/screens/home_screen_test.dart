import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/providers/insights_provider.dart';
import 'package:valora_app/providers/theme_provider.dart';
import 'package:valora_app/screens/home_screen.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/services/auth_service.dart';
import 'package:valora_app/services/notification_service.dart';
import 'package:valora_app/widgets/insights/insights_map.dart';

class _TestNotificationService extends NotificationService {
  _TestNotificationService(super.apiService);

  @override
  void startPolling() {}
}

class _TestInsightsProvider extends InsightsProvider {
  _TestInsightsProvider(super.apiService);

  @override
  Future<void> loadInsights() async {
    // Keep tests deterministic and offline.
  }
}

void main() {
  testWidgets('HomeScreen navigation matches tab content', (tester) async {
    final apiService = ApiService();

    await tester.pumpWidget(
      MultiProvider(
        providers: [
          Provider<ApiService>.value(value: apiService),
          ChangeNotifierProvider<NotificationService>(
            create: (_) => _TestNotificationService(apiService),
          ),
          ChangeNotifierProvider<InsightsProvider>(
            create: (_) => _TestInsightsProvider(apiService),
          ),
          ChangeNotifierProvider<ThemeProvider>(create: (_) => ThemeProvider()),
          ChangeNotifierProvider<AuthProvider>(
            create: (_) => AuthProvider(authService: AuthService()),
          ),
        ],
        child: const MaterialApp(home: HomeScreen()),
      ),
    );

    await tester.pumpAndSettle();
    expect(find.text('Search Property'), findsOneWidget);

    await tester.tap(find.byTooltip('Insights'));
    await tester.pumpAndSettle();
    expect(find.byType(InsightsMap), findsOneWidget);

    await tester.tap(find.byTooltip('Settings'));
    await tester.pumpAndSettle();
    expect(find.text('Log Out'), findsOneWidget);
  });
}
