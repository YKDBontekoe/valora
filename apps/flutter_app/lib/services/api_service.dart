import 'dart:convert';
import 'dart:developer' as developer;
import 'dart:io';
import 'dart:async';

import 'package:http/http.dart' as http;
import 'package:retry/retry.dart';

import '../core/config/app_config.dart';
import '../core/exceptions/app_exceptions.dart';
import '../models/listing.dart';
import '../models/listing_filter.dart';
import '../models/listing_response.dart';
import '../models/notification.dart';
import '../models/map_city_insight.dart';
import '../models/map_amenity.dart';
import '../models/map_overlay.dart';
import '../models/context_report.dart';
import 'crash_reporting_service.dart';

class ApiService {
  final String baseUrl;
  final String? authToken;
  final Future<String?> Function()? refreshTokenCallback;
  final http.Client _client;
  final RetryOptions _retryOptions;
  final Duration timeoutDuration;

  ApiService({
    String? baseUrl,
    this.authToken,
    this.refreshTokenCallback,
    http.Client? client,
    RetryOptions? retryOptions,
    this.timeoutDuration = const Duration(seconds: 30),
  }) : baseUrl = baseUrl ?? AppConfig.apiUrl,
       _client = client ?? http.Client(),
       _retryOptions = retryOptions ?? const RetryOptions(
         maxAttempts: 3,
         delayFactor: Duration(seconds: 2),
       );

  Future<Map<String, String>> _getHeaders() async {
    return {
      'Content-Type': 'application/json',
      if (authToken != null) 'Authorization': 'Bearer $authToken',
    };
  }

  Future<http.Response> _authenticatedRequest(
    Future<http.Response> Function(Map<String, String> headers) requestFn,
  ) async {
    var headers = await _getHeaders();
    var response = await requestFn(headers);

    if (response.statusCode == 401 && refreshTokenCallback != null) {
      final newToken = await refreshTokenCallback!();
      if (newToken != null) {
        headers['Authorization'] = 'Bearer $newToken';
        response = await requestFn(headers);
      }
    }

    return response;
  }

  Future<T> _runner<T>(T Function(String) parser, String body) async {
    return parser(body);
  }

  ListingResponse _parseListings(String body) {
    return ListingResponse.fromJson(json.decode(body));
  }

  List<ValoraNotification> _parseNotifications(String body) {
    final List<dynamic> jsonList = json.decode(body);
    return jsonList.map((e) => ValoraNotification.fromJson(e)).toList();
  }

