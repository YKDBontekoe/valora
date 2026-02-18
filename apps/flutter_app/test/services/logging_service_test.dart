import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:logging/logging.dart';
import 'package:valora_app/services/logging_service.dart';

void main() {
  final originalDebugPrint = debugPrint;

  setUp(() {
    Logger.root.clearListeners();
    // Allow re-initialization
    LoggingService.reset();
  });

  tearDown(() {
    debugPrint = originalDebugPrint;
  });

  group('LoggingService Tests', () {
    test('Initialization sets log level', () {
      LoggingService.initialize();

      // In tests, kReleaseMode is typically false
      if (kReleaseMode) {
        expect(Logger.root.level, Level.WARNING);
      } else {
        expect(Logger.root.level, Level.ALL);
      }
    });

    test('Logs are formatted as JSON', () async {
      final logMessages = <String>[];

      debugPrint = (String? message, {int? wrapWidth}) {
        if (message != null) logMessages.add(message);
      };

      LoggingService.initialize();

      Logger.root.info('Test JSON formatting');

      // Allow microtasks
      await Future.delayed(Duration.zero);

      if (kDebugMode) {
        expect(logMessages, isNotEmpty, reason: 'Log messages should be captured in debug mode');

        final logJson = logMessages.last;
        try {
          final Map<String, dynamic> decoded = jsonDecode(logJson);
          expect(decoded['level'], 'INFO');
          expect(decoded['message'], 'Test JSON formatting');
        } catch (e) {
          fail('Failed to parse JSON log: $logJson. Error: $e');
        }
      }
    });

    test('Severe logs execute without error (Sentry integration)', () async {
      LoggingService.initialize();
      expect(() => Logger.root.severe('Critical error'), returnsNormally);
    });
  });
}
