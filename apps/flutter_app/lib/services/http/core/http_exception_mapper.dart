import 'dart:async';
import 'dart:convert';
import 'dart:developer' as developer;
import 'dart:io';

import 'package:http/http.dart' as http;

import '../../../core/exceptions/app_exceptions.dart';
import '../../crash_reporting_service.dart';

class HttpExceptionMapper {
  const HttpExceptionMapper();

  T parseOrThrow<T>(http.Response response, T Function(String body) parser) {
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

  Exception mapException(dynamic error, StackTrace? stack, [Uri? uri]) {
    if (error is AppException) {
      return error;
    }

    final Uri redactedUri = uri?.replace(queryParameters: <String, String>{}) ?? Uri();
    final String urlString = redactedUri.toString().isEmpty ? 'unknown URL' : redactedUri.toString();

    developer.log('Network Error: $error (URI: $urlString)', name: 'ApiService');

    CrashReportingService.captureException(
      error,
      stackTrace: stack ?? (error is Error ? error.stackTrace : null),
      context: <String, dynamic>{
        'url': urlString,
        'error_type': error.runtimeType.toString(),
      },
    );

    if (error is SocketException) {
      return NetworkException('No internet connection. Please check your settings.');
    }
    if (error is TimeoutException) {
      return NetworkException('Request timed out. Please check your connection or try again later.');
    }
    if (error is http.ClientException) {
      return NetworkException('Unable to reach the server. Please check your connection.');
    }
    if (error is FormatException) {
      return JsonParsingException('Failed to process server response.');
    }

    return UnknownException('An unexpected error occurred. Please try again.');
  }

  String? _parseTraceId(String body) {
    try {
      final dynamic jsonBody = json.decode(body);
      if (jsonBody is Map<String, dynamic>) {
        if (jsonBody['extensions'] is Map<String, dynamic>) {
          final Map<String, dynamic> extensions = jsonBody['extensions'] as Map<String, dynamic>;
          if (extensions['traceId'] is String) {
            return extensions['traceId'] as String;
          }
          if (extensions['requestId'] is String) {
            return extensions['requestId'] as String;
          }
        }
        if (jsonBody['traceId'] is String) {
          return jsonBody['traceId'] as String;
        }
        if (jsonBody['requestId'] is String) {
          return jsonBody['requestId'] as String;
        }
      }
    } catch (_) {
      return null;
    }
    return null;
  }

  String? _parseErrorMessage(String body) {
    try {
      final dynamic jsonBody = json.decode(body);
      if (jsonBody is Map<String, dynamic>) {
        if (jsonBody['errors'] is Map<String, dynamic>) {
          final Map<String, dynamic> errors = jsonBody['errors'] as Map<String, dynamic>;
          final Iterable<String> messages = errors.entries.map((MapEntry<String, dynamic> e) {
            final dynamic value = e.value;
            if (value is List<dynamic>) {
              return value.join(', ');
            }
            return value.toString();
          });
          return messages.join('\n');
        }

        return jsonBody['detail'] as String? ?? jsonBody['title'] as String?;
      }
    } catch (_) {
      return null;
    }
    return null;
  }
}
