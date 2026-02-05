import 'dart:convert';
import 'dart:developer' as developer;
import 'package:flutter/material.dart';
import '../core/exceptions/app_exceptions.dart';
import '../services/auth_service.dart';

class AuthProvider extends ChangeNotifier {
  final AuthService _authService;
  bool _isAuthenticated = false;
  bool _isLoading = true;
  String? _token;
  String? _email;

  bool get isAuthenticated => _isAuthenticated;
  bool get isLoading => _isLoading;
  String? get token => _token;
  String? get email => _email;

  AuthProvider({AuthService? authService})
      : _authService = authService ?? AuthService();

  Future<void> checkAuth() async {
    _isLoading = true;
    notifyListeners();
    try {
      _token = await _authService.getToken();
      if (_token != null) {
        _parseJwt(_token!);
        _isAuthenticated = true;
      } else {
        _isAuthenticated = false;
      }
    } catch (e) {
      _isAuthenticated = false;
      _token = null;
      _email = null;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> login(String email, String password) async {
    _isLoading = true;
    notifyListeners();
    try {
      final data = await _authService.login(email, password);
      _token = data['token'];
      if (_token != null) {
        _parseJwt(_token!);
        _isAuthenticated = true;
      }
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> register(String email, String password, String confirmPassword) async {
    _isLoading = true;
    notifyListeners();
    try {
      await _authService.register(email, password, confirmPassword);
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> logout() async {
    await _authService.deleteToken();
    _token = null;
    _email = null;
    _isAuthenticated = false;
    notifyListeners();
  }

  Future<String?> refreshSession() async {
    try {
      final newToken = await _authService.refreshToken();
      if (newToken != null) {
        _token = newToken;
        _parseJwt(newToken);
        _isAuthenticated = true;
        notifyListeners();
        return newToken;
      }
    } on RefreshTokenInvalidException catch (e, stackTrace) {
      developer.log(
        'Refresh token invalid. Clearing auth state.',
        name: 'AuthProvider',
        error: e,
        stackTrace: stackTrace,
      );
      await logout();
      return null;
    } on AppException catch (e, stackTrace) {
      developer.log(
        'Refresh token failed (transient). Keeping auth state.',
        name: 'AuthProvider',
        error: e,
        stackTrace: stackTrace,
      );
      return null;
    } catch (e, stackTrace) {
      developer.log(
        'Refresh token failed (unexpected). Keeping auth state.',
        name: 'AuthProvider',
        error: e,
        stackTrace: stackTrace,
      );
      return null;
    }

    return null;
  }

  void _parseJwt(String token) {
    try {
      final parts = token.split('.');
      if (parts.length != 3) return;

      final payload = _decodeBase64(parts[1]);
      final payloadMap = json.decode(payload);

      // Try standard claims for email/username
      if (payloadMap is Map<String, dynamic>) {
        _email = payloadMap['email'] ?? payloadMap['unique_name'] ?? payloadMap['sub'];
      }
    } catch (e) {
      debugPrint('Error parsing JWT: $e');
    }
  }

  String _decodeBase64(String str) {
    String output = str.replaceAll('-', '+').replaceAll('_', '/');
    switch (output.length % 4) {
      case 0: break;
      case 2: output += '=='; break;
      case 3: output += '='; break;
      default: throw Exception('Illegal base64url string!"');
    }
    return utf8.decode(base64.decode(output));
  }
}
