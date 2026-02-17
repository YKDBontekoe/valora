import 'dart:async';
import 'dart:convert';
import 'dart:developer' as developer;
import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:retry/retry.dart';

import '../core/config/app_config.dart';
import '../core/exceptions/app_exceptions.dart';
import '../models/context_report.dart';
import '../models/listing.dart';
import '../models/listing_filter.dart';
import '../models/listing_response.dart';
import '../models/map_city_insight.dart';
import '../models/map_amenity.dart';
import '../models/map_overlay.dart';
import '../models/notification.dart';
import 'crash_reporting_service.dart';

typedef ApiRunner = Future<R> Function<Q, R>(ComputeCallback<Q, R> callback, Q message, {String? debugLabel});

class ApiService {
  String? _authToken;
  final Future<String?> Function()? _refreshTokenCallback;
  final http.Client _client;
  final ApiRunner _runner;
  final RetryOptions _retryOptions;

  static String get baseUrl => AppConfig.apiUrl;
  static const timeoutDuration = Duration(seconds: 15);

  ApiService({
    String? authToken,
    Future<String?> Function()? refreshTokenCallback,
    http.Client? client,
    ApiRunner? runner,
    RetryOptions? retryOptions,
  })  : _authToken = authToken,
        _refreshTokenCallback = refreshTokenCallback,
        _client = client ?? http.Client(),
        _runner = runner ?? _defaultRunner,
        _retryOptions = retryOptions ?? const RetryOptions(maxAttempts: 3);

  static Future<R> _defaultRunner<Q, R>(ComputeCallback<Q, R> callback, Q message, {String? debugLabel}) async {
    return await callback(message);
  }

