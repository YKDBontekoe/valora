import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mockito/mockito.dart';
import 'package:retry/retry.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/services/api_client.dart';

class MockClient extends Mock implements http.Client {
  @override
  Future<http.Response> get(Uri? url, {Map<String, String>? headers}) {
    return super.noSuchMethod(
      Invocation.method(#get, [url], {#headers: headers}),
      returnValue: Future.value(http.Response('', 200)),
      returnValueForMissingStub: Future.value(http.Response('', 200)),
    ) as Future<http.Response>;
  }

  @override
  Future<http.Response> post(Uri? url, {Object? body, Encoding? encoding, Map<String, String>? headers}) {
    return super.noSuchMethod(
      Invocation.method(#post, [url], {#body: body, #encoding: encoding, #headers: headers}),
      returnValue: Future.value(http.Response('', 200)),
      returnValueForMissingStub: Future.value(http.Response('', 200)),
    ) as Future<http.Response>;
  }

  @override
  Future<http.Response> put(Uri? url, {Object? body, Encoding? encoding, Map<String, String>? headers}) {
    return super.noSuchMethod(
      Invocation.method(#put, [url], {#body: body, #encoding: encoding, #headers: headers}),
      returnValue: Future.value(http.Response('', 200)),
      returnValueForMissingStub: Future.value(http.Response('', 200)),
    ) as Future<http.Response>;
  }

  @override
  Future<http.Response> delete(Uri? url, {Object? body, Encoding? encoding, Map<String, String>? headers}) {
    return super.noSuchMethod(
      Invocation.method(#delete, [url], {#body: body, #encoding: encoding, #headers: headers}),
      returnValue: Future.value(http.Response('', 200)),
      returnValueForMissingStub: Future.value(http.Response('', 200)),
    ) as Future<http.Response>;
  }

  @override
  Future<http.Response> patch(Uri? url, {Object? body, Encoding? encoding, Map<String, String>? headers}) {
    return super.noSuchMethod(
      Invocation.method(#patch, [url], {#body: body, #encoding: encoding, #headers: headers}),
      returnValue: Future.value(http.Response('', 200)),
      returnValueForMissingStub: Future.value(http.Response('', 200)),
    ) as Future<http.Response>;
  }
}

void main() {
  late MockClient mockClient;
  late ApiClient apiClient;

  setUp(() {
    mockClient = MockClient();
    apiClient = ApiClient(client: mockClient, retryOptions: const RetryOptions(maxAttempts: 1));
  });

  group('ApiClient', () {
    test('get performs a GET request', () async {
      when(mockClient.get(any, headers: anyNamed('headers'))).thenAnswer(
        (_) async => http.Response(jsonEncode({'data': 'test'}), 200),
      );

      final response = await apiClient.get('/test');

      expect(response.statusCode, 200);
      verify(mockClient.get(any, headers: anyNamed('headers'))).called(1);
    });

    test('post performs a POST request', () async {
      when(mockClient.post(any, headers: anyNamed('headers'), body: anyNamed('body'))).thenAnswer(
        (_) async => http.Response(jsonEncode({'id': 1}), 201),
      );

      final response = await apiClient.post('/test', data: {'name': 'test'});

      expect(response.statusCode, 201);
      verify(mockClient.post(any, headers: anyNamed('headers'), body: anyNamed('body'))).called(1);
    });

    test('put performs a PUT request', () async {
      when(mockClient.put(any, headers: anyNamed('headers'), body: anyNamed('body'))).thenAnswer(
        (_) async => http.Response(jsonEncode({'id': 1}), 200),
      );

      final response = await apiClient.put('/test/1', data: {'name': 'updated'});

      expect(response.statusCode, 200);
      verify(mockClient.put(any, headers: anyNamed('headers'), body: anyNamed('body'))).called(1);
    });

    test('delete performs a DELETE request', () async {
      when(mockClient.delete(any, headers: anyNamed('headers'), body: anyNamed('body'))).thenAnswer(
        (_) async => http.Response('', 204),
      );

      final response = await apiClient.delete('/test/1');

      expect(response.statusCode, 204);
      verify(mockClient.delete(any, headers: anyNamed('headers'), body: anyNamed('body'))).called(1);
    });

    test('handleResponse parses success response', () async {
      final response = http.Response(jsonEncode({'key': 'value'}), 200);
      final result = await apiClient.handleResponse(response, (body) => jsonDecode(body));
      expect(result, {'key': 'value'});
    });

    test('handleResponse throws ValidationException on 400', () async {
      final response = http.Response(jsonEncode({'detail': 'Bad Request'}), 400);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<ValidationException>()),
      );
    });

    test('handleResponse throws UnauthorizedException on 401', () async {
      final response = http.Response('Unauthorized', 401);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<UnauthorizedException>()),
      );
    });

    test('handleResponse throws ForbiddenException on 403', () async {
      final response = http.Response('Forbidden', 403);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<ForbiddenException>()),
      );
    });

    test('handleResponse throws NotFoundException on 404', () async {
      final response = http.Response('Not Found', 404);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<NotFoundException>()),
      );
    });

    test('handleResponse throws ConflictException on 409', () async {
      final response = http.Response('Conflict', 409);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<ConflictException>()),
      );
    });

    test('handleResponse throws ServerException on 500', () async {
      final response = http.Response('Internal Server Error', 500);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<ServerException>()),
      );
    });

    test('handleResponse throws ServerException on 503', () async {
      final response = http.Response('Service Unavailable', 503);
      expect(
        () => apiClient.handleResponse(response, (body) => body),
        throwsA(isA<ServerException>()),
      );
    });

    test('request throws Exception on unknown method', () async {
      expect(
        () => apiClient.request('UNKNOWN', '/test'),
        throwsA(isA<UnsupportedError>()),
      );
    });

    test('request uses refresh token on 401', () async {
      // First call 401
      // Second call 200
      var callCount = 0;
      when(mockClient.get(any, headers: anyNamed('headers'))).thenAnswer((invocation) async {
        callCount++;
        if (callCount == 1) {
          return http.Response('Unauthorized', 401);
        }
        return http.Response('Success', 200);
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
      when(mockClient.get(any, headers: anyNamed('headers'))).thenThrow(const SocketException('No internet'));

      expect(
        () => apiClient.get('/test'),
        throwsA(isA<NetworkException>()),
      );
    });

    test('request handles TimeoutException', () async {
      when(mockClient.get(any, headers: anyNamed('headers'))).thenThrow(TimeoutException('Timeout'));

      expect(
        () => apiClient.get('/test'),
        throwsA(isA<NetworkException>()),
      );
    });
  });
}
