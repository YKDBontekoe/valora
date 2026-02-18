import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'package:logging/logging.dart';
import 'package:retry/retry.dart';
import '../core/exceptions/app_exceptions.dart';
import 'api_service.dart';

class AuthService {
  static final _log = Logger('AuthService');
  final FlutterSecureStorage _storage;
  final http.Client _client;
  static const String _tokenKey = 'auth_token';
  static const String _refreshTokenKey = 'refresh_token';

  final RetryOptions _retryOptions = const RetryOptions(
    maxAttempts: 3,
    delayFactor: Duration(seconds: 1),
  );

  String get baseUrl => ApiService.baseUrl;

  AuthService({FlutterSecureStorage? storage, http.Client? client})
    : _storage =
          storage ??
          const FlutterSecureStorage(
            iOptions: IOSOptions(
              accessibility: KeychainAccessibility.first_unlock,
            ),
            // encryptedSharedPreferences is deprecated and ignored in newer versions
            aOptions: AndroidOptions(),
          ),
      _client = client ?? http.Client();

  Future<String?> getToken() async {
    try {
      return await _storage.read(key: _tokenKey);
    } catch (e) {
      _log.warning('SecureStorage read failed', e);
      throw StorageException('Failed to read auth token: $e');
    }
  }

  Future<void> saveToken(String token) async {
    try {
      await _storage.write(key: _tokenKey, value: token);
    } catch (e) {
      _log.warning('SecureStorage write failed', e);
      throw StorageException('Failed to save auth token: $e');
    }
  }

  Future<void> deleteToken() async {
    try {
      await _storage.delete(key: _tokenKey);
      await _storage.delete(key: _refreshTokenKey);
    } catch (e) {
      _log.warning('SecureStorage delete failed', e);
      throw StorageException('Failed to clear auth data: $e');
    }
  }

  Future<String?> refreshToken() async {
    String? refreshToken;
    try {
      refreshToken = await _storage.read(key: _refreshTokenKey);
    } catch (e) {
      _log.warning('SecureStorage read (refresh) failed', e);
      throw RefreshTokenInvalidException('No refresh token available');
    }

    if (refreshToken == null) {
      throw RefreshTokenInvalidException('No refresh token available');
    }

    try {
      final response = await _retryOptions.retry(
        () async {
          final res = await _client
              .post(
                Uri.parse('$baseUrl/auth/refresh'),
                headers: {'Content-Type': 'application/json'},
                body: jsonEncode({'refreshToken': refreshToken}),
              )
              .timeout(ApiService.timeoutDuration);

          if (res.statusCode >= 500) {
            throw ServerException('Server error (${res.statusCode})');
          }
          return res;
        },
        retryIf: (e) =>
            e is SocketException ||
            e is TimeoutException ||
            e is http.ClientException ||
            e is ServerException,
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final newToken = data['token'];
        final newRefreshToken = data['refreshToken'];
        if (newToken != null) {
          await saveToken(newToken);
          if (newRefreshToken != null) {
             try {
               await _storage.write(key: _refreshTokenKey, value: newRefreshToken);
             } catch (e) {
               _log.warning('Failed to update refresh token', e);
             }
          }
          return newToken;
        }
        throw JsonParsingException('Missing token in refresh response');
      }
      if (response.statusCode == 400 || response.statusCode == 401) {
        throw RefreshTokenInvalidException('Refresh token rejected');
      }
      throw ServerException('Refresh failed: ${response.statusCode}');
    } on AppException {
      rethrow;
    } on TimeoutException catch (e) {
      throw NetworkException('Refresh timed out: $e');
    } on SocketException catch (e) {
      throw NetworkException('Refresh failed: $e');
    } on http.ClientException catch (e) {
      throw NetworkException('Refresh failed: $e');
    } on FormatException catch (e) {
      throw JsonParsingException('Failed to parse refresh response: $e');
    } catch (e) {
      throw UnknownException('Refresh failed: $e');
    }
  }

