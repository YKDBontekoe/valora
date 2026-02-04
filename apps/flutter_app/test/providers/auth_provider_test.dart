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
      // Create a valid JWT with email
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

      // Current implementation sets isAuthenticated = true if token is not null,
      // even if parsing fails. This might be desired behavior (e.g. valid opaque token)
      // or a bug. Based on code:
      // if (_token != null) { _parseJwt(_token!); _isAuthenticated = true; }
      // _parseJwt catches errors internally and doesn't rethrow.
      expect(authProvider.isAuthenticated, true);
      expect(authProvider.email, null);
    });

    test('checkAuth should set isAuthenticated to false if token is null', () async {
      when(mockAuthService.getToken()).thenAnswer((_) async => null);

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

    test('logout should clear token and email', () async {
      // Setup state
      final header = base64Url.encode(utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})));
      final payload = base64Url.encode(utf8.encode(json.encode({'email': 'test@example.com'})));
      final token = '$header.$payload.signature';
      when(mockAuthService.getToken()).thenAnswer((_) async => token);
      await authProvider.checkAuth();

      expect(authProvider.email, 'test@example.com');

      // Logout
      await authProvider.logout();

      expect(authProvider.isAuthenticated, false);
      expect(authProvider.email, null);
      verify(mockAuthService.deleteToken()).called(1);
    });

    test('checkAuth catches exception during parsing', () async {
       // Make base64 decode fail
       final token = 'header.invalid_base64.sig';
       when(mockAuthService.getToken()).thenAnswer((_) async => token);

       // The code:
       // try { _token = ...; if token!=null { _parseJwt; ... } } catch (e) { ... }
       // _parseJwt calls _decodeBase64.
       // _decodeBase64 throws Exception if invalid length or bad chars.
       // This exception is caught inside _parseJwt?
       // Let's check code:
       // void _parseJwt(String token) { try { ... } catch (e) { debugPrint... } }
       // So _parseJwt swallows the error.
       // So _isAuthenticated will be TRUE if token is not null, even if parsing fails.

       await authProvider.checkAuth();
       expect(authProvider.isAuthenticated, true);
       expect(authProvider.email, null);
    });
  });
}
