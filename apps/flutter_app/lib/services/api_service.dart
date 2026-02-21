import 'dart:async';
import 'dart:convert';
import 'dart:developer' as developer;
import 'dart:io';

import 'package:http/http.dart' as http;
import 'package:retry/retry.dart';
import '../core/config/app_config.dart';
import '../core/exceptions/app_exceptions.dart';
import '../models/context_report.dart';
import '../models/map_city_insight.dart';
import '../models/map_amenity.dart';
import '../models/map_overlay.dart';
import '../models/notification.dart';
import '../models/saved_property.dart';
import 'crash_reporting_service.dart';

class ApiService {
  final String baseUrl;
  final String? authToken;
  final Future<void> Function()? refreshTokenCallback;
  final http.Client _client;
  static const timeoutDuration = Duration(seconds: 30);
  static const heavyReadTimeoutDuration = Duration(seconds: 60);

  ApiService({
    String? baseUrl,
    this.authToken,
    this.refreshTokenCallback,
    http.Client? client,
  })  : baseUrl = baseUrl ?? AppConfig.apiUrl,
        _client = client ?? http.Client();

  /// Executes an HTTP request with automatic retries for transient failures.
  /// Used for idempotent operations (GET) and heavy reads (POST search).
  Future<T> _requestWithRetry<T>(Future<T> Function() function) async {
    const r = RetryOptions(maxAttempts: 3, delayFactor: Duration(seconds: 1));
    return r.retry(
      function,
      retryIf: (e) =>
          e is SocketException ||
          e is TimeoutException ||
          e is http.ClientException ||
          (e is ServerException && _isTransientError(e)),
    );
  }

  bool _isTransientError(ServerException e) {
    // Retry 5xx errors and 429 Too Many Requests
    // We can check the message or implement status code in exception if needed
    // For now, assume ServerException covers 5xx
    return true;
  }

  Future<Map<String, String>> _getHeaders() async {
    final headers = <String, String>{
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };

    if (authToken != null) {
      headers['Authorization'] = 'Bearer $authToken';
    }

    return headers;
  }

  Future<http.Response> _authenticatedRequest(
      Future<http.Response> Function(Map<String, String> headers) request) async {
    final headers = await _getHeaders();
    final response = await request(headers);

    if (response.statusCode == 401 && refreshTokenCallback != null) {
      // Token might be expired, try refreshing
      developer.log('401 received, attempting token refresh', name: 'ApiService');
      await refreshTokenCallback!();
      // Retry with new token
      final newHeaders = await _getHeaders();
      return await request(newHeaders);
    }

    return response;
  }

