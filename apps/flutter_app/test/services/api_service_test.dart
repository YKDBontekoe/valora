import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  setUpAll(() async {
    await dotenv.load(fileName: ".env.example");
  });

  group('ApiService', () {
    test('getListings throws ServerException on 500', () async {
      final client = MockClient((request) async {
        return http.Response('Internal Server Error', 500);
      });

      final apiService = ApiService(client: client);

      expect(
        () => apiService.getListings(),
        throwsA(isA<ServerException>()),
      );
    });

    test('getListings throws NetworkException on SocketException', () async {
      final client = MockClient((request) async {
        throw const SocketException('No internet');
      });

      final apiService = ApiService(client: client);

      expect(
        () => apiService.getListings(),
        throwsA(isA<NetworkException>()),
      );
    });

    test('getListings throws NetworkException on TimeoutException', () async {
      final client = MockClient((request) async {
        throw TimeoutException('Timed out');
      });

      final apiService = ApiService(client: client);

      expect(
        () => apiService.getListings(),
        throwsA(isA<NetworkException>()),
      );
    });

    test('getListings throws NetworkException on ClientException', () async {
      final client = MockClient((request) async {
        throw http.ClientException('Client error');
      });

      final apiService = ApiService(client: client);

      expect(
        () => apiService.getListings(),
        throwsA(isA<NetworkException>()),
      );
    });

    test('getListings throws UnknownException on Generic Exception', () async {
      final client = MockClient((request) async {
        throw Exception('Boom');
      });

      final apiService = ApiService(client: client);

      expect(
        () => apiService.getListings(),
        throwsA(isA<UnknownException>()),
      );
    });

    test('getListings throws ValidationException on 400 with detail', () async {
      final client = MockClient((request) async {
        return http.Response(json.encode({'detail': 'Some detailed error'}), 400);
      });

      final apiService = ApiService(client: client);

      try {
        await apiService.getListings();
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
        await apiService.getListings();
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
        await apiService.getListings();
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

      final result = await apiService.getListings();
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
  });
}
