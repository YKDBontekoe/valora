import 'dart:async';
import 'dart:convert';
import 'dart:developer' as developer;
import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:retry/retry.dart';

import '../core/config/app_config.dart';
import '../core/exceptions/app_exceptions.dart';
import 'crash_reporting_service.dart';
import '../models/context_report.dart';
import '../models/listing.dart';
import '../models/listing_filter.dart';
import '../models/listing_response.dart';
import '../models/notification.dart';

// Top-level function for compute
ListingResponse _parseListingResponse(String body) {
  return ListingResponse.fromJson(json.decode(body));
}

Listing _parseListing(String body) {
  return Listing.fromJson(json.decode(body));
}

List<ValoraNotification> _parseNotifications(String body) {
  final List<dynamic> list = json.decode(body);
  return list.map((e) => ValoraNotification.fromJson(e)).toList();
}

ContextReport _parseContextReport(String body) {
  return ContextReport.fromJson(json.decode(body));
}

typedef ComputeRunner = Future<R> Function<Q, R>(
  ComputeCallback<Q, R> callback,
  Q message, {
  String? debugLabel,
});

class ApiService {
  // baseUrl is provided by AppConfig which handles dotenv and fallbacks.
  static String get baseUrl => AppConfig.apiUrl;

  static const Duration timeoutDuration = Duration(seconds: 30);

  final http.Client _client;
  String? _authToken;
  final Future<String?> Function()? _refreshTokenCallback;
  final RetryOptions _retryOptions;
  final ComputeRunner _runner;

  ApiService({
    http.Client? client,
    String? authToken,
    Future<String?> Function()? refreshTokenCallback,
    RetryOptions? retryOptions,
    ComputeRunner? runner,
  }) : _client = client ?? http.Client(),
       _authToken = authToken,
       _refreshTokenCallback = refreshTokenCallback,
       _retryOptions =
           retryOptions ??
           const RetryOptions(
             maxAttempts: 3,
             delayFactor: Duration(seconds: 1),
           ),
       _runner = runner ?? compute;

  Map<String, String> get _headers => {
    if (_authToken != null) 'Authorization': 'Bearer $_authToken',
    'Content-Type': 'application/json',
  };

  Future<http.Response> _authenticatedRequest(
    Future<http.Response> Function(Map<String, String> headers) request,
  ) async {
    return await _retryOptions.retry(
      () async {
        final response = await request(_headers);

        if (response.statusCode == 401 && _refreshTokenCallback != null) {
          final newToken = await _refreshTokenCallback();
          if (newToken != null) {
            _authToken = newToken;
            return await request(_headers);
          }
        }

        // Force retry on server errors
        if (response.statusCode >= 500) {
          throw ServerException(
            'Server error (${response.statusCode}). Please try again later.',
          );
        }

        return response;
      },
      retryIf: (e) =>
          e is SocketException ||
          e is TimeoutException ||
          e is http.ClientException ||
          e is ServerException,
    );
  }

  Future<bool> healthCheck() async {
    final uri = Uri.parse('$baseUrl/health');
    try {
      final response = await _client
          .get(uri, headers: _headers)
          .timeout(timeoutDuration);
      return response.statusCode == 200;
    } catch (e) {
      developer.log('Health check failed for $uri: $e', name: 'ApiService');
      return false;
    }
  }

  Future<ListingResponse> getListings(ListingFilter filter) async {
    try {
      final uri = Uri.parse(
        '$baseUrl/listings',
      ).replace(queryParameters: filter.toQueryParameters());

      final response = await _authenticatedRequest(
        (headers) =>
            _client.get(uri, headers: headers).timeout(timeoutDuration),
      );

      return await _handleResponse(
        response,
        (body) => _runner(_parseListingResponse, body),
      );
    } catch (e, stack) {
      final uri = Uri.parse('$baseUrl/listings')
          .replace(queryParameters: filter.toQueryParameters());
      throw _handleException(e, stack, uri);
    }
  }

