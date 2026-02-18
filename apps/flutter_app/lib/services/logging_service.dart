import 'package:flutter/foundation.dart';
import 'package:logging/logging.dart';
import 'crash_reporting_service.dart';

class LoggingService {
  static void initialize() {
    // Set the log level based on the build mode.
    // In release mode, only log warnings and severe errors.
    // In debug/profile mode, log everything.
    Logger.root.level = kReleaseMode ? Level.WARNING : Level.ALL;

    Logger.root.onRecord.listen((record) {
      // 1. Print to console (formatted)
      if (kDebugMode) {
        // Use debugPrint for potentially long output to avoid truncation on Android
        debugPrint(
            '${record.level.name}: ${record.time}: [${record.loggerName}] ${record.message}');
        if (record.error != null) {
          debugPrint('Error: ${record.error}');
        }
        if (record.stackTrace != null) {
          debugPrint('Stack: ${record.stackTrace}');
        }
      }

      // 2. Send to Sentry if severe
      if (record.level >= Level.SEVERE) {
        CrashReportingService.captureException(
          record.error ?? record.message,
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
