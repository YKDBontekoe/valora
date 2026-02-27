import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:retry/retry.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/services/api_client.dart';

void main() {
  late MockClient mockClient;
  late ApiClient apiClient;

  MockClient createClient({int statusCode = 200, String body = '{}'}) {
    return MockClient((request) async {
      return http.Response(body, statusCode);
    });
  }

  setUp(() {
    // Initialize with a dummy client, though individual tests usually override it.
    mockClient = createClient();
    apiClient = ApiClient(client: mockClient, retryOptions: const RetryOptions(maxAttempts: 1));
  });

  group('ApiClient Tests', () {
    test('get performs a GET request', () async {
      mockClient = MockClient((request) async {
        if (request.method == 'GET' && request.url.path == '/test') {
          return http.Response(jsonEncode({'data': 'test'}), 200);
        }
        return http.Response('Not Found', 404);
      });
      apiClient = ApiClient(client: mockClient, retryOptions: const RetryOptions(maxAttempts: 1));

      final response = await apiClient.get('/test');

      expect(response.statusCode, 200);
      expect(jsonDecode(response.body)['data'], 'test');
    });

    test('post performs a POST request', () async {
      mockClient = MockClient((request) async {
        if (request.method == 'POST' && request.url.path == '/test') {
          return http.Response(jsonEncode({'id': 1}), 201);
        }
        return http.Response('Error', 400);
      });
      apiClient = ApiClient(client: mockClient, retryOptions: const RetryOptions(maxAttempts: 1));

      final response = await apiClient.post('/test', data: {'name': 'test'});

      expect(response.statusCode, 201);
    });

    test('put performs a PUT request', () async {
      mockClient = createClient(statusCode: 200);
      apiClient = ApiClient(client: mockClient, retryOptions: const RetryOptions(maxAttempts: 1));

      final response = await apiClient.put('/test/1', data: {'name': 'updated'});
      expect(response.statusCode, 200);
    });

    test('delete performs a DELETE request', () async {
      mockClient = createClient(statusCode: 204, body: '');
      apiClient = ApiClient(client: mockClient, retryOptions: const RetryOptions(maxAttempts: 1));

      final response = await apiClient.delete('/test/1');
      expect(response.statusCode, 204);
    });

    test('handleResponse parses success response', () async {
      apiClient = ApiClient(client: createClient(), retryOptions: const RetryOptions(maxAttempts: 1));
      final response = http.Response(jsonEncode({'key': 'value'}), 200);
      final result = await apiClient.handleResponse(response, (body) => jsonDecode(body));
      expect(result, {'key': 'value'});
    });

    test('handleResponse throws ValidationException on 400', () async {
      apiClient = ApiClient(client: createClient(), retryOptions: const RetryOptions(maxAttempts: 1));
      final response = http.Response(jsonEncode({'detail': 'Bad Request'}), 400);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<ValidationException>()),
      );
    });

    test('handleResponse throws UnauthorizedException on 401', () async {
      apiClient = ApiClient(client: createClient(), retryOptions: const RetryOptions(maxAttempts: 1));
      final response = http.Response('Unauthorized', 401);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<UnauthorizedException>()),
      );
    });

    test('handleResponse throws ForbiddenException on 403', () async {
      apiClient = ApiClient(client: createClient(), retryOptions: const RetryOptions(maxAttempts: 1));
      final response = http.Response('Forbidden', 403);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<ForbiddenException>()),
      );
    });

    test('handleResponse throws NotFoundException on 404', () async {
      apiClient = ApiClient(client: createClient(), retryOptions: const RetryOptions(maxAttempts: 1));
      final response = http.Response('Not Found', 404);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<NotFoundException>()),
      );
    });

    test('handleResponse throws ConflictException on 409', () async {
      apiClient = ApiClient(client: createClient(), retryOptions: const RetryOptions(maxAttempts: 1));
      final response = http.Response('Conflict', 409);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<ConflictException>()),
      );
    });

    test('handleResponse throws ServerException on 500', () async {
      apiClient = ApiClient(client: createClient(), retryOptions: const RetryOptions(maxAttempts: 1));
      final response = http.Response('Internal Server Error', 500);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<ServerException>()),
      );
    });

    test('handleResponse throws ServerException on 503', () async {
      apiClient = ApiClient(client: createClient(), retryOptions: const RetryOptions(maxAttempts: 1));
      final response = http.Response('Service Unavailable', 503);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<ServerException>()),
      );
    });

    test('request throws UnknownException on unknown method', () async {
      apiClient = ApiClient(client: createClient(), retryOptions: const RetryOptions(maxAttempts: 1));
      expect(
        () => apiClient.request('UNKNOWN', '/test'),
        throwsA(isA<UnknownException>()),
      );
    });

    test('request uses refresh token on 401', () async {
      var callCount = 0;
      mockClient = MockClient((request) async {
        callCount++;
        if (callCount == 1) {
          if (request.headers['Authorization'] == 'Bearer old_token') {
            return http.Response('Unauthorized', 401);
          }
        }
        if (callCount == 2) {
          if (request.headers['Authorization'] == 'Bearer new_token') {
            return http.Response('Success', 200);
          }
        }
        return http.Response('Fail', 500);
      });

      apiClient = ApiClient(
        client: mockClient,
        retryOptions: const RetryOptions(maxAttempts: 1),
        refreshTokenCallback: () async => 'new_token',
        authToken: 'old_token',
      );

      final response = await apiClient.get('/test');
      expect(response.statusCode, 200);
      expect(response.body, 'Success');
    });

    test('request handles SocketException', () async {
      mockClient = MockClient((request) async {
        throw const SocketException('No internet');
      });
      apiClient = ApiClient(client: mockClient, retryOptions: const RetryOptions(maxAttempts: 1));

      expect(
        () => apiClient.get('/test'),
        throwsA(isA<NetworkException>()),
      );
    });

    test('request handles TimeoutException', () async {
      mockClient = MockClient((request) async {
        throw TimeoutException('Timeout');
      });
      apiClient = ApiClient(client: mockClient, retryOptions: const RetryOptions(maxAttempts: 1));

      expect(
        () => apiClient.get('/test'),
        throwsA(isA<NetworkException>()),
      );
    });
  });
}
