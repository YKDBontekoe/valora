import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/core/config/app_config.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUpAll(() async {
    await dotenv.load(fileName: ".env.example");
  });

  group('ApiService Configuration', () {
    test('baseUrl matches AppConfig.apiUrl', () {
      expect(ApiService.baseUrl, equals(AppConfig.apiUrl));
    });

    test('baseUrl reflects changes in dotenv (simulated)', () {
      final oldUrl = dotenv.env['API_URL'];
      
      try {
        dotenv.env['API_URL'] = 'https://dynamic-test-url.com/api';
        expect(ApiService.baseUrl, 'https://dynamic-test-url.com/api');
        expect(AppConfig.apiUrl, 'https://dynamic-test-url.com/api');
      } finally {
        if (oldUrl != null) {
          dotenv.env['API_URL'] = oldUrl;
        } else {
          dotenv.env.remove('API_URL');
        }
      }
    });
  });
}
