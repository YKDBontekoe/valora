import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:valora_app/services/api_client.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';

void main() {
  group('ApiClient', () {
    test('post retries when retry is true', () async {
      int calls = 0;
      final mockClient = MockClient((request) async {
        calls++;
        if (calls < 3) {
          return http.Response('Error', 500);
        }
        return http.Response('{"success": true}', 200);
      });

      final client = ApiClient(client: mockClient);

      final result = await client.post('/test', {}, retry: true);

      expect(calls, 3);
      expect(result['success'], isTrue);
    });

    test('post does NOT retry when retry is false (default)', () async {
      int calls = 0;
      final mockClient = MockClient((request) async {
        calls++;
        return http.Response('Error', 500);
      });

      final client = ApiClient(client: mockClient);

      // Should throw ServerException (mapped from 500) immediately without retrying
      await expectLater(
        client.post('/test', {}), // retry default is false
        throwsA(isA<ServerException>()),
      );

      expect(calls, 1);
    });

    test('get retries by default', () async {
      int calls = 0;
      final mockClient = MockClient((request) async {
        calls++;
        if (calls < 3) {
          return http.Response('Error', 500);
        }
        return http.Response('{"success": true}', 200);
      });

      final client = ApiClient(client: mockClient);

      final result = await client.get('/test');

      expect(calls, 3);
      expect(result['success'], isTrue);
    });

    test('returns response on failure after max retries (Option A)', () async {
      int calls = 0;
      final mockClient = MockClient((request) async {
        calls++;
        return http.Response('{"detail": "Specific Error"}', 503);
      });

      final client = ApiClient(client: mockClient);

      try {
        await client.get('/test');
        fail('Should have thrown');
      } on ServerException catch (e) {
        expect(e.message, contains('Specific Error'));
      }

      // Max attempts is 3
      expect(calls, 3);
    });
  });
}
