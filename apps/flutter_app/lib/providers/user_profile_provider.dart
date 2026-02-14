import 'package:flutter/foundation.dart';
import 'package:local_auth/local_auth.dart';
import '../models/user_profile.dart';
import '../services/api_service.dart';

class UserProfileProvider extends ChangeNotifier {
  final ApiService _apiService;
  final LocalAuthentication _localAuth = LocalAuthentication();

  UserProfile? _profile;
  bool _isLoading = false;
  String? _error;

  UserProfile? get profile => _profile;
  bool get isLoading => _isLoading;
  String? get error => _error;

  UserProfileProvider({required ApiService apiService}) : _apiService = apiService;

  Future<void> fetchProfile() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _profile = await _apiService.getUserProfile();
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> updateProfile({
    String? firstName,
    String? lastName,
    int? defaultRadiusMeters,
    bool? biometricsEnabled,
  }) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      await _apiService.updateProfile(
        firstName: firstName ?? _profile?.firstName,
        lastName: lastName ?? _profile?.lastName,
        defaultRadiusMeters: defaultRadiusMeters ?? _profile?.defaultRadiusMeters ?? 1000,
        biometricsEnabled: biometricsEnabled ?? _profile?.biometricsEnabled ?? false,
      );
      await fetchProfile();
      return true;
    } catch (e) {
      _error = e.toString();
      notifyListeners();
      return false;
    } finally {
      _isLoading = false;
    }
  }

  Future<bool> changePassword(String current, String next, String confirm) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      await _apiService.changePassword(current, next, confirm);
      return true;
    } catch (e) {
      _error = e.toString();
      notifyListeners();
      return false;
    } finally {
      _isLoading = false;
    }
  }

  Future<bool> checkBiometrics() async {
    try {
      return await _localAuth.canCheckBiometrics;
    } catch (e) {
      return false;
    }
  }

  Future<bool> authenticate() async {
    try {
      return await _localAuth.authenticate(
        localizedReason: 'Please authenticate to change security settings',
        options: const AuthenticationOptions(
          stickyAuth: true,
        ),
      );
    } catch (e) {
      return false;
    }
  }
}
