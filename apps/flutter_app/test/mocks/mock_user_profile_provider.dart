import 'package:flutter/foundation.dart';
import 'package:valora_app/models/user_profile.dart';
import 'package:valora_app/providers/user_profile_provider.dart';

class MockUserProfileProvider extends ChangeNotifier implements UserProfileProvider {
  UserProfile? _profile = UserProfile(
    email: 'test@example.com',
    firstName: 'Test',
    lastName: 'User',
    defaultRadiusMeters: 1000,
    biometricsEnabled: false,
  );

  @override
  UserProfile? get profile => _profile;

  @override
  bool get isLoading => false;

  @override
  String? get error => null;

  @override
  Future<void> fetchProfile() async {}

  @override
  Future<bool> updateProfile({
    String? firstName,
    String? lastName,
    int? defaultRadiusMeters,
    bool? biometricsEnabled,
  }) async {
    _profile = UserProfile(
      email: _profile?.email ?? 'test@example.com',
      firstName: firstName ?? _profile?.firstName,
      lastName: lastName ?? _profile?.lastName,
      defaultRadiusMeters: defaultRadiusMeters ?? _profile?.defaultRadiusMeters ?? 1000,
      biometricsEnabled: biometricsEnabled ?? _profile?.biometricsEnabled ?? false,
    );
    notifyListeners();
    return true;
  }

  @override
  Future<bool> changePassword(String current, String next, String confirm) async => true;

  @override
  Future<bool> checkBiometrics() async => false;

  @override
  Future<bool> authenticate() async => true;
}