  Future<ListingResponse> getListings(ListingFilter filter) async {
    final uri = Uri.parse('$baseUrl/listings').replace(
      queryParameters: filter.toQueryParameters(),
    );

    try {
      final response = await _retryOptions.retry(
        () => _authenticatedRequest(
          (headers) => _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
        retryIf: (e) => e is SocketException || e is TimeoutException,
      );

      return await _handleResponse(
        response,
        (body) => _runner(_parseListings, body),
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<Listing> getListingById(String id) async {
    final sanitizedId = _sanitizeListingId(id);
    final uri = Uri.parse('$baseUrl/listings/$sanitizedId');

    try {
      final response = await _retryOptions.retry(
        () => _authenticatedRequest(
          (headers) => _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
        retryIf: (e) => e is SocketException || e is TimeoutException,
      );

      return await _handleResponse(
        response,
        (body) => Listing.fromJson(json.decode(body)),
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<Listing> lookupListing(String pdokId) async {
    final String sanitizedId = Uri.encodeComponent(pdokId);
    final uri = Uri.parse('$baseUrl/listings/lookup?id=$sanitizedId');
    try {
      final response = await _retryOptions.retry(
        () => _authenticatedRequest(
          (headers) =>
              _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
        retryIf: (e) => e is SocketException || e is TimeoutException,
      );

      return await _handleResponse(
        response,
        (body) => Listing.fromJson(json.decode(body)),
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<ContextReport> getContextReport(String input, {int radiusMeters = 1000}) async {
    final uri = Uri.parse('$baseUrl/context/report');
    try {
      final response = await _retryOptions.retry(
        () => _authenticatedRequest(
          (headers) => _client.post(
            uri,
            headers: headers,
            body: json.encode({
              'input': input,
              'radiusMeters': radiusMeters,
            }),
          ).timeout(timeoutDuration),
        ),
        retryIf: (e) => e is SocketException || e is TimeoutException,
      );

      return await _handleResponse(
        response,
        (body) => ContextReport.fromJson(json.decode(body)),
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<String> getAiAnalysis(ContextReport report) async {
    final uri = Uri.parse('$baseUrl/ai/analyze-report');
    try {
      final response = await _retryOptions.retry(
        () => _authenticatedRequest(
          (headers) => _client.post(
            uri,
            headers: headers,
            body: json.encode({'report': report.toJson()}),
          ).timeout(timeoutDuration),
        ),
        retryIf: (e) => e is SocketException || e is TimeoutException,
      );

      return await _handleResponse(
        response,
        (body) {
          final jsonBody = json.decode(body);
          return jsonBody['analysis'] as String;
        },
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<List<ValoraNotification>> getNotifications({int limit = 20, int offset = 0}) async {
    final uri = Uri.parse('$baseUrl/notifications').replace(queryParameters: {
      'limit': limit.toString(),
      'offset': offset.toString(),
    });

    try {
      final response = await _retryOptions.retry(
        () => _authenticatedRequest(
          (headers) => _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
        retryIf: (e) => e is SocketException || e is TimeoutException,
      );

      return await _handleResponse(
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
      final response = await _retryOptions.retry(
        () => _authenticatedRequest(
          (headers) => _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
        retryIf: (e) => e is SocketException || e is TimeoutException,
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
      final response = await _authenticatedRequest(
        (headers) => _client.post(uri, headers: headers).timeout(timeoutDuration),
      );
      await _handleResponse(response, (_) => null);
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<void> markAllNotificationsAsRead() async {
    final uri = Uri.parse('$baseUrl/notifications/read-all');
    try {
      final response = await _authenticatedRequest(
        (headers) => _client.post(uri, headers: headers).timeout(timeoutDuration),
      );
      await _handleResponse(response, (_) => null);
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<void> deleteNotification(String id) async {
    final uri = Uri.parse('$baseUrl/notifications/$id');
    try {
      final response = await _authenticatedRequest(
        (headers) => _client.delete(uri, headers: headers).timeout(timeoutDuration),
      );
      await _handleResponse(response, (_) => null);
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

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
        throw ValidationException(message ?? 'Invalid request');
      case 401:
        throw UnauthorizedException(message ?? 'Unauthorized access');
      case 403:
        throw ForbiddenException(message ?? 'Access denied');
      case 404:
        throw NotFoundException(message ?? 'Resource not found');
      case 429:
        throw ServerException('Too many requests. Please try again later.$traceSuffix');
      case 503:
        throw ServerException('Service is temporarily unavailable. Please try again later.$traceSuffix');
      case 500:
      case 502:
      case 504:
        throw ServerException('We are experiencing technical difficulties. Please try again later.$traceSuffix');
      default:
        throw ServerException('Request failed with status: ${response.statusCode}$traceSuffix');
    }
  }

  Exception _handleException(dynamic error, StackTrace? stack, [Uri? uri]) {
    if (error is AppException) return error;

    final redactedUri = uri?.replace(queryParameters: {}) ?? Uri();
    final urlString = redactedUri.toString().isEmpty ? 'unknown URL' : redactedUri.toString();

    developer.log('Network Error: $error (URI: $urlString)', name: 'ApiService');

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

  String? _parseTraceId(String body) {
    try {
      final jsonBody = json.decode(body);
      if (jsonBody is Map<String, dynamic>) {
        if (jsonBody['extensions'] is Map<String, dynamic>) {
          final extensions = jsonBody['extensions'] as Map<String, dynamic>;
          if (extensions['traceId'] is String) return extensions['traceId'] as String;
          if (extensions['requestId'] is String) return extensions['requestId'] as String;
        }
        if (jsonBody['traceId'] is String) return jsonBody['traceId'] as String;
        if (jsonBody['requestId'] is String) return jsonBody['requestId'] as String;
      }
    } catch (_) {}
    return null;
  }

  String? _parseErrorMessage(String body) {
    try {
      final jsonBody = json.decode(body);
      if (jsonBody is Map<String, dynamic>) {
        if (jsonBody['errors'] is Map<String, dynamic>) {
          final errors = jsonBody['errors'] as Map<String, dynamic>;
          final messages = errors.entries.map((e) {
            final value = e.value;
            if (value is List) return value.join(', ');
            return value.toString();
          });
          return messages.join('\n');
        }
        return jsonBody['detail'] as String? ?? jsonBody['title'] as String?;
      }
    } catch (_) {}
    return null;
  }

  String _sanitizeListingId(String id) {
    final String sanitized = id.trim();
    if (sanitized.isEmpty || sanitized.contains(RegExp(r'[/?#]'))) {
      throw ValidationException('Invalid listing identifier');
    }
    return sanitized;
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
      final response = await _retryOptions.retry(
        () => _authenticatedRequest(
          (headers) => _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
        retryIf: (e) => e is SocketException || e is TimeoutException,
      );

      return _handleResponse(
        response,
        (body) {
          final List<dynamic> jsonList = json.decode(body);
          return jsonList.map((e) => MapCityInsight.fromJson(e)).toList();
        },
      );
    } catch (e, stack) {
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
      final response = await _retryOptions.retry(
        () => _authenticatedRequest(
          (headers) => _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
        retryIf: (e) => e is SocketException || e is TimeoutException,
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
      final response = await _retryOptions.retry(
        () => _authenticatedRequest(
          (headers) => _client.get(uri, headers: headers).timeout(timeoutDuration),
        ),
        retryIf: (e) => e is SocketException || e is TimeoutException,
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
