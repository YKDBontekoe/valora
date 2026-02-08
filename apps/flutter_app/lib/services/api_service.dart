import 'package:flutter/foundation.dart';
import 'dart:async';
import 'dart:convert';
import 'dart:developer' as developer;
import 'dart:io';
import 'package:http/http.dart' as http;
import 'package:retry/retry.dart';
import '../core/exceptions/app_exceptions.dart';
import '../core/config/app_config.dart';
import '../models/listing.dart';
import '../models/listing_filter.dart';
import '../models/listing_response.dart';
import '../models/context_report.dart';
import '../models/notification.dart';

typedef ComputeCallback<Q, R> = FutureOr<R> Function(Q message);
typedef ComputeRunner =
    Future<R> Function<Q, R>(ComputeCallback<Q, R> callback, Q message);

class ApiService {
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
    } catch (e) {
      final uri = Uri.parse('$baseUrl/listings')
          .replace(queryParameters: filter.toQueryParameters());
      throw _handleException(e, uri);
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
    } catch (e) {
      throw _handleException(e, listingUri);
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
    } catch (e) {
      throw _handleException(e, uri);
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
    } catch (e) {
      throw _handleException(e, uri);
    }
  }

  Future<List<ValoraNotification>> getNotifications({
    bool unreadOnly = false,
    int limit = 50,
  }) async {
    Uri? uri;
    try {
      uri = Uri.parse('$baseUrl/notifications').replace(
        queryParameters: {
          'unreadOnly': unreadOnly.toString(),
          'limit': limit.toString(),
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
    } catch (e) {
      throw _handleException(e, uri);
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
    } catch (e) {
      throw _handleException(e, uri);
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
    } catch (e) {
      throw _handleException(e, uri);
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
    } catch (e) {
      throw _handleException(e, uri);
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

  Exception _handleException(dynamic error, [Uri? uri]) {
    if (error is AppException) return error;

    final urlString = uri?.toString() ?? 'unknown URL';
    developer.log('Network Error: $error (URI: $urlString)', name: 'ApiService');

    if (error is SocketException) {
      return NetworkException('Server unreachable at $urlString. Please ensure the backend is running.');
    } else if (error is TimeoutException) {
      return NetworkException('Request to $urlString timed out.');
    } else if (error is http.ClientException) {
      return NetworkException('Connection failed to $urlString. Please check your network.');
    } else if (error is FormatException) {
      return JsonParsingException();
    }

    return UnknownException('Unexpected error: $error');
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

// Top-level functions for isolate
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
