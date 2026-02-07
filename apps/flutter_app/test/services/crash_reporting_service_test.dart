import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/services/crash_reporting_service.dart';

void main() {
  setUpAll(() async {
    await dotenv.load(fileName: '.env.example');
  });

  test('initialize is no-op when SENTRY_DSN is missing', () async {
    final Map<String, String> backup = Map<String, String>.from(dotenv.env);
    dotenv.env.clear();

    try {
      await CrashReportingService.initialize();
      expect(CrashReportingService.isInitialized, isFalse);

      await CrashReportingService.captureException(Exception('no-op test'));
    } finally {
      dotenv.env
        ..clear()
        ..addAll(backup);
    }
  });
}
