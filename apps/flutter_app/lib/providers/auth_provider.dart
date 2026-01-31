import 'package:flutter/material.dart';
import '../services/auth_service.dart';

class AuthProvider extends ChangeNotifier {
  final AuthService _authService;
  bool _isAuthenticated = false;
  bool _isLoading = true;
  String? _token;

  bool get isAuthenticated => _isAuthenticated;
  bool get isLoading => _isLoading;
  String? get token => _token;

  AuthProvider({AuthService? authService})
      : _authService = authService ?? AuthService();

  Future<void> checkAuth() async {
    _isLoading = true;
    notifyListeners();
    try {
      _token = await _authService.getToken();
      _isAuthenticated = _token != null;
    } catch (e) {
      _isAuthenticated = false;
      _token = null;
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
      _isAuthenticated = true;
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
    _isAuthenticated = false;
    notifyListeners();
  }
}
