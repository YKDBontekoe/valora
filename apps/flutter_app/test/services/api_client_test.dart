import 'dart:async';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/services/api_client.dart';

@GenerateMocks([http.Client])
import 'api_client_test.mocks.dart';

void main() {
  late ApiClient client;
  late MockClient mockHttpClient;

  setUp(() {
    mockHttpClient = MockClient();
    client = ApiClient(client: mockHttpClient);
  });

  group('ApiClient', () {
    test('request adds auth token if present', () async {
      client.updateAuthToken('token');
      when(mockHttpClient.get(
        any,
        headers: anyNamed('headers'),
      )).thenAnswer((_) async => http.Response('{}', 200));

      await client.get('/test');

      verify(mockHttpClient.get(
        Uri.parse('${ApiClient.baseUrl}/test'),
        headers: argThat(containsPair('Authorization', 'Bearer token')),
      )).called(1);
    });

    test('handleResponse throws ValidationException on 400', () async {
      final response = http.Response('{"detail": "Bad Request"}', 400);

      expect(
        () => client.handleResponse(response, (body) => body),
        throwsA(isA<ValidationException>()),
      );
    });

    test('handleResponse throws UnauthorizedException on 401', () async {
      final response = http.Response('', 401);

      expect(
        () => client.handleResponse(response, (body) => body),
        throwsA(isA<UnauthorizedException>()),
      );
    });

    test('handleResponse throws ServerException on 500', () async {
      final response = http.Response('', 500);

      expect(
        () => client.handleResponse(response, (body) => body),
        throwsA(isA<ServerException>()),
      );
    });

    test('handleResponse parses TraceId from response', () async {
      final response = http.Response('{"detail": "Error", "traceId": "123"}', 400);

      try {
        await client.handleResponse(response, (body) => body);
        fail('Should throw');
      } on ValidationException catch (e) {
        expect(e.message, contains('Ref: 123'));
      }
    });

    test('retry logic retries on 500', () async {
      int calls = 0;
      when(mockHttpClient.get(any, headers: anyNamed('headers')))
          .thenAnswer((_) async {
            calls++;
            if (calls < 3) return http.Response('', 500);
            return http.Response('{}', 200);
          });

      await client.get('/retry');
      expect(calls, 3);
    });
  });
}
