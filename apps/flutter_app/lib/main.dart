mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'dart:async';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'dart:ui';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'package:flutter/foundation.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'package:flutter/material.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'package:provider/provider.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'core/config/app_config.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'core/theme/valora_theme.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'providers/auth_provider.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'providers/favorites_provider.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'providers/theme_provider.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'screens/startup_screen.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'services/api_service.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'services/auth_service.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'services/crash_reporting_service.dart';
import 'services/notification_service.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
import 'widgets/global_error_widget.dart';
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
// coverage:ignore-start
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
Future<void> main() async {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  // Ensure binding is initialized before using PlatformDispatcher
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  WidgetsFlutterBinding.ensureInitialized();
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  try {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    // Prefer local overrides when available, but always fall back to committed defaults.
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    try {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      await dotenv.load(fileName: ".env");
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    } catch (_) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      await dotenv.load(fileName: ".env.example");
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    }
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    // Catch Flutter framework errors
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    FlutterError.onError = (FlutterErrorDetails details) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      FlutterError.presentError(details);
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      if (kDebugMode) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        debugPrint('Flutter Error: ${details.exception}');
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      }
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      unawaited(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        _reportCrash(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          details.exception,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          stackTrace: details.stack,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          context: const <String, dynamic>{'source': 'flutter_error'},
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      );
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    };
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    // Catch asynchronous errors
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    PlatformDispatcher.instance.onError = (error, stack) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      if (kDebugMode) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        debugPrint('Async Error: $error');
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      }
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      unawaited(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        _reportCrash(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          error,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          stackTrace: stack,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          context: const <String, dynamic>{'source': 'async_error'},
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      );
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      return true; // Prevent app from crashing
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    };
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    await CrashReportingService.initialize();
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    runApp(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      MultiProvider(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        providers: [
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          ChangeNotifierProvider<ThemeProvider>(create: (_) => ThemeProvider()),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          ChangeNotifierProvider<FavoritesProvider>(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            create: (_) => FavoritesProvider(),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          Provider<AuthService>(create: (_) => AuthService()),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          ChangeNotifierProxyProvider<AuthService, AuthProvider>(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            create: (context) =>
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                AuthProvider(authService: context.read<AuthService>()),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            update: (context, authService, previous) =>
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                previous ?? AuthProvider(authService: authService),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          ProxyProvider2<AuthService, AuthProvider, ApiService>(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            update: (context, authService, authProvider, _) => ApiService(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
              authToken: authProvider.token,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
              refreshTokenCallback: authProvider.refreshSession,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            ),


          ChangeNotifierProxyProvider<ApiService, NotificationService>(
            create: (context) => NotificationService(context.read<ApiService>()),
            update: (context, apiService, previous) =>
                (previous ?? NotificationService(apiService))..update(apiService),
          ),

            update: (context, apiService, previous) =>
                previous ?? NotificationService(apiService),
          ),

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        ],
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        child: const ValoraApp(),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    );
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  } catch (e, stack) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    if (kDebugMode) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      debugPrint('Initialization Error: $e');
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    }
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    runApp(InitializationErrorApp(error: e, stackTrace: stack));
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  }
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
}
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
// coverage:ignore-end
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
Future<void> _reportCrash(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  Object error, {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  StackTrace? stackTrace,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  Map<String, dynamic>? context,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
}) async {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  try {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    await CrashReportingService.captureException(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      error,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      stackTrace: stackTrace,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      context: context,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    );
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  } catch (_) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    // Never let crash reporting throw into app error handlers.
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  }
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
}
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
class InitializationErrorApp extends StatelessWidget {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  final Object error;
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  final StackTrace? stackTrace;
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  const InitializationErrorApp({
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    super.key,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    required this.error,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    this.stackTrace,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  });
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  @override
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  Widget build(BuildContext context) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    return MaterialApp(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      debugShowCheckedModeBanner: false,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      home: Scaffold(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        backgroundColor: Colors.white,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        body: Center(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          child: Padding(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            padding: const EdgeInsets.all(24.0),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            child: Column(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
              mainAxisAlignment: MainAxisAlignment.center,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
              children: [
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                const Icon(Icons.error_outline, size: 64, color: Colors.red),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                const SizedBox(height: 24),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                const Text(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                  'Application Initialization Failed',
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                  style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                  textAlign: TextAlign.center,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                const SizedBox(height: 16),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                if (kDebugMode)
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                  Text(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                    error.toString(),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                    style: const TextStyle(color: Colors.black54),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                    textAlign: TextAlign.center,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                  )
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                else
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                  const Text(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                    'Please restart the application or contact support.',
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                    style: TextStyle(color: Colors.black54),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                    textAlign: TextAlign.center,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                  ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
              ],
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    );
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  }
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
}
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
class ValoraApp extends StatelessWidget {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  const ValoraApp({super.key});
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  @override
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  Widget build(BuildContext context) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    return Consumer<ThemeProvider>(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      builder: (context, themeProvider, _) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        return MaterialApp(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          title: 'Valora',
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          theme: ValoraTheme.light,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          darkTheme: ValoraTheme.dark,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          themeMode: themeProvider.themeMode,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          home: const StartupScreen(),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          debugShowCheckedModeBanner: false,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          builder: (context, child) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            // Global error widget for build errors
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            ErrorWidget.builder = (FlutterErrorDetails details) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
              return GlobalErrorWidget(details: details);
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            };
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            if (!AppConfig.isUsingFallbackApiUrl) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
              return child!;
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            }
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            return Stack(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
              children: [
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                child!,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                const Positioned(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                  left: 0,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                  right: 0,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                  top: 0,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                  child: _ConfigWarningBanner(),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
                ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
              ],
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            );
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          },
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        );
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      },
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    );
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  }
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
}
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
class _ConfigWarningBanner extends StatelessWidget {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  const _ConfigWarningBanner();
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';

mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  @override
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  Widget build(BuildContext context) {
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    final ColorScheme colorScheme = Theme.of(context).colorScheme;
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    return SafeArea(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      bottom: false,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      child: Material(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        color: colorScheme.errorContainer,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        child: Padding(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          child: Text(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            'API_URL is not configured. Using fallback: ${AppConfig.apiUrl}',
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            textAlign: TextAlign.center,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            style: TextStyle(
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
              color: colorScheme.onErrorContainer,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
              fontWeight: FontWeight.w600,
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
            ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
          ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
        ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
      ),
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
    );
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
  }
mport 'services/crash_reporting_service.dart';/a import 'services/notification_service.dart';
}
