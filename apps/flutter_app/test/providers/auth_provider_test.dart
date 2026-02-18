import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_sign_in/google_sign_in.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/providers/auth_provider.dart';
import 'package:valora_app/services/auth_service.dart';

@GenerateMocks([AuthService, GoogleSignIn, GoogleSignInAccount, GoogleSignInAuthentication])
import 'auth_provider_test.mocks.dart';

void main() {
  late MockAuthService mockAuthService;
  late MockGoogleSignIn mockGoogleSignIn;
  late AuthProvider authProvider;

  setUp(() {
    mockAuthService = MockAuthService();
    mockGoogleSignIn = MockGoogleSignIn();
    authProvider = AuthProvider(
      authService: mockAuthService,
      googleSignIn: mockGoogleSignIn,
    );
  });

  group('AuthProvider', () {
    test('checkAuth should set email from valid token', () async {
      final header = base64Url.encode(
        utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})),
      );
      final payload = base64Url.encode(
        utf8.encode(json.encode({'email': 'test@example.com', 'sub': '123'})),
      );
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

    test(
      'checkAuth should set isAuthenticated to false if token is null',
      () async {
        when(mockAuthService.getToken()).thenAnswer((_) async => null);

        await authProvider.checkAuth();

        expect(authProvider.isAuthenticated, false);
        expect(authProvider.email, null);
      },
    );

    test('checkAuth catches exception from authService', () async {
      when(mockAuthService.getToken()).thenThrow(Exception('Storage error'));

      await authProvider.checkAuth();

      expect(authProvider.isAuthenticated, false);
      expect(authProvider.email, null);
    });

    test('login should set email from returned token', () async {
      final header = base64Url.encode(
        utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})),
      );
      final payload = base64Url.encode(
        utf8.encode(json.encode({'email': 'login@example.com'})),
      );
      final token = '$header.$payload.signature';

      when(
        mockAuthService.login('login@example.com', 'password'),
      ).thenAnswer((_) async => {'token': token});

      await authProvider.login('login@example.com', 'password');

      expect(authProvider.isAuthenticated, true);
      expect(authProvider.email, 'login@example.com');
    });

    test('login propagates exception', () async {
      when(
        mockAuthService.login(any, any),
      ).thenThrow(Exception('Login failed'));

      expect(
        () => authProvider.login('test@example.com', 'password'),
        throwsException,
      );
    });

    test('register calls authService register', () async {
      when(mockAuthService.register(any, any, any)).thenAnswer((_) async {});

      await authProvider.register('new@example.com', 'password', 'password');

      verify(
        mockAuthService.register('new@example.com', 'password', 'password'),
      ).called(1);
    });

    test('register propagates exception', () async {
      when(
        mockAuthService.register(any, any, any),
      ).thenThrow(Exception('Registration failed'));

      expect(
        () => authProvider.register('new@example.com', 'pass', 'pass'),
        throwsException,
      );
    });

    test('logout should clear token and email', () async {
      final header = base64Url.encode(
        utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})),
      );
      final payload = base64Url.encode(
        utf8.encode(json.encode({'email': 'test@example.com'})),
      );
      final token = '$header.$payload.signature';
      when(mockAuthService.getToken()).thenAnswer((_) async => token);
      await authProvider.checkAuth();

      expect(authProvider.email, 'test@example.com');

      await authProvider.logout();

      expect(authProvider.isAuthenticated, false);
      expect(authProvider.email, null);
      verify(mockAuthService.deleteToken()).called(1);
    });

    test('refreshSession should update token and email on success', () async {
      final header = base64Url.encode(
        utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})),
      );
      final payload = base64Url.encode(
        utf8.encode(json.encode({'email': 'refresh@example.com'})),
      );
      final token = '$header.$payload.signature';

      when(mockAuthService.refreshToken()).thenAnswer((_) async => token);

      final result = await authProvider.refreshSession();

      expect(result, token);
      expect(authProvider.isAuthenticated, true);
      expect(authProvider.email, 'refresh@example.com');
    });

    test(
      'refreshSession should clear state on invalid refresh token',
      () async {
        final header = base64Url.encode(
          utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})),
        );
        final payload = base64Url.encode(
          utf8.encode(json.encode({'email': 'stale@example.com'})),
        );
        final token = '$header.$payload.signature';

        when(mockAuthService.getToken()).thenAnswer((_) async => token);
        when(mockAuthService.deleteToken()).thenAnswer((_) async {});

        await authProvider.checkAuth();
        expect(authProvider.isAuthenticated, true);
        expect(authProvider.email, 'stale@example.com');

        when(
          mockAuthService.refreshToken(),
        ).thenThrow(RefreshTokenInvalidException('Invalid refresh'));

        final result = await authProvider.refreshSession();

        expect(result, isNull);
        expect(authProvider.isAuthenticated, false);
        expect(authProvider.email, null);
        verify(mockAuthService.deleteToken()).called(1);
      },
    );

    test('refreshSession should keep state on transient failure', () async {
      final header = base64Url.encode(
        utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})),
      );
      final payload = base64Url.encode(
        utf8.encode(json.encode({'email': 'keep@example.com'})),
      );
      final token = '$header.$payload.signature';

      when(mockAuthService.getToken()).thenAnswer((_) async => token);
      await authProvider.checkAuth();

      when(
        mockAuthService.refreshToken(),
      ).thenThrow(NetworkException('Timeout'));

      final result = await authProvider.refreshSession();

      expect(result, isNull);
      expect(authProvider.isAuthenticated, true);
      expect(authProvider.email, 'keep@example.com');
      verifyNever(mockAuthService.deleteToken());
    });

    test('checkAuth catches exception during parsing', () async {
      final token = 'header.invalid_base64.sig';
      when(mockAuthService.getToken()).thenAnswer((_) async => token);

      await authProvider.checkAuth();
      expect(authProvider.isAuthenticated, true);
      expect(authProvider.email, null);
    });

    test('refreshSession should log severe and keep state on unexpected failure', () async {
      final header = base64Url.encode(
        utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})),
      );
      final payload = base64Url.encode(
        utf8.encode(json.encode({'email': 'unexpected@example.com'})),
      );
      final token = '$header.$payload.signature';

      when(mockAuthService.getToken()).thenAnswer((_) async => token);
      await authProvider.checkAuth();

      when(
        mockAuthService.refreshToken(),
      ).thenThrow(Exception('Unexpected error'));

      final result = await authProvider.refreshSession();

      expect(result, isNull);
      expect(authProvider.isAuthenticated, true);
      expect(authProvider.email, 'unexpected@example.com');
      verifyNever(mockAuthService.deleteToken());
    });

    test('checkAuth handles JWT parsing error gracefully', () async {
      final header = base64Url.encode(utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})));
      final invalidPayload = base64Url.encode(utf8.encode('this is not json'));
      final token = '$header.$invalidPayload.signature';

      when(mockAuthService.getToken()).thenAnswer((_) async => token);

      await authProvider.checkAuth();

      expect(authProvider.isAuthenticated, true);
      expect(authProvider.email, null);
    });

    test('loginWithGoogle success path', () async {
      final mockAccount = MockGoogleSignInAccount();
      final mockAuth = MockGoogleSignInAuthentication();
      final header = base64Url.encode(utf8.encode(json.encode({'typ': 'JWT', 'alg': 'HS256'})));
      final payload = base64Url.encode(utf8.encode(json.encode({'email': 'google@example.com'})));
      final token = '$header.$payload.signature';

      when(mockGoogleSignIn.initialize()).thenAnswer((_) async {});
      when(mockGoogleSignIn.authenticate(scopeHint: anyNamed('scopeHint')))
          .thenAnswer((_) async => mockAccount);
      when(mockAccount.authentication).thenReturn(mockAuth);
      when(mockAuth.idToken).thenReturn('google_id_token');

      when(mockAuthService.externalLogin('google', 'google_id_token'))
          .thenAnswer((_) async => {'token': token});

      await authProvider.loginWithGoogle();

      expect(authProvider.isAuthenticated, true);
      expect(authProvider.email, 'google@example.com');
      verify(mockGoogleSignIn.initialize()).called(1);
      verify(mockGoogleSignIn.authenticate(scopeHint: ['email'])).called(1);
      verify(mockAuthService.externalLogin('google', 'google_id_token')).called(1);
    });

    test('loginWithGoogle handles missing idToken', () async {
      final mockAccount = MockGoogleSignInAccount();
      final mockAuth = MockGoogleSignInAuthentication();

      when(mockGoogleSignIn.initialize()).thenAnswer((_) async {});
      when(mockGoogleSignIn.authenticate(scopeHint: anyNamed('scopeHint')))
          .thenAnswer((_) async => mockAccount);
      when(mockAccount.authentication).thenReturn(mockAuth);
      when(mockAuth.idToken).thenReturn(null);

      expect(() => authProvider.loginWithGoogle(), throwsException);
      expect(authProvider.isAuthenticated, false);
    });

    test('loginWithGoogle failure propagates exception', () async {
      when(mockGoogleSignIn.initialize()).thenThrow(Exception('Init failed'));
      expect(() => authProvider.loginWithGoogle(), throwsException);
    });
  });
}