  Future<Map<String, dynamic>> login(String email, String password) async {
    try {
      final response = await _retryOptions.retry(
        () async {
          final res = await _client
              .post(
                Uri.parse('$baseUrl/auth/login'),
                headers: {'Content-Type': 'application/json'},
                body: jsonEncode({'email': email, 'password': password}),
              )
              .timeout(ApiService.timeoutDuration);

          if (res.statusCode >= 500) {
            throw ServerException('Server error (${res.statusCode})');
          }
          return res;
        },
        retryIf: (e) =>
            e is SocketException ||
            e is TimeoutException ||
            e is http.ClientException ||
            e is ServerException,
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['token'] != null) {
          await saveToken(data['token']);
        }
        if (data['refreshToken'] != null) {
          try {
            await _storage.write(
              key: _refreshTokenKey,
              value: data['refreshToken'],
            );
          } catch (e) {
             _log.warning('SecureStorage write refresh failed', e);
          }
        }
        return data;
      } else {
        throw _handleError(response);
      }
    } catch (e) {
      if (e is AppException) rethrow;
      throw NetworkException('Login failed: $e');
    }
  }

  Future<Map<String, dynamic>> externalLogin(String provider, String idToken) async {
    try {
      final response = await _retryOptions.retry(
        () async {
          final res = await _client
              .post(
                Uri.parse('$baseUrl/auth/external-login'),
                headers: {'Content-Type': 'application/json'},
                body: jsonEncode({'provider': provider, 'idToken': idToken}),
              )
              .timeout(ApiService.timeoutDuration);

          if (res.statusCode >= 500) {
            throw ServerException('Server error (${res.statusCode})');
          }
          return res;
        },
        retryIf: (e) =>
            e is SocketException ||
            e is TimeoutException ||
            e is http.ClientException ||
            e is ServerException,
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['token'] != null) {
          await saveToken(data['token']);
        }
        if (data['refreshToken'] != null) {
          try {
            await _storage.write(
              key: _refreshTokenKey,
              value: data['refreshToken'],
            );
          } catch (e) {
             _log.warning('SecureStorage write refresh failed', e);
          }
        }
        return data;
      } else {
        throw _handleError(response);
      }
    } catch (e) {
      if (e is AppException) rethrow;
      throw NetworkException('External login failed: $e');
    }
  }

  Future<void> register(
    String email,
    String password,
    String confirmPassword,
  ) async {
    try {
      final response = await _retryOptions.retry(
        () async {
          final res = await _client
              .post(
                Uri.parse('$baseUrl/auth/register'),
                headers: {'Content-Type': 'application/json'},
                body: jsonEncode({
                  'email': email,
                  'password': password,
                  'confirmPassword': confirmPassword,
                }),
              )
              .timeout(ApiService.timeoutDuration);

          if (res.statusCode >= 500) {
            throw ServerException('Server error (${res.statusCode})');
          }
          return res;
        },
        retryIf: (e) =>
            e is SocketException ||
            e is TimeoutException ||
            e is http.ClientException ||
            e is ServerException,
      );

      if (response.statusCode != 200) {
        throw _handleError(response);
      }
    } catch (e) {
      if (e is AppException) rethrow;
      throw NetworkException('Registration failed: $e');
    }
  }

  Exception _handleError(http.Response response) {
    if (response.statusCode == 400) {
      // Try parsing error message from body
      try {
        final body = jsonDecode(response.body);
        // Identity errors are often array of objects with Code and Description
        if (body is List) {
          return ValidationException(
            body.map((e) => e['description'] ?? e.toString()).join('\n'),
          );
        }

        // Validation dictionary: { "errors": { "Email": ["Required"] } }
        if (body is Map && body.containsKey('errors')) {
          final errors = body['errors'];
          if (errors is Map) {
            return ValidationException(
              errors.values
                  .map((v) => (v is List) ? v.join(', ') : v.toString())
                  .join('\n'),
            );
          }
        }

        // Generic problem details
        if (body is Map &&
            (body.containsKey('detail') || body.containsKey('title'))) {
          return ValidationException(body['detail'] ?? body['title']);
        }

        return ValidationException('Invalid input');
      } catch (_) {
        return ValidationException('Invalid input');
      }
    } else if (response.statusCode == 401) {
      return ValidationException('Invalid email or password');
    }
    return ServerException('Auth failed: ${response.statusCode}');
  }
}
