import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/services/auth_service.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';

// Generate Mocks
@GenerateMocks([FlutterSecureStorage, http.Client])
import 'auth_service_test.mocks.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUpAll(() async {
    await dotenv.load(fileName: ".env.example");
  });

  group('AuthService', () {
    late MockFlutterSecureStorage mockStorage;
    late MockClient mockClient;
    late AuthService authService;

    setUp(() {
      mockStorage = MockFlutterSecureStorage();
      mockClient = MockClient();
      authService = AuthService(storage: mockStorage, client: mockClient);
    });

    test('login saves token and refresh token on success', () async {
      final mockResponse = {
        'token': 'access_token',
        'refreshToken': 'refresh_token',
        'email': 'test@test.com',
        'userId': '123'
      };

      when(mockClient.post(
        any,
        headers: anyNamed('headers'),
        body: anyNamed('body'),
      )).thenAnswer((_) async => http.Response(jsonEncode(mockResponse), 200));

      // Mock storage write (return void)
      when(mockStorage.write(key: anyNamed('key'), value: anyNamed('value')))
          .thenAnswer((_) async => {});

      final result = await authService.login('test@test.com', 'password');

      verify(mockStorage.write(key: 'auth_token', value: 'access_token')).called(1);
      verify(mockStorage.write(key: 'refresh_token', value: 'refresh_token')).called(1);
      expect(result['token'], 'access_token');
    });

    test('refreshToken returns new token on success', () async {
      final mockResponse = {
        'token': 'new_access_token',
        'refreshToken': 'refresh_token',
        'email': 'test@test.com',
        'userId': '123'
      };

      when(mockStorage.read(key: 'refresh_token'))
          .thenAnswer((_) async => 'valid_refresh_token');

      when(mockClient.post(
        any,
        headers: anyNamed('headers'),
        body: anyNamed('body'),
      )).thenAnswer((_) async => http.Response(jsonEncode(mockResponse), 200));

       when(mockStorage.write(key: anyNamed('key'), value: anyNamed('value')))
          .thenAnswer((_) async => {});

      final result = await authService.refreshToken();

      expect(result, 'new_access_token');
      verify(mockStorage.write(key: 'auth_token', value: 'new_access_token')).called(1);
    });

    test('refreshToken returns null if no refresh token stored', () async {
      when(mockStorage.read(key: 'refresh_token'))
          .thenAnswer((_) async => null);

      final result = await authService.refreshToken();

      expect(result, isNull);
      verifyNever(mockClient.post(any, headers: anyNamed('headers'), body: anyNamed('body')));
    });

    test('refreshToken returns null on API failure', () async {
      when(mockStorage.read(key: 'refresh_token'))
          .thenAnswer((_) async => 'valid_refresh_token');

      when(mockClient.post(
        any,
        headers: anyNamed('headers'),
        body: anyNamed('body'),
      )).thenAnswer((_) async => http.Response('Unauthorized', 401));

      final result = await authService.refreshToken();

      expect(result, isNull);
    });

    test('deleteToken removes both tokens', () async {
      when(mockStorage.delete(key: anyNamed('key')))
          .thenAnswer((_) async => {});

      await authService.deleteToken();

      verify(mockStorage.delete(key: 'auth_token')).called(1);
      verify(mockStorage.delete(key: 'refresh_token')).called(1);
    });
  });
}
