import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;
import 'package:retry/retry.dart';

import '../../../core/config/app_config.dart';
import 'http_exception_mapper.dart';

typedef AuthTokenReader = String? Function();

class HttpTransport {
  HttpTransport({
    required http.Client client,
    required AuthTokenReader authTokenReader,
    Future<String?> Function()? refreshToken,
    RetryOptions? retryOptions,
    HttpExceptionMapper? exceptionMapper,
  })  : _client = client,
        _authTokenReader = authTokenReader,
        _refreshToken = refreshToken,
        _retryOptions = retryOptions ?? const RetryOptions(maxAttempts: 3),
        _exceptionMapper = exceptionMapper ?? const HttpExceptionMapper();

  final http.Client _client;
  final AuthTokenReader _authTokenReader;
  final Future<String?> Function()? _refreshToken;
  final RetryOptions _retryOptions;
  final HttpExceptionMapper _exceptionMapper;

  static const Duration defaultTimeout = Duration(seconds: 15);

  String get baseUrl => AppConfig.apiUrl;

  Future<T> get<T>({
    required Uri uri,
    required T Function(http.Response response) responseHandler,
    Duration timeout = defaultTimeout,
    bool retryOnNetworkError = false,
  }) {
    return _execute(
      uri: uri,
      timeout: timeout,
      retryOnNetworkError: retryOnNetworkError,
      requestBuilder: (Map<String, String> headers) => _client.get(uri, headers: headers),
      responseHandler: responseHandler,
    );
  }

  Future<T> post<T>({
    required Uri uri,
    Object? body,
    required T Function(http.Response response) responseHandler,
    Duration timeout = defaultTimeout,
    bool retryOnNetworkError = false,
  }) {
    return _execute(
      uri: uri,
      timeout: timeout,
      retryOnNetworkError: retryOnNetworkError,
      requestBuilder: (Map<String, String> headers) => _client.post(
        uri,
        headers: headers,
        body: body is String ? body : json.encode(body),
      ),
      responseHandler: responseHandler,
    );
  }

  Future<T> delete<T>({
    required Uri uri,
    required T Function(http.Response response) responseHandler,
    Duration timeout = defaultTimeout,
    bool retryOnNetworkError = false,
  }) {
    return _execute(
      uri: uri,
      timeout: timeout,
      retryOnNetworkError: retryOnNetworkError,
      requestBuilder: (Map<String, String> headers) => _client.delete(uri, headers: headers),
      responseHandler: responseHandler,
    );
  }

  Exception mapException(dynamic error, StackTrace stack, [Uri? uri]) {
    return _exceptionMapper.mapException(error, stack, uri);
  }

  T parseOrThrow<T>(http.Response response, T Function(String body) parser) {
    return _exceptionMapper.parseOrThrow(response, parser);
  }

  Future<T> _execute<T>({
    required Uri uri,
    required Future<http.Response> Function(Map<String, String> headers) requestBuilder,
    required T Function(http.Response response) responseHandler,
    required Duration timeout,
    required bool retryOnNetworkError,
  }) async {
    try {
      Future<http.Response> request() => _authenticatedRequest(requestBuilder).timeout(timeout);

      final http.Response response = retryOnNetworkError
          ? await _retryOptions.retry(
              request,
              retryIf: (dynamic e) => e is SocketException || e is TimeoutException,
            )
          : await request();

      return responseHandler(response);
    } catch (error, stack) {
      throw _exceptionMapper.mapException(error, stack, uri);
    }
  }

  Future<http.Response> _authenticatedRequest(
    Future<http.Response> Function(Map<String, String> headers) request,
  ) async {
    final Map<String, String> headers = <String, String>{
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      if (_authTokenReader() != null) 'Authorization': 'Bearer ${_authTokenReader()}',
    };

    http.Response response = await request(headers);

    if (response.statusCode == 401 && _refreshToken != null) {
      final String? refreshedToken = await _refreshToken();
      if (refreshedToken != null) {
        headers['Authorization'] = 'Bearer $refreshedToken';
        response = await request(headers);
      }
    }

    return response;
  }
}
