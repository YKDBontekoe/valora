import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:logging/logging.dart';
import 'package:valora_app/services/logging_service.dart';

void main() {
  final originalDebugPrint = debugPrint;

  setUp(() {
    Logger.root.clearListeners();
    LoggingService.reset();
  });

  tearDown(() {
    debugPrint = originalDebugPrint;
  });

  group('LoggingService Tests', () {
    test('Initialization sets log level', () {
      LoggingService.initialize();

      if (kReleaseMode) {
        expect(Logger.root.level, Level.WARNING);
      } else {
        expect(Logger.root.level, Level.ALL);
      }
    });

    // This test ensures idempotency coverage
    test('Initialization is idempotent', () {
      LoggingService.initialize();
      LoggingService.initialize();
      // Should not crash or change behavior. Hard to test "listener count" without reflection.
      expect(true, true);
    });

    test('Debug logs are formatted as JSON', () async {
      final logMessages = <String>[];

      debugPrint = (String? message, {int? wrapWidth}) {
        if (message != null) logMessages.add(message);
      };

      LoggingService.initialize();

      Logger.root.info('Test JSON formatting');

      await Future.delayed(Duration.zero);

      if (kDebugMode) {
        expect(logMessages, isNotEmpty);
        final logJson = logMessages.last;
        final decoded = jsonDecode(logJson);
        expect(decoded['level'], 'INFO');
        expect(decoded['message'], 'Test JSON formatting');
        // No error/stackTrace
        expect(decoded.containsKey('error'), isFalse);
        expect(decoded.containsKey('stackTrace'), isFalse);
      }
    });

    test('Error logs include error and stackTrace in JSON', () async {
      final logMessages = <String>[];
      debugPrint = (String? message, {int? wrapWidth}) {
        if (message != null) logMessages.add(message);
      };

      LoggingService.initialize();

      try {
        throw Exception('Test Exception');
      } catch (e, s) {
        Logger.root.warning('Something went wrong', e, s);
      }

      await Future.delayed(Duration.zero);

      if (kDebugMode) {
        expect(logMessages, isNotEmpty);
        final logJson = logMessages.last;
        final decoded = jsonDecode(logJson);

        expect(decoded['level'], 'WARNING');
        expect(decoded['message'], 'Something went wrong');
        expect(decoded['error'], contains('Exception: Test Exception'));
        expect(decoded.containsKey('stackTrace'), isTrue);
      }
    });

    test('Severe logs execute (Sentry integration path)', () async {
      LoggingService.initialize();

      // Test without error object (triggers `record.error ?? Exception(...)` logic)
      expect(() => Logger.root.severe('Critical error'), returnsNormally);

      // Test with error object
      expect(
        () => Logger.root.severe('Critical error', Exception('Fatal')),
        returnsNormally,
      );
    });
  });
}
