import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/models/listing_filter.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:retry/retry.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUpAll(() async {
    await dotenv.load(fileName: ".env.example");
  });

  group('ApiService', () {
    test('baseUrl uses dotenv', () {
      expect(ApiService.baseUrl, 'http://localhost:5000/api');
    });

    test('baseUrl falls back to default if dotenv missing', () {
      // Backup
      final backup = Map<String, String>.from(dotenv.env);
      dotenv.env.clear();

      try {
        expect(ApiService.baseUrl, 'http://localhost:5000/api');
      } finally {
        // Restore
        dotenv.env.addAll(backup);
      }
    });

    test('getListings throws ServerException on 500', () async {
      final client = MockClient((request) async {
        return http.Response('Internal Server Error', 500);
      });

      final apiService = ApiService(client: client);

      expect(
        () => apiService.getListings(const ListingFilter()),
        throwsA(isA<ServerException>()),
      );
    });

    test('getListings throws NetworkException on SocketException', () async {
      final client = MockClient((request) async {
        throw const SocketException('No internet');
      });

      final apiService = ApiService(client: client);

      expect(
        () => apiService.getListings(const ListingFilter()),
        throwsA(isA<NetworkException>()),
      );
    });

    test('getListings throws NetworkException on TimeoutException', () async {
      final client = MockClient((request) async {
        throw TimeoutException('Timed out');
      });

      final apiService = ApiService(
        client: client,
        retryOptions: const RetryOptions(maxAttempts: 1),
      );

      expect(
        () => apiService.getListings(const ListingFilter()),
        throwsA(isA<NetworkException>()),
      );
    });

    test('getListings throws NetworkException on ClientException', () async {
      final client = MockClient((request) async {
        throw http.ClientException('Client error');
      });

      final apiService = ApiService(
        client: client,
        retryOptions: const RetryOptions(maxAttempts: 1),
      );

      expect(
        () => apiService.getListings(const ListingFilter()),
        throwsA(isA<NetworkException>()),
      );
    });

    test('getListings throws UnknownException on Generic Exception', () async {
      final client = MockClient((request) async {
        throw Exception('Boom');
      });

      final apiService = ApiService(client: client);

      expect(
        () => apiService.getListings(const ListingFilter()),
        throwsA(isA<UnknownException>()),
      );
    });

    test('getListings throws ValidationException on 400 with detail', () async {
      final client = MockClient((request) async {
        return http.Response(json.encode({'detail': 'Some detailed error'}), 400);
      });

      final apiService = ApiService(client: client);

      try {
        await apiService.getListings(const ListingFilter());
        fail('Should have thrown');
      } on ValidationException catch (e) {
        expect(e.message, 'Some detailed error');
      }
    });

    test('getListings throws ValidationException on 400 with title', () async {
      final client = MockClient((request) async {
        return http.Response(json.encode({'title': 'Invalid input'}), 400);
      });

      final apiService = ApiService(client: client);

      try {
        await apiService.getListings(const ListingFilter());
        fail('Should have thrown');
      } on ValidationException catch (e) {
         expect(e.message, 'Invalid input');
      }
    });

    test('getListings throws ValidationException on 400 with default message on parsing fail', () async {
      final client = MockClient((request) async {
        return http.Response('Not JSON', 400);
      });

      final apiService = ApiService(client: client);

      try {
        await apiService.getListings(const ListingFilter());
        fail('Should have thrown');
      } on ValidationException catch (e) {
        expect(e.message, 'Invalid request');
      }
    });

    test('getListings returns data on 200', () async {
      final mockResponse = {
        'items': [],
        'pageIndex': 1,
        'totalPages': 1,
        'totalCount': 0,
        'hasNextPage': false,
        'hasPreviousPage': false
      };

      final client = MockClient((request) async {
        return http.Response(json.encode(mockResponse), 200);
      });

      final apiService = ApiService(client: client);

      final result = await apiService.getListings(const ListingFilter());
      expect(result.items, isEmpty);
    });

    test('healthCheck returns false on exception', () async {
      final client = MockClient((request) async {
        throw Exception('Fail');
      });

      final apiService = ApiService(client: client);
      expect(await apiService.healthCheck(), isFalse);
    });

    test('getListing returns null on 404', () async {
       final client = MockClient((request) async {
        return http.Response('Not Found', 404);
      });

      final apiService = ApiService(client: client);
      expect(await apiService.getListing('123'), isNull);
    });

     test('getListing throws ServerException on 500', () async {
       final client = MockClient((request) async {
        return http.Response('Error', 500);
      });

      final apiService = ApiService(client: client);
       expect(
        () => apiService.getListing('123'),
        throwsA(isA<ServerException>()),
      );
    });

    test('triggerLimitedScrape makes correct request', () async {
      final client = MockClient((request) async {
        expect(request.url.path, '/api/scraper/trigger-limited');
        expect(request.url.queryParameters['region'], 'amsterdam');
        expect(request.url.queryParameters['limit'], '10');
        expect(request.method, 'POST');
        return http.Response(json.encode({'message': 'Queued'}), 200);
      });

      final apiService = ApiService(client: client);
      await apiService.triggerLimitedScrape('amsterdam', 10);
    });

    test('triggerLimitedScrape throws ServerException on failure', () async {
      final client = MockClient((request) async {
        return http.Response('Error', 500);
      });

      final apiService = ApiService(client: client);
      expect(
        () => apiService.triggerLimitedScrape('amsterdam', 10),
        throwsA(isA<ServerException>()),
      );
    });

    test('getListings retries on 401 if refresh callback succeeds', () async {
      int callCount = 0;
      final mockResponse = {
        'items': [],
        'pageIndex': 1,
        'totalPages': 1,
        'totalCount': 0,
        'hasNextPage': false,
        'hasPreviousPage': false
      };

      final client = MockClient((request) async {
        callCount++;
        if (callCount == 1) {
          // First call fails
          return http.Response('Unauthorized', 401);
        }
        // Second call (after refresh) succeeds and uses new token
        if (request.headers['Authorization'] == 'Bearer new_token') {
          return http.Response(json.encode(mockResponse), 200);
        }
        return http.Response('Unauthorized', 401);
      });

      final apiService = ApiService(
        client: client,
        authToken: 'old_token',
        refreshTokenCallback: () async => 'new_token',
      );

      final result = await apiService.getListings(const ListingFilter());
      expect(result.items, isEmpty);
      expect(callCount, 2);
    });

    test('getListings fails on 401 if refresh callback returns null', () async {
      final client = MockClient((request) async {
        return http.Response('Unauthorized', 401);
      });

      final apiService = ApiService(
        client: client,
        authToken: 'old_token',
        refreshTokenCallback: () async => null,
      );

      expect(
        () => apiService.getListings(const ListingFilter()),
        throwsA(isA<UnknownException>()),
      );
    });
  });
}
