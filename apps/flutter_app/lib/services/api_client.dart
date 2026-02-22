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

typedef ApiRunner = Future<R> Function<Q, R>(ComputeCallback<Q, R> callback, Q message, {String? debugLabel});

class ApiClient {
  String? _authToken;
  final Future<String?> Function()? _refreshTokenCallback;
  final http.Client _client;
  final ApiRunner _runner;
  final RetryOptions _retryOptions;

  static String get baseUrl => AppConfig.apiUrl;
  static const timeoutDuration = Duration(seconds: 15);

  ApiClient({
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

  void updateAuthToken(String? token) {
    _authToken = token;
  }

  void dispose() {
    _client.close();
  }

  Future<http.Response> request(
    String method,
    String path, {
    Map<String, dynamic>? queryParameters,
    dynamic data,
    Map<String, String>? headers,
    Duration? timeout,
  }) async {
    final uri = Uri.parse('$baseUrl$path').replace(queryParameters: queryParameters);
    try {
      return await _requestWithRetry(
        () => _authenticatedRequest(
          (reqHeaders) {
            final mergedHeaders = {...reqHeaders, ...?headers};

            // Add Content-Type if body is present
            if (data != null && !mergedHeaders.containsKey('Content-Type')) {
              mergedHeaders['Content-Type'] = 'application/json';
            }

            // Encode body if json
            final body = data != null ? json.encode(data) : null;

            switch (method.toUpperCase()) {
              case 'GET':
                return _client.get(uri, headers: mergedHeaders);
              case 'POST':
                return _client.post(uri, headers: mergedHeaders, body: body);
              case 'PUT':
                return _client.put(uri, headers: mergedHeaders, body: body);
              case 'DELETE':
                return _client.delete(uri, headers: mergedHeaders, body: body);
              case 'PATCH':
                return _client.patch(uri, headers: mergedHeaders, body: body);
              default:
                throw UnsupportedError('Method $method is not supported');
            }
          },
          timeout: timeout,
        ),
      );
    } catch (e, stack) {
      throw _handleException(e, stack, uri);
    }
  }

  Future<http.Response> get(String path, {Map<String, dynamic>? queryParameters, Map<String, String>? headers}) {
    return request('GET', path, queryParameters: queryParameters, headers: headers);
  }

  Future<http.Response> post(String path, {dynamic data, Map<String, dynamic>? queryParameters, Map<String, String>? headers, Duration? timeout}) {
    return request('POST', path, data: data, queryParameters: queryParameters, headers: headers, timeout: timeout);
  }

  Future<http.Response> put(String path, {dynamic data, Map<String, dynamic>? queryParameters, Map<String, String>? headers}) {
    return request('PUT', path, data: data, queryParameters: queryParameters, headers: headers);
  }

  Future<http.Response> delete(String path, {dynamic data, Map<String, dynamic>? queryParameters, Map<String, String>? headers}) {
    return request('DELETE', path, data: data, queryParameters: queryParameters, headers: headers);
  }

  Future<http.Response> _requestWithRetry(
    Future<http.Response> Function() requestFn,
  ) {
    return _retryOptions.retry(
      () async {
        final response = await requestFn();
        if (response.statusCode >= 500 || response.statusCode == 429) {
          throw TransientHttpException(
            'Service temporarily unavailable (Status: ${response.statusCode})',
          );
        }
        return response;
      },
      retryIf: (e) =>
          e is SocketException ||
          e is TimeoutException ||
          e is TransientHttpException,
    );
  }

  Future<http.Response> _authenticatedRequest(
    Future<http.Response> Function(Map<String, String> headers) request, {
    Duration? timeout,
  }) async {
    final headers = {
      'Accept': 'application/json',
      if (_authToken != null) 'Authorization': 'Bearer $_authToken',
    };

    // Helper to add timeout
    Future<http.Response> withTimeout(Future<http.Response> req) {
      return req.timeout(timeout ?? timeoutDuration);
    }

    var response = await withTimeout(request(headers));

    if (response.statusCode == 401 && _refreshTokenCallback != null) {
      final newToken = await _refreshTokenCallback();
      if (newToken != null) {
        _authToken = newToken;
        headers['Authorization'] = 'Bearer $newToken';
        response = await withTimeout(request(headers));
      }
    }

    return response;
  }

  Exception _handleException(dynamic error, StackTrace? stack, [Uri? uri]) {
    if (error is AppException) return error;

    // Redact query parameters to prevent PII leakage
    final redactedUri = uri?.replace(queryParameters: {}) ?? Uri();
    final urlString = redactedUri.toString().isEmpty ? 'unknown URL' : redactedUri.toString();

    developer.log('Network Error: $error (URI: $urlString)', name: 'ApiClient');

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

  Future<T> handleResponse<T>(http.Response response, T Function(String body) parser) async {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      // If parser involves heavy computation, consider using _runner (compute)
      // But _runner is instance method. We can expose runner or just execute here.
      // Since parsing might be simple JSON decoding, executing here is fine.
      // If heavy, the caller can wrap parser in compute.
      // However, ApiService was using _runner(parser, body).
      return await _runner(parser, response.body);
    }

    developer.log(
      'API Error: ${response.statusCode} - ${response.body}',
      name: 'ApiClient',
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
      default:
        throw ServerException(
          (message ?? 'An unexpected error occurred.') + traceSuffix,
        );
    }
  }
}
