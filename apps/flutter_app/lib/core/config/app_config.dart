import 'package:flutter_dotenv/flutter_dotenv.dart';

class AppConfig {
  static const String _fallbackApiUrl = 'http://localhost:5001/api';

  static String get apiUrl {
    final configured = dotenv.env['API_URL']?.trim();
    if (configured == null || configured.isEmpty) {
      return _fallbackApiUrl;
    }
    return configured;
  }

  static bool get isApiUrlConfigured {
    final configured = dotenv.env['API_URL']?.trim();
    return configured != null && configured.isNotEmpty;
  }

  static bool get isUsingFallbackApiUrl => !isApiUrlConfigured;
}
