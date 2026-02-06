import 'dart:ui';
import 'package:flutter/foundation.dart';
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

  try {
    // Load environment variables. In production/CI, we often rely on build arguments or
    // the .env.example file being bundled if a specific .env isn't provided.
    // Since .env is gitignored and not guaranteed to exist in CI, we load .env.example
    // which is safe to commit.
    await dotenv.load(fileName: ".env.example");

    // Catch Flutter framework errors
    FlutterError.onError = (FlutterErrorDetails details) {
      FlutterError.presentError(details);
      if (kDebugMode) {
        debugPrint('Flutter Error: ${details.exception}');
      }
      // TODO: Send to crash reporting service
    };

    // Catch asynchronous errors
    PlatformDispatcher.instance.onError = (error, stack) {
      if (kDebugMode) {
        debugPrint('Async Error: $error');
      }
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
  } catch (e, stack) {
    if (kDebugMode) {
      debugPrint('Initialization Error: $e');
    }
    runApp(InitializationErrorApp(error: e, stackTrace: stack));
  }
}
// coverage:ignore-end

class InitializationErrorApp extends StatelessWidget {
  final Object error;
  final StackTrace? stackTrace;

  const InitializationErrorApp({super.key, required this.error, this.stackTrace});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      home: Scaffold(
        backgroundColor: Colors.white,
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(24.0),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.error_outline, size: 64, color: Colors.red),
                const SizedBox(height: 24),
                const Text(
                  'Application Initialization Failed',
                  style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 16),
                if (kDebugMode)
                  Text(
                    error.toString(),
                    style: const TextStyle(color: Colors.black54),
                    textAlign: TextAlign.center,
                  )
                else
                  const Text(
                    'Please restart the application or contact support.',
                    style: TextStyle(color: Colors.black54),
                    textAlign: TextAlign.center,
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

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
