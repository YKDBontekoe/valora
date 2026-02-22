import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:logging/logging.dart';
import 'crash_reporting_service.dart';

class LoggingService {
  static bool _initialized = false;
  @visibleForTesting
  static void reset() => _initialized = false;

  static void initialize() {
    if (_initialized) return;
    _initialized = true;

    // Set the log level based on the build mode.
    // In release mode, only log warnings and severe errors.
    // In debug/profile mode, log everything.
    Logger.root.level = kReleaseMode ? Level.WARNING : Level.ALL;

    Logger.root.onRecord.listen((record) {
      // 1. Print to console (formatted as JSON)
      if (kDebugMode) {
        final logEntry = {
          'level': record.level.name,
          'time': record.time.toIso8601String(),
          'logger': record.loggerName,
          'message': record.message,
          if (record.error != null) 'error': record.error.toString(),
          if (record.stackTrace != null)
            'stackTrace': record.stackTrace.toString(),
        };
        debugPrint(jsonEncode(logEntry));
      }

      // 2. Send to Sentry if severe
      if (record.level >= Level.SEVERE) {
        final dynamic error = record.error ?? Exception(record.message);

        CrashReportingService.captureException(
          error,
          stackTrace: record.stackTrace,
          context: {
            'logger_name': record.loggerName,
            'message': record.message,
            'level': record.level.name,
          },
        );
      }
    });
  }
}
