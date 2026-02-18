import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/services/auth_service.dart';

@GenerateMocks([FlutterSecureStorage, http.Client])
import 'auth_service_coverage_test.mocks.dart';

void main() {
  late MockFlutterSecureStorage mockStorage;
  late MockClient mockClient;
  late AuthService authService;

  setUp(() {
    mockStorage = MockFlutterSecureStorage();
    mockClient = MockClient();
    authService = AuthService(storage: mockStorage, client: mockClient);
  });

  test('getToken handles StorageException (read error)', () async {
    when(mockStorage.read(key: anyNamed('key'))).thenThrow(Exception('Read failed'));

    expect(() => authService.getToken(), throwsA(isA<StorageException>()));
  });

  test('saveToken handles StorageException (write error)', () async {
    when(mockStorage.write(key: anyNamed('key'), value: anyNamed('value'))).thenThrow(Exception('Write failed'));

    expect(() => authService.saveToken('token'), throwsA(isA<StorageException>()));
  });

  test('deleteToken handles StorageException (delete error)', () async {
    when(mockStorage.delete(key: anyNamed('key'))).thenThrow(Exception('Delete failed'));

    expect(() => authService.deleteToken(), throwsA(isA<StorageException>()));
  });

  test('refreshToken handles StorageException on read', () async {
    when(mockStorage.read(key: 'refresh_token')).thenThrow(Exception('Read failed'));

    expect(() => authService.refreshToken(), throwsA(isA<RefreshTokenInvalidException>()));
  });

  test('refreshToken handles StorageException on write (new token)', () async {
    when(mockStorage.read(key: 'refresh_token')).thenAnswer((_) async => 'old_refresh');

    when(mockClient.post(any, headers: anyNamed('headers'), body: anyNamed('body')))
        .thenAnswer((_) async => http.Response('{"token": "new_token", "refreshToken": "new_refresh"}', 200));

    when(mockStorage.write(key: 'auth_token', value: 'new_token')).thenAnswer((_) async {});
    when(mockStorage.write(key: 'refresh_token', value: 'new_refresh')).thenThrow(Exception('Write refresh failed'));

    // Should succeed but log error (not visible here, but covers the line)
    final token = await authService.refreshToken();
    expect(token, 'new_token');
  });

  test('login handles StorageException on refresh token write', () async {
    when(mockClient.post(any, headers: anyNamed('headers'), body: anyNamed('body')))
        .thenAnswer((_) async => http.Response('{"token": "new_token", "refreshToken": "new_refresh"}', 200));

    when(mockStorage.write(key: 'auth_token', value: 'new_token')).thenAnswer((_) async {});
    when(mockStorage.write(key: 'refresh_token', value: 'new_refresh')).thenThrow(Exception('Write refresh failed'));

    // Should succeed but log error
    final data = await authService.login('email', 'password');
    expect(data['token'], 'new_token');
  });
}
