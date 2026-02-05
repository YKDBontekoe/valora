import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'core/theme/valora_theme.dart';
import 'providers/auth_provider.dart';
import 'providers/favorites_provider.dart';
import 'providers/theme_provider.dart';
import 'screens/startup_screen.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'services/api_service.dart';
import 'services/auth_service.dart';
import 'widgets/global_error_widget.dart';

// coverage:ignore-start
Future<void> main() async {
  // Ensure binding is initialized before using PlatformDispatcher
  WidgetsFlutterBinding.ensureInitialized();

  // Load environment variables. In production/CI, we often rely on build arguments or
  // the .env.example file being bundled if a specific .env isn't provided.
  // Since .env is gitignored and not guaranteed to exist in CI, we load .env.example
  // which is safe to commit.
  await dotenv.load(fileName: ".env.example");

  // Catch Flutter framework errors
  FlutterError.onError = (FlutterErrorDetails details) {
    FlutterError.presentError(details);
    debugPrint('Flutter Error: ${details.exception}');
    // TODO: Send to crash reporting service
  };

  // Catch asynchronous errors
  PlatformDispatcher.instance.onError = (error, stack) {
    debugPrint('Async Error: $error');
    // TODO: Send to crash reporting service
    return true; // Prevent app from crashing
  };

  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider<ThemeProvider>(
          create: (_) => ThemeProvider(),
        ),
        ChangeNotifierProvider<FavoritesProvider>(
          create: (_) => FavoritesProvider(),
        ),
        Provider<AuthService>(
          create: (_) => AuthService(),
        ),
        ChangeNotifierProxyProvider<AuthService, AuthProvider>(
          create: (context) => AuthProvider(authService: context.read<AuthService>()),
          update: (context, authService, previous) =>
              previous ?? AuthProvider(authService: authService),
        ),
        ProxyProvider2<AuthService, AuthProvider, ApiService>(
          update: (context, authService, authProvider, _) => ApiService(
            authToken: authProvider.token,
            refreshTokenCallback: authProvider.refreshSession,
          ),
        ),
      ],
      child: const ValoraApp(),
    ),
  );
}
// coverage:ignore-end

class ValoraApp extends StatelessWidget {
  const ValoraApp({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<ThemeProvider>(
      builder: (context, themeProvider, _) {
        return MaterialApp(
          title: 'Valora',
          theme: ValoraTheme.light,
          darkTheme: ValoraTheme.dark,
          themeMode: themeProvider.themeMode,
          home: const StartupScreen(),
          debugShowCheckedModeBanner: false,
          builder: (context, child) {
            // Global error widget for build errors
            ErrorWidget.builder = (FlutterErrorDetails details) {
              return GlobalErrorWidget(details: details);
            };
            return child!;
          },
        );
      },
    );
  }
}
