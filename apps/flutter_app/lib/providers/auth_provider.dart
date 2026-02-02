import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../models/user_model.dart';

class AuthProvider extends ChangeNotifier {
  final AuthService _authService;
  bool _isAuthenticated = false;
  bool _isLoading = true;
  String? _token;
  User? _user;

  bool get isAuthenticated => _isAuthenticated;
  bool get isLoading => _isLoading;
  String? get token => _token;
  User? get user => _user;

  AuthProvider({AuthService? authService})
      : _authService = authService ?? AuthService();

  Future<void> checkAuth() async {
    _isLoading = true;
    notifyListeners();
    try {
      _token = await _authService.getToken();
      if (_token != null) {
          final data = await _authService.refreshToken();
          if (data != null) {
              _token = data['token'];
              _user = User.fromJson(data);
              _isAuthenticated = true;
          } else {
              _isAuthenticated = true;
          }
      } else {
          _isAuthenticated = false;
      }
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
      _user = User.fromJson(data);
      _isAuthenticated = true;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> register(String email, String password, String confirmPassword, List<String> preferredCities) async {
    _isLoading = true;
    notifyListeners();
    try {
      await _authService.register(email, password, confirmPassword, preferredCities);
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> updateProfile(List<String> preferredCities) async {
    _isLoading = true;
    notifyListeners();
    try {
      await _authService.updateProfile(preferredCities);
      if (_user != null) {
        _user = User(
          id: _user!.id,
          email: _user!.email,
          preferredCities: preferredCities,
        );
      }
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> logout() async {
    await _authService.deleteToken();
    _token = null;
    _user = null;
    _isAuthenticated = false;
    notifyListeners();
  }
}
