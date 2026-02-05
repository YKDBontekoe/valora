import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import '../core/exceptions/app_exceptions.dart';
import 'api_service.dart';

class AuthService {
  final FlutterSecureStorage _storage;
  final http.Client _client;
  static const String _tokenKey = 'auth_token';
  static const String _refreshTokenKey = 'refresh_token';

  String get baseUrl => ApiService.baseUrl;

  AuthService({FlutterSecureStorage? storage, http.Client? client})
      : _storage = storage ?? const FlutterSecureStorage(),
        _client = client ?? http.Client();

  Future<String?> getToken() async {
    return await _storage.read(key: _tokenKey);
  }

  Future<void> saveToken(String token) async {
    await _storage.write(key: _tokenKey, value: token);
  }

  Future<void> deleteToken() async {
    await _storage.delete(key: _tokenKey);
    await _storage.delete(key: _refreshTokenKey);
  }

  Future<String?> refreshToken() async {
    final refreshToken = await _storage.read(key: _refreshTokenKey);
    if (refreshToken == null) {
      throw RefreshTokenInvalidException('No refresh token available');
    }

    try {
      final response = await _client.post(
        Uri.parse('$baseUrl/auth/refresh'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'refreshToken': refreshToken}),
      ).timeout(ApiService.timeoutDuration);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final newToken = data['token'];
        final newRefreshToken = data['refreshToken'];
        if (newToken != null) {
          await saveToken(newToken);
          if (newRefreshToken != null) {
            await _storage.write(key: _refreshTokenKey, value: newRefreshToken);
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
      final response = await _client.post(
        Uri.parse('$baseUrl/auth/login'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'email': email, 'password': password}),
      ).timeout(ApiService.timeoutDuration);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['token'] != null) {
          await saveToken(data['token']);
        }
        if (data['refreshToken'] != null) {
          await _storage.write(key: _refreshTokenKey, value: data['refreshToken']);
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

  Future<void> register(String email, String password, String confirmPassword) async {
    try {
      final response = await _client.post(
        Uri.parse('$baseUrl/auth/register'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'email': email,
          'password': password,
          'confirmPassword': confirmPassword,
        }),
      ).timeout(ApiService.timeoutDuration);

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
              body.map((e) => e['description'] ?? e.toString()).join('\n'));
        }

        // Validation dictionary: { "errors": { "Email": ["Required"] } }
        if (body is Map && body.containsKey('errors')) {
          final errors = body['errors'];
          if (errors is Map) {
            return ValidationException(errors.values
                .map((v) => (v is List) ? v.join(', ') : v.toString())
                .join('\n'));
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