  Future<Listing?> getListing(String id) async {
    Uri? listingUri;
    try {
      final String sanitizedId = _sanitizeListingId(id);
      final Uri baseUri = Uri.parse(baseUrl);
      listingUri = baseUri.replace(
        pathSegments: <String>[
          ...baseUri.pathSegments.where((segment) => segment.isNotEmpty),
          'listings',
          sanitizedId,
        ],
      );

      final response = await _authenticatedRequest(
        (headers) =>
            _client.get(listingUri!, headers: headers).timeout(timeoutDuration),
      );

      return await _handleResponse(
        response,
        (body) => _runner(_parseListing, body),
      );
    } catch (e, stack) {
      throw _handleException(e, stack, listingUri);
    }
  }

  Future<Listing?> getListingFromPdok(String id) async {
    Uri? uri;
    try {
      uri = Uri.parse('$baseUrl/listings/lookup').replace(queryParameters: {'id': id});
      
      final response = await _authenticatedRequest(
        (headers) => _client.get(uri!, headers: headers).timeout(timeoutDuration),
      );

      return await _handleResponse(
        response,
        (body) => _runner(_parseListing, body),
      );
    } on NotFoundException {
      return null;
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<ContextReport> getContextReport(
    String input, {
    int radiusMeters = 1000,
  }) async {
    final uri = Uri.parse('$baseUrl/context/report');
    try {
      final payload = json.encode(<String, dynamic>{
        'input': input,
        'radiusMeters': radiusMeters,
      });

      final response = await _authenticatedRequest(
        (headers) => _client
            .post(uri, headers: headers, body: payload)
            .timeout(timeoutDuration),
      );

      return await _handleResponse(
        response,
        (body) => _runner(_parseContextReport, body),
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<String> getAiAnalysis(ContextReport report) async {
    final uri = Uri.parse('$baseUrl/ai/analyze-report');
    try {
      final payload = json.encode({
        'report': report.toJson(),
      });

      final response = await _authenticatedRequest(
        (headers) => _client
            .post(uri, headers: headers, body: payload)
            .timeout(const Duration(seconds: 60)), // AI takes longer
      );

      return await _handleResponse(
        response,
        (body) {
          final jsonBody = json.decode(body);
          return jsonBody['summary'] as String;
        },
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<List<ValoraNotification>> getNotifications({
    bool unreadOnly = false,
    int limit = 50,
    int offset = 0,
  }) async {
    Uri? uri;
    try {
      uri = Uri.parse('$baseUrl/notifications').replace(
        queryParameters: {
          'unreadOnly': unreadOnly.toString(),
          'limit': limit.toString(),
          'offset': offset.toString(),
        },
      );

      final response = await _authenticatedRequest(
        (headers) =>
            _client.get(uri!, headers: headers).timeout(timeoutDuration),
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
      final response = await _authenticatedRequest(
        (headers) =>
            _client.get(uri, headers: headers).timeout(timeoutDuration),
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
        (headers) =>
            _client.post(uri, headers: headers).timeout(timeoutDuration),
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
        (headers) =>
            _client.post(uri, headers: headers).timeout(timeoutDuration),
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
        (headers) =>
            _client.delete(uri, headers: headers).timeout(timeoutDuration),
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

    switch (response.statusCode) {
      case 400:
        throw ValidationException(
          _parseErrorMessage(response.body) ?? 'Invalid request',
        );
      case 404:
        throw NotFoundException('Resource not found');
      case 500:
      case 502:
      case 503:
      case 504:
        throw ServerException(
          'Server error (${response.statusCode}). Please try again later.',
        );
      default:
        throw UnknownException(
          'Request failed with status: ${response.statusCode}',
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
      return NetworkException('Unable to reach the server. Please check your internet connection.');
    } else if (error is TimeoutException) {
      return NetworkException('The request timed out. Please try again later.');
    } else if (error is http.ClientException) {
      return NetworkException('Connection failed. Please check your network settings.');
    } else if (error is FormatException) {
      return JsonParsingException('Failed to process server response.');
    }

    return UnknownException('An unexpected error occurred. Please try again.');
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

  String _sanitizeListingId(String id) {
    final String sanitized = id.trim();
    if (sanitized.isEmpty || sanitized.contains(RegExp(r'[/?#]'))) {
      throw ValidationException('Invalid listing identifier');
    }
    return sanitized;
  }
}
