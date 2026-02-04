import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/services/auth_service.dart';

@GenerateMocks([AuthService])
import 'auth_provider_test.mocks.dart';

void main() {
  late MockAuthService mockAuthService;
  late AuthProvider authProvider;

  setUp(() {
    mockAuthService = MockAuthService();
    authProvider = AuthProvider(authService: mockAuthService);
  });

  group('AuthProvider', () {
    test('checkAuth should set email from valid token', () async {
      final header = base64Url.encode(utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})));
      final payload = base64Url.encode(utf8.encode(json.encode({'email': 'test@example.com', 'sub': '123'})));
      final token = '$header.$payload.signature';

      when(mockAuthService.getToken()).thenAnswer((_) async => token);

      await authProvider.checkAuth();

      expect(authProvider.isAuthenticated, true);
      expect(authProvider.email, 'test@example.com');
    });

    test('checkAuth should handle invalid token gracefully', () async {
      when(mockAuthService.getToken()).thenAnswer((_) async => 'invalid.token');

      await authProvider.checkAuth();

      expect(authProvider.isAuthenticated, true);
      expect(authProvider.email, null);
    });

    test('checkAuth should set isAuthenticated to false if token is null', () async {
      when(mockAuthService.getToken()).thenAnswer((_) async => null);

      await authProvider.checkAuth();

      expect(authProvider.isAuthenticated, false);
      expect(authProvider.email, null);
    });

    test('checkAuth catches exception from authService', () async {
      when(mockAuthService.getToken()).thenThrow(Exception('Storage error'));

      await authProvider.checkAuth();

      expect(authProvider.isAuthenticated, false);
      expect(authProvider.email, null);
    });

    test('login should set email from returned token', () async {
      final header = base64Url.encode(utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})));
      final payload = base64Url.encode(utf8.encode(json.encode({'email': 'login@example.com'})));
      final token = '$header.$payload.signature';

      when(mockAuthService.login('login@example.com', 'password'))
          .thenAnswer((_) async => {'token': token});

      await authProvider.login('login@example.com', 'password');

      expect(authProvider.isAuthenticated, true);
      expect(authProvider.email, 'login@example.com');
    });

    test('login propagates exception', () async {
      when(mockAuthService.login(any, any)).thenThrow(Exception('Login failed'));

      expect(() => authProvider.login('test@example.com', 'password'), throwsException);
    });

    test('register calls authService register', () async {
      when(mockAuthService.register(any, any, any)).thenAnswer((_) async {});

      await authProvider.register('new@example.com', 'password', 'password');

      verify(mockAuthService.register('new@example.com', 'password', 'password')).called(1);
    });

    test('register propagates exception', () async {
       when(mockAuthService.register(any, any, any)).thenThrow(Exception('Registration failed'));

       expect(() => authProvider.register('new@example.com', 'pass', 'pass'), throwsException);
    });

    test('logout should clear token and email', () async {
      final header = base64Url.encode(utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})));
      final payload = base64Url.encode(utf8.encode(json.encode({'email': 'test@example.com'})));
      final token = '$header.$payload.signature';
      when(mockAuthService.getToken()).thenAnswer((_) async => token);
      await authProvider.checkAuth();

      expect(authProvider.email, 'test@example.com');

      await authProvider.logout();

      expect(authProvider.isAuthenticated, false);
      expect(authProvider.email, null);
      verify(mockAuthService.deleteToken()).called(1);
    });

    test('checkAuth catches exception during parsing', () async {
       final token = 'header.invalid_base64.sig';
       when(mockAuthService.getToken()).thenAnswer((_) async => token);

       await authProvider.checkAuth();
       expect(authProvider.isAuthenticated, true);
       expect(authProvider.email, null);
    });

    test('checkAuth sets isAdmin true for simple role', () async {
      final header = base64Url.encode(utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})));
      final payload = base64Url.encode(utf8.encode(json.encode({'email': 'admin@example.com', 'role': 'Admin'})));
      final token = '$header.$payload.signature';

      when(mockAuthService.getToken()).thenAnswer((_) async => token);

      await authProvider.checkAuth();

      expect(authProvider.isAdmin, true);
    });

    test('checkAuth sets isAdmin true for microsoft role claim', () async {
      final header = base64Url.encode(utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})));
      final payload = base64Url.encode(utf8.encode(json.encode({'email': 'admin@example.com', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Admin'})));
      final token = '$header.$payload.signature';

      when(mockAuthService.getToken()).thenAnswer((_) async => token);

      await authProvider.checkAuth();

      expect(authProvider.isAdmin, true);
    });

    test('checkAuth sets isAdmin true for list of roles', () async {
      final header = base64Url.encode(utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})));
      final payload = base64Url.encode(utf8.encode(json.encode({'email': 'admin@example.com', 'role': ['User', 'Admin']})));
      final token = '$header.$payload.signature';

      when(mockAuthService.getToken()).thenAnswer((_) async => token);

      await authProvider.checkAuth();

      expect(authProvider.isAdmin, true);
    });

    test('checkAuth sets isAdmin false for User role', () async {
      final header = base64Url.encode(utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})));
      final payload = base64Url.encode(utf8.encode(json.encode({'email': 'user@example.com', 'role': 'User'})));
      final token = '$header.$payload.signature';

      when(mockAuthService.getToken()).thenAnswer((_) async => token);

      await authProvider.checkAuth();

      expect(authProvider.isAdmin, false);
    });

    test('checkAuth sets isAdmin false on error', () async {
      when(mockAuthService.getToken()).thenThrow(Exception('Fail'));
      await authProvider.checkAuth();
      expect(authProvider.isAdmin, false);
    });

    test('logout resets isAdmin', () async {
      final header = base64Url.encode(utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})));
      final payload = base64Url.encode(utf8.encode(json.encode({'role': 'Admin'})));
      final token = '$header.$payload.signature';

      when(mockAuthService.getToken()).thenAnswer((_) async => token);
      await authProvider.checkAuth();
      expect(authProvider.isAdmin, true);

      await authProvider.logout();
      expect(authProvider.isAdmin, false);
    });
  });
}
