import 'dart:async';
import 'dart:ui';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';

import 'core/config/app_config.dart';
import 'core/theme/valora_theme.dart';
import 'providers/auth_provider.dart';
import 'providers/theme_provider.dart';
import 'providers/insights_provider.dart';
import 'screens/startup_screen.dart';
import 'services/api_client.dart';
import 'services/auth_service.dart';
import 'services/crash_reporting_service.dart';
import 'services/logging_service.dart';
import 'services/notification_service.dart';
import 'repositories/map_repository.dart';
import 'repositories/notification_repository.dart';
import 'repositories/context_report_repository.dart';
import 'repositories/workspace_repository.dart';
import 'widgets/global_error_widget.dart';

// coverage:ignore-start
Future<void> main() async {
  // Ensure binding is initialized before using PlatformDispatcher
  WidgetsFlutterBinding.ensureInitialized();

  try {
    // Prefer local overrides when available, but always fall back to committed defaults.
    // In release/profile mode, dotenv can only load files declared in pubspec.yaml assets.
    bool envLoaded = false;
    try {
      await dotenv.load(fileName: ".env");
      envLoaded = true;
    } catch (_) {
      // .env not found in asset bundle, try .env.example
    }
    if (!envLoaded) {
      try {
        await dotenv.load(fileName: ".env.example");
      } catch (_) {
        // Neither file available — app will use AppConfig fallback values
        if (kDebugMode) {
          debugPrint('Warning: No .env file found. Using fallback configuration.');
        }
      }
    }

    // Catch Flutter framework errors
    FlutterError.onError = (FlutterErrorDetails details) {
      FlutterError.presentError(details);
      if (kDebugMode) {
        debugPrint('Flutter Error: ${details.exception}');
      }
      unawaited(
        _reportCrash(
          details.exception,
          stackTrace: details.stack,
          context: const <String, dynamic>{'source': 'flutter_error'},
        ),
      );
    };

    // Catch asynchronous errors
    PlatformDispatcher.instance.onError = (error, stack) {
      if (kDebugMode) {
        debugPrint('Async Error: $error');
      }
      unawaited(
        _reportCrash(
          error,
          stackTrace: stack,
          context: const <String, dynamic>{'source': 'async_error'},
        ),
      );
      return true; // Prevent app from crashing
    };

    await CrashReportingService.initialize();
    LoggingService.initialize();

    runApp(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<ThemeProvider>(create: (_) => ThemeProvider()),
          Provider<AuthService>(create: (_) => AuthService()),
          ChangeNotifierProxyProvider<AuthService, AuthProvider>(
            create: (context) =>
                AuthProvider(authService: context.read<AuthService>()),
            update: (context, authService, previous) =>
                previous ?? AuthProvider(authService: authService),
          ),
          ProxyProvider2<AuthService, AuthProvider, ApiClient>(
            update: (context, authService, authProvider, previous) {
              if (previous != null) {
                previous.updateAuthToken(authProvider.token);
                return previous;
              }
              return ApiClient(
                authToken: authProvider.token,
                refreshTokenCallback: authProvider.refreshSession,
              );
            },
            dispose: (_, client) => client.dispose(),
          ),
          ProxyProvider<ApiClient, MapRepository>(
            update: (context, client, _) => MapRepository(client),
          ),
          ProxyProvider<ApiClient, NotificationRepository>(
            update: (context, client, _) => NotificationRepository(client),
          ),
          ProxyProvider<ApiClient, ContextReportRepository>(
            update: (context, client, _) => ContextReportRepository(client),
          ),
          ProxyProvider<ApiClient, WorkspaceRepository>(
            update: (context, client, _) => WorkspaceRepository(client),
          ),
          ChangeNotifierProxyProvider<NotificationRepository, NotificationService>(
            create: (context) => NotificationService(context.read<NotificationRepository>()),
            update: (context, repo, previous) =>
                (previous ?? NotificationService(repo))..update(repo),
          ),
          ChangeNotifierProxyProvider<MapRepository, InsightsProvider>(
            create: (context) => InsightsProvider(context.read<MapRepository>()),
            update: (context, repo, previous) =>
                (previous ?? InsightsProvider(repo))..update(repo),
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

Future<void> _reportCrash(
  Object error, {
  StackTrace? stackTrace,
  Map<String, dynamic>? context,
}) async {
  try {
    await CrashReportingService.captureException(
      error,
      stackTrace: stackTrace,
      context: context,
    );
  } catch (_) {
    // Never let crash reporting throw into app error handlers.
  }
}

class InitializationErrorApp extends StatelessWidget {
  final Object error;
  final StackTrace? stackTrace;

  const InitializationErrorApp({
    super.key,
    required this.error,
    this.stackTrace,
  });

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
            if (!AppConfig.isUsingFallbackApiUrl) {
              return child!;
            }

            return Stack(
              children: [
                child!,
                const Positioned(
                  left: 0,
                  right: 0,
                  top: 0,
                  child: _ConfigWarningBanner(),
                ),
              ],
            );
          },
        );
      },
    );
  }
}

class _ConfigWarningBanner extends StatelessWidget {
  const _ConfigWarningBanner();

  @override
  Widget build(BuildContext context) {
    final ColorScheme colorScheme = Theme.of(context).colorScheme;
    return SafeArea(
      bottom: false,
      child: Material(
        color: colorScheme.errorContainer,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: Text(
            'API_URL is not configured. Using fallback: ${AppConfig.apiUrl}',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: colorScheme.onErrorContainer,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
      ),
    );
  }
}