  Future<ContextReport> getContextReport(String address) async {
    final uri = Uri.parse('$baseUrl/context/report');
    final body = json.encode({'address': address});

    try {
      final response = await _requestWithRetry(
        () => _authenticatedRequest(
          (headers) => _client
              .post(uri, headers: headers, body: body)
              .timeout(heavyReadTimeoutDuration),
        ),
      );

      return _handleResponse(
        response,
        (body) => ContextReport.fromJson(json.decode(body)),
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<List<ValoraNotification>> getNotifications({int? limit, int? offset}) async {
    final uri = Uri.parse('$baseUrl/notifications').replace(queryParameters: {
      if (limit != null) 'limit': limit.toString(),
      if (offset != null) 'offset': offset.toString(),
    });
    try {
      final response = await _requestWithRetry(
        () => _authenticatedRequest(
          (headers) =>
              _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
      );

      return _handleResponse(
        response,
        (body) {
          final List<dynamic> jsonList = json.decode(body);
          return jsonList.map((e) => ValoraNotification.fromJson(e)).toList();
        },
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<void> _parseNotifications(String body) {
    // No-op for now, used as generic parser
    return Future.value();
  }

  Future<void> sendNotification({
    required String title,
    required String message,
    required String type,
  }) async {
    final uri = Uri.parse('$baseUrl/notifications');
    final body = json.encode({
      'title': title,
      'message': message,
      'type': type,
    });

    // Helper to invoke parser for void
    void _runner(Future<void> Function(String) parser, String b) {}

    try {
      final response = await _requestWithRetry(
        () => _authenticatedRequest(
          (headers) => _client
              .post(uri, headers: headers, body: body)
              .timeout(timeoutDuration),
        ),
      );

      // Fix: Don't await void
      _handleResponse(
        response,
        (body) => _runner(_parseNotifications, body),
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<int> getUnreadNotificationCount() async {
    final uri = Uri.parse('$baseUrl/notifications/unread-count');
    try {
      final response = await _requestWithRetry(
        () => _authenticatedRequest(
          (headers) =>
              _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
      );

      return await _handleResponse(
        response,
        (body) {
          final jsonBody = json.decode(body);
          return jsonBody['count'] as int;
        },
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<void> markNotificationAsRead(String id) async {
    final uri = Uri.parse('$baseUrl/notifications/$id/read');
    try {
      final response = await _requestWithRetry(
        () => _authenticatedRequest(
          (headers) =>
              _client.post(uri, headers: headers).timeout(timeoutDuration),
        ),
      );
      await _handleResponse(response, (_) => null);
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<void> markAllNotificationsAsRead() async {
    final uri = Uri.parse('$baseUrl/notifications/read-all');
    try {
      final response = await _requestWithRetry(
        () => _authenticatedRequest(
          (headers) =>
              _client.post(uri, headers: headers).timeout(timeoutDuration),
        ),
      );
      await _handleResponse(response, (_) => null);
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<void> deleteNotification(String id) async {
    final uri = Uri.parse('$baseUrl/notifications/$id');
    try {
      final response = await _requestWithRetry(
        () => _authenticatedRequest(
          (headers) =>
              _client.delete(uri, headers: headers).timeout(timeoutDuration),
        ),
      );
      await _handleResponse(response, (_) => null);
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  /// Saved Properties Methods

  Future<List<SavedProperty>> getSavedProperties() async {
    final uri = Uri.parse('$baseUrl/saved-properties');
    try {
      final response = await _requestWithRetry(
        () => _authenticatedRequest(
          (headers) =>
              _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
      );

      return _handleResponse(
        response,
        (body) {
          final List<dynamic> jsonList = json.decode(body);
          return jsonList.map((e) => SavedProperty.fromJson(e)).toList();
        },
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<SavedProperty> saveProperty({
    required String address,
    required double latitude,
    required double longitude,
    String? cachedScore,
  }) async {
    final uri = Uri.parse('$baseUrl/saved-properties');
    final body = json.encode({
      'address': address,
      'latitude': latitude,
      'longitude': longitude,
      'cachedScore': cachedScore,
    });
    try {
      final response = await _requestWithRetry(
        () => _authenticatedRequest(
          (headers) => _client
              .post(uri, headers: headers, body: body)
              .timeout(timeoutDuration),
        ),
      );

      return _handleResponse(
        response,
        (body) => SavedProperty.fromJson(json.decode(body)),
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<void> deleteSavedProperty(String id) async {
    final uri = Uri.parse('$baseUrl/saved-properties/$id');
    try {
      final response = await _requestWithRetry(
        () => _authenticatedRequest(
          (headers) =>
              _client.delete(uri, headers: headers).timeout(timeoutDuration),
        ),
      );
      await _handleResponse(response, (_) => null);
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  /// Centralized response handler.
  /// Maps HTTP status codes to typed Application Exceptions for consistent UI error handling.
  /// Also logs non-success responses for debugging.
  T _handleResponse<T>(http.Response response, T Function(String body) parser) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return parser(response.body);
    }

    developer.log(
      'API Error: ${response.statusCode} - ${response.body}',
      name: 'ApiService',
    );

    final String? message = _parseErrorMessage(response.body);
    final String? traceId = _parseTraceId(response.body);
    final String traceSuffix = traceId != null ? ' (Ref: $traceId)' : '';

    switch (response.statusCode) {
      case 400:
        throw ValidationException(
          (message ?? 'Invalid request') + traceSuffix,
        );
      case 401:
        throw UnauthorizedException(
          (message ?? 'Unauthorized access') + traceSuffix,
        );
      case 403:
        throw ForbiddenException(
          (message ?? 'Access denied') + traceSuffix,
        );
      case 404:
        throw NotFoundException(
          (message ?? 'Resource not found') + traceSuffix,
        );
      case 409:
         throw ValidationException(
          (message ?? 'Conflict') + traceSuffix,
        );
      case 429:
        throw ServerException(
          'Too many requests. Please try again later.$traceSuffix',
        );
      case 503:
        throw ServerException(
          'Service is temporarily unavailable. Please try again later.$traceSuffix',
        );
      case 500:
      case 502:
      case 504:
        throw ServerException(
          'We are experiencing technical difficulties. Please try again later.$traceSuffix',
        );
      default:
        throw ServerException(
          'Request failed with status: ${response.statusCode}$traceSuffix',
        );
    }
  }

  Exception _handleException(dynamic error, StackTrace? stack, [Uri? uri]) {
    if (error is AppException) return error;

    // Redact query parameters to prevent PII leakage
    final redactedUri = uri?.replace(queryParameters: {}) ?? Uri();
    final urlString = redactedUri.toString().isEmpty ? 'unknown URL' : redactedUri.toString();

    developer.log('Network Error: $error (URI: $urlString)', name: 'ApiService');

    // Report non-business exceptions to Sentry
    CrashReportingService.captureException(
      error,
      stackTrace: stack ?? (error is Error ? error.stackTrace : null),
      context: {
        'url': urlString,
        'error_type': error.runtimeType.toString(),
      },
    );

    if (error is SocketException) {
      return NetworkException('No internet connection. Please check your settings.');
    } else if (error is TimeoutException) {
      return NetworkException('Request timed out. Please check your connection or try again later.');
    } else if (error is http.ClientException) {
      return NetworkException('Unable to reach the server. Please check your connection.');
    } else if (error is FormatException) {
      return JsonParsingException('Failed to process server response.');
    }

    return UnknownException('An unexpected error occurred. Please try again.');
  }

  /// Extracts the Correlation ID (TraceId) from standard Problem Details (RFC 7807) responses.
  /// This allows us to display a "Reference ID" to the user, which support can use to find
  /// the exact error log in the backend (Sentry/Kibana).
  String? _parseTraceId(String body) {
    try {
      final jsonBody = json.decode(body);
      if (jsonBody is Map<String, dynamic>) {
        // Look in extensions (RFC 7807)
        if (jsonBody['extensions'] is Map<String, dynamic>) {
          final extensions = jsonBody['extensions'] as Map<String, dynamic>;
          if (extensions['traceId'] is String) return extensions['traceId'] as String;
          if (extensions['requestId'] is String) return extensions['requestId'] as String;
        }
        // Look in root
        if (jsonBody['traceId'] is String) return jsonBody['traceId'] as String;
        if (jsonBody['requestId'] is String) return jsonBody['requestId'] as String;
      }
    } catch (_) {
      // Ignore parsing errors
    }
    return null;
  }

  String? _parseErrorMessage(String body) {
    try {
      final jsonBody = json.decode(body);
      if (jsonBody is Map<String, dynamic>) {
        // Handle FluentValidation 'errors' dictionary
        if (jsonBody['errors'] is Map<String, dynamic>) {
          final errors = jsonBody['errors'] as Map<String, dynamic>;
          final messages = errors.entries.map((e) {
            final value = e.value;
            if (value is List) return value.join(', ');
            return value.toString();
          });
          return messages.join('\n');
        }

        // Handle RFC 7807 problem details
        return jsonBody['detail'] as String? ?? jsonBody['title'] as String?;
      }
    } catch (_) {
      // Ignore parsing errors
    }
    return null;
  }

  Future<bool> healthCheck() async {
    final uri = Uri.parse('$baseUrl/health');
    try {
      final response = await _client.get(uri).timeout(timeoutDuration);
      return response.statusCode == 200;
    } catch (_) {
      return false;
    }
  }

  Future<List<MapCityInsight>> getCityInsights() async {
    final uri = Uri.parse('$baseUrl/map/cities');
    try {
      final response = await _requestWithRetry(
        () => _authenticatedRequest(
          (headers) =>
              _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
      );

      return _handleResponse(
        response,
        (body) {
          final List<dynamic> jsonList = json.decode(body);
          return jsonList.map((e) => MapCityInsight.fromJson(e)).toList();
        },
      );
    } catch (e, stack) {
      final uri = Uri.parse('$baseUrl/map/cities');
      throw _handleException(e, stack, uri);
    }
  }

  Future<List<MapAmenity>> getMapAmenities({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    List<String>? types,
  }) async {
    final uri = Uri.parse('$baseUrl/map/amenities').replace(queryParameters: {
      'minLat': minLat.toString(),
      'minLon': minLon.toString(),
      'maxLat': maxLat.toString(),
      'maxLon': maxLon.toString(),
      if (types != null) 'types': types.join(','),
    });
    try {
      final response = await _requestWithRetry(
        () => _authenticatedRequest(
          (headers) =>
              _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
      );

      return _handleResponse(
        response,
        (body) {
          final List<dynamic> jsonList = json.decode(body);
          return jsonList.map((e) => MapAmenity.fromJson(e)).toList();
        },
      );
    } catch (e, stack) {
      throw _handleException(e, stack, Uri.parse('$baseUrl/map/amenities'));
    }
  }

  Future<List<MapOverlay>> getMapOverlays({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    required String metric,
  }) async {
    final uri = Uri.parse('$baseUrl/map/overlays').replace(queryParameters: {
      'minLat': minLat.toString(),
      'minLon': minLon.toString(),
      'maxLat': maxLat.toString(),
      'maxLon': maxLon.toString(),
      'metric': metric,
    });
    try {
      final response = await _requestWithRetry(
        () => _authenticatedRequest(
          (headers) =>
              _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
      );

      return _handleResponse(
        response,
        (body) {
          final List<dynamic> jsonList = json.decode(body);
          return jsonList.map((e) => MapOverlay.fromJson(e)).toList();
        },
      );
    } catch (e, stack) {
      throw _handleException(e, stack, Uri.parse('$baseUrl/map/overlays'));
    }
  }
}
