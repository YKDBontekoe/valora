import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/models/user_profile.dart';

void main() {
  group('UserProfile', () {
    test('displayName returns full name when both exist', () {
      final user = UserProfile(
        email: 'test@example.com',
        firstName: 'John',
        lastName: 'Doe',
        defaultRadiusMeters: 1000,
        biometricsEnabled: false,
      );
      expect(user.displayName, 'John Doe');
    });

    test('displayName returns firstName when lastName is missing', () {
      final user = UserProfile(
        email: 'test@example.com',
        firstName: 'John',
        defaultRadiusMeters: 1000,
        biometricsEnabled: false,
      );
      expect(user.displayName, 'John');
    });

    test('displayName returns email when name is missing', () {
      final user = UserProfile(
        email: 'test@example.com',
        defaultRadiusMeters: 1000,
        biometricsEnabled: false,
      );
      expect(user.displayName, 'test@example.com');
    });

    test('initials returns JD for John Doe', () {
      final user = UserProfile(
        email: 'test@example.com',
        firstName: 'John',
        lastName: 'Doe',
        defaultRadiusMeters: 1000,
        biometricsEnabled: false,
      );
      expect(user.initials, 'JD');
    });

    test('fromJson and toJson are consistent', () {
      final json = {
        'email': 'test@example.com',
        'firstName': 'John',
        'lastName': 'Doe',
        'defaultRadiusMeters': 2000,
        'biometricsEnabled': true,
      };
      final user = UserProfile.fromJson(json);
      expect(user.email, 'test@example.com');
      expect(user.defaultRadiusMeters, 2000);
      expect(user.biometricsEnabled, true);
      expect(user.toJson(), json);
    });
  });
}
