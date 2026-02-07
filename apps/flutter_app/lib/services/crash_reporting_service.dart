import 'package:flutter/foundation.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:sentry_flutter/sentry_flutter.dart';

class CrashReportingService {
  static bool _initialized = false;

  static bool get isInitialized => _initialized;

  static Future<void> initialize() async {
    final String? dsn = dotenv.env['SENTRY_DSN']?.trim();
    if (dsn == null || dsn.isEmpty) {
      _initialized = false;
      return;
    }

    await SentryFlutter.init((options) {
      options.dsn = dsn;
      options.tracesSampleRate = kDebugMode ? 0 : 0.1;
    });

    _initialized = true;
  }

  static Future<void> captureException(
    Object error, {
    StackTrace? stackTrace,
    Map<String, dynamic>? context,
  }) async {
    if (!_initialized) {
      return;
    }

    await Sentry.captureException(
      error,
      stackTrace: stackTrace,
      withScope: (scope) {
        if (context != null) {
          for (final entry in context.entries) {
            scope.setTag(entry.key, entry.value.toString());
          }
        }
      },
    );
  }
}
