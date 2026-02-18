import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';

class AppConfig {
  static const String _fallbackApiUrl = 'http://localhost:5001/api';

  static String get apiUrl {
    String url;
    try {
      url = dotenv.env['API_URL']?.trim() ?? '';
    } catch (_) {
      url = '';
    }
    if (url.isEmpty) {
      url = _fallbackApiUrl;
    }

    // Handle Android emulator localhost issue
    if (!kIsWeb && Platform.isAndroid && url.contains('localhost')) {
      return url.replaceFirst('localhost', '10.0.2.2');
    }
    
    return url;
  }

  static bool get isApiUrlConfigured {
    try {
      final configured = dotenv.env['API_URL']?.trim();
      return configured != null && configured.isNotEmpty;
    } catch (_) {
      return false;
    }
  }

  static bool get isUsingFallbackApiUrl => !isApiUrlConfigured;
}
