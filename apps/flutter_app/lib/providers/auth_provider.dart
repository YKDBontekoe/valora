import 'dart:convert';
import 'package:flutter/material.dart';
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
      final result = await _authService.refreshToken();
      if (result.isSuccess && result.token != null) {
        _token = result.token;
        _parseJwt(result.token!);
        _isAuthenticated = true;
        notifyListeners();
        return result.token;
      }
      if (result.isInvalid) {
        _token = null;
        _email = null;
        _isAuthenticated = false;
        notifyListeners();
      }
    } catch (_) {
      // Treat unexpected refresh errors as transient to avoid logging out users.
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
