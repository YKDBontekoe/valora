import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/core/config/app_config.dart';

void main() {
  setUpAll(() async {
    await dotenv.load(fileName: '.env.example');
  });

  group('AppConfig', () {
    test('uses fallback when API_URL is missing', () {
      final Map<String, String> backup = Map<String, String>.from(dotenv.env);
      dotenv.env.clear();

      try {
        expect(AppConfig.apiUrl, 'http://localhost:5000/api');
        expect(AppConfig.isUsingFallbackApiUrl, isTrue);
      } finally {
        dotenv.env
          ..clear()
          ..addAll(backup);
      }
    });

    test('uses configured API_URL when provided', () {
      final Map<String, String> backup = Map<String, String>.from(dotenv.env);
      dotenv.env
        ..clear()
        ..addAll(<String, String>{'API_URL': 'https://example.test/api'});

      try {
        expect(AppConfig.apiUrl, 'https://example.test/api');
        expect(AppConfig.isUsingFallbackApiUrl, isFalse);
      } finally {
        dotenv.env
          ..clear()
          ..addAll(backup);
      }
    });
  });
}