  Future<T> _executeRequest<T>(
    Future<http.Response> Function(Map<String, String> headers) request,
    FutureOr<T> Function(String body) parser,
    Uri uri, {
    Duration? timeout,
    bool retry = true,
  }) async {
    try {
      final response = retry
          ? await _retryOptions.retry(
              () => _authenticatedRequest(
                (headers) => request(headers).timeout(timeout ?? timeoutDuration),
              ),
              retryIf: (e) => e is SocketException || e is TimeoutException,
            )
          : await _authenticatedRequest(
              (headers) => request(headers).timeout(timeout ?? timeoutDuration),
            );

      return await _handleResponse(response, parser);
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<http.Response> _authenticatedRequest(
    Future<http.Response> Function(Map<String, String> headers) request,
  ) async {
    final headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      if (_authToken != null) 'Authorization': 'Bearer $_authToken',
    };

    var response = await request(headers);

    if (response.statusCode == 401 && _refreshTokenCallback != null) {
      final newToken = await _refreshTokenCallback();
      if (newToken != null) {
        _authToken = newToken;
        headers['Authorization'] = 'Bearer $newToken';
        response = await request(headers);
      }
    }

    return response;
  }

  Listing _parseListing(String body) {
    return Listing.fromJson(json.decode(body));
  }

  ListingResponse _parseListings(String body) {
    return ListingResponse.fromJson(json.decode(body));
  }

  ContextReport _parseContextReport(String body) {
    return ContextReport.fromJson(json.decode(body));
  }

  List<ValoraNotification> _parseNotifications(String body) {
    final List<dynamic> jsonList = json.decode(body);
    return jsonList.map((e) => ValoraNotification.fromJson(e)).toList();
  }

  Future<ListingResponse> getListings(
    ListingFilter filter, {
    int page = 1,
    int pageSize = 20,
  }) async {
    final queryParams = {
      'page': page.toString(),
      'pageSize': pageSize.toString(),
      ...filter.toQueryParameters(),
    };
    final uri = Uri.parse('$baseUrl/listings').replace(queryParameters: queryParams);

    return _executeRequest(
      (headers) => _client.get(uri, headers: headers),
      (body) => _runner(_parseListings, body),
      uri,
    );
  }

  Future<Listing> getListing(String id) async {
    final sanitizedId = _sanitizeListingId(id);
    final uri = Uri.parse('$baseUrl/listings/$sanitizedId');

    return _executeRequest(
      (headers) => _client.get(uri, headers: headers),
      (body) => _runner(_parseListing, body),
      uri,
    );
  }

  Future<Listing?> getListingFromPdok(String id) async {
    final uri = Uri.parse('$baseUrl/listings/lookup').replace(queryParameters: {'id': id});

    try {
      return await _executeRequest(
        (headers) => _client.get(uri, headers: headers),
        (body) => _runner(_parseListing, body),
        uri,
      );
    } on NotFoundException {
      return null;
    }
  }

  Future<ContextReport> getContextReport(
    String input, {
    int radiusMeters = 1000,
  }) async {
    final uri = Uri.parse('$baseUrl/context/report');
    final payload = json.encode(<String, dynamic>{
      'input': input,
      'radiusMeters': radiusMeters,
    });

    // Allowing retries for report generation as it's a read-heavy operation in this context
    return _executeRequest(
      (headers) => _client.post(uri, headers: headers, body: payload),
      (body) => _runner(_parseContextReport, body),
      uri,
    );
  }

  Future<String> getAiAnalysis(ContextReport report) async {
    final uri = Uri.parse('$baseUrl/ai/analyze-report');
    final payload = json.encode({
      'report': report.toJson(),
    });

    return _executeRequest(
      (headers) => _client.post(uri, headers: headers, body: payload),
      (body) async {
        final jsonBody = json.decode(body);
        return jsonBody['summary'] as String;
      },
      uri,
      timeout: const Duration(seconds: 60),
    );
  }

  Future<List<ValoraNotification>> getNotifications({
    bool unreadOnly = false,
    int limit = 50,
    int offset = 0,
  }) async {
    final uri = Uri.parse('$baseUrl/notifications').replace(
      queryParameters: {
        'unreadOnly': unreadOnly.toString(),
        'limit': limit.toString(),
        'offset': offset.toString(),
      },
    );

    return _executeRequest(
      (headers) => _client.get(uri, headers: headers),
      (body) => _runner(_parseNotifications, body),
      uri,
    );
  }

  Future<int> getUnreadNotificationCount() async {
    final uri = Uri.parse('$baseUrl/notifications/unread-count');

    return _executeRequest(
      (headers) => _client.get(uri, headers: headers),
      (body) async {
        final jsonBody = json.decode(body);
        return jsonBody['count'] as int;
      },
      uri,
    );
  }

  Future<void> markNotificationAsRead(String id) async {
    final uri = Uri.parse('$baseUrl/notifications/$id/read');

    await _executeRequest(
      (headers) => _client.post(uri, headers: headers),
      (_) async => null,
      uri,
    );
  }

  Future<void> markAllNotificationsAsRead() async {
    final uri = Uri.parse('$baseUrl/notifications/read-all');

    await _executeRequest(
      (headers) => _client.post(uri, headers: headers),
      (_) async => null,
      uri,
    );
  }

  Future<void> deleteNotification(String id) async {
    final uri = Uri.parse('$baseUrl/notifications/$id');

    await _executeRequest(
      (headers) => _client.delete(uri, headers: headers),
      (_) async => null,
      uri,
    );
  }

  FutureOr<T> _handleResponse<T>(http.Response response, FutureOr<T> Function(String body) parser) {
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

      // Handle legacy error array format: [{ "property": "...", "error": "..." }]
      if (jsonBody is List) {
        return jsonBody.map((e) {
          if (e is Map<String, dynamic> && e['error'] != null) {
            return e['error'].toString();
          }
          return e.toString();
        }).join('\n');
      }

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

    return _executeRequest(
      (headers) => _client.get(uri, headers: headers),
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => MapCityInsight.fromJson(e)).toList();
      },
      uri,
    );
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

    return _executeRequest(
      (headers) => _client.get(uri, headers: headers),
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => MapAmenity.fromJson(e)).toList();
      },
      uri,
    );
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

    return _executeRequest(
      (headers) => _client.get(uri, headers: headers),
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => MapOverlay.fromJson(e)).toList();
      },
      uri,
    );
  }
}
