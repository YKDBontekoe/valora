import '../helpers/test_runners.dart';
import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/services/api_service.dart';
import 'package:valora_app/models/listing_filter.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/models/listing.dart';
import 'package:valora_app/models/notification.dart';
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
      dotenv.env['API_URL'] = 'http://test-api.com/api';
      expect(ApiService.baseUrl, 'http://test-api.com/api');
    });

    test('baseUrl falls back to default if dotenv missing', () {
      // Backup
      final backup = Map<String, String>.from(dotenv.env);
      dotenv.env.clear();

      try {
        expect(ApiService.baseUrl, 'http://localhost:5001/api');
      } finally {
        // Restore
        dotenv.env.addAll(backup);
      }
    });

    // ... existing tests for exceptions ...

    test('getListings throws TransientHttpException on 500', () async {
      final client = MockClient((request) async {
        return http.Response('Internal Server Error', 500);
      });

      final apiService = ApiService(
          runner: syncRunner,
          client: client,
          retryOptions: const RetryOptions(maxAttempts: 1),
      );

      try {
        await apiService.getListings(const ListingFilter());
        fail('Should have thrown');
      } on TransientHttpException catch (e) {
        expect(e.message, contains('Service temporarily unavailable'));
      }
    });

    // ... other exception tests ...

    // New tests to cover success paths of refactored methods

    test('getListing returns data on success', () async {
      final mockListing = {
        'id': '123',
        'fundaId': 'f123',
        'address': 'Test Street 1',
        'price': 100000,
        'city': 'Test City',
        'postalCode': '1234AB',
      };
      final client = MockClient((request) async {
        return http.Response(json.encode(mockListing), 200);
      });
      final apiService = ApiService(runner: syncRunner, client: client);
      final result = await apiService.getListing('123');
      expect(result.id, '123');
    });

    test('getListingFromPdok returns data on success', () async {
      final mockListing = {
        'id': 'pdok-123',
        'fundaId': 'f123',
        'address': 'Test Pdok',
        'price': 200000,
        'city': 'Test City',
        'postalCode': '1234AB',
      };
      final client = MockClient((request) async {
        return http.Response(json.encode(mockListing), 200);
      });
      final apiService = ApiService(runner: syncRunner, client: client);
      final result = await apiService.getListingFromPdok('pdok-123');
      expect(result?.id, 'pdok-123');
    });

    test('getNotifications returns data on success', () async {
      final mockNotifications = [
        {
          'id': 'n1',
          'title': 'Test',
          'body': 'Message',
          'isRead': false,
          'createdAt': DateTime.now().toIso8601String(),
          'type': 0 // NotificationType.info
        }
      ];
      final client = MockClient((request) async {
        return http.Response(json.encode(mockNotifications), 200);
      });
      final apiService = ApiService(runner: syncRunner, client: client);
      final result = await apiService.getNotifications();
      expect(result.length, 1);
      expect(result.first.id, 'n1');
    });

    test('getUnreadNotificationCount returns count on success', () async {
      final client = MockClient((request) async {
        return http.Response(json.encode({'count': 5}), 200);
      });
      final apiService = ApiService(runner: syncRunner, client: client);
      final count = await apiService.getUnreadNotificationCount();
      expect(count, 5);
    });

    test('markNotificationAsRead succeeds', () async {
      final client = MockClient((request) async {
        return http.Response('', 200);
      });
      final apiService = ApiService(runner: syncRunner, client: client);
      await apiService.markNotificationAsRead('n1');
    });

    test('markAllNotificationsAsRead succeeds', () async {
      final client = MockClient((request) async {
        return http.Response('', 200);
      });
      final apiService = ApiService(runner: syncRunner, client: client);
      await apiService.markAllNotificationsAsRead();
    });

    test('getCityInsights returns data on success', () async {
      final mockInsights = [
        {
          'city': 'Amsterdam',
          'count': 100,
          'latitude': 52.377956,
          'longitude': 4.897070
        }
      ];
      final client = MockClient((request) async {
        return http.Response(json.encode(mockInsights), 200);
      });
      final apiService = ApiService(runner: syncRunner, client: client);
      final result = await apiService.getCityInsights();
      expect(result.first.city, 'Amsterdam');
    });

    test('getMapAmenities returns data on success', () async {
      final mockAmenities = [
        {'id': 'a1', 'type': 'school', 'latitude': 52.0, 'longitude': 4.0, 'name': 'School'}
      ];
      final client = MockClient((request) async {
        return http.Response(json.encode(mockAmenities), 200);
      });
      final apiService = ApiService(runner: syncRunner, client: client);
      final result = await apiService.getMapAmenities(
        minLat: 51,
        minLon: 3,
        maxLat: 53,
        maxLon: 5
      );
      expect(result.first.id, 'a1');
    });

    test('getMapOverlays returns data on success', () async {
      final mockOverlays = [
        {'id': 'o1', 'geometry': [], 'metricValue': 80.0}
      ];
      final client = MockClient((request) async {
        return http.Response(json.encode(mockOverlays), 200);
      });
      final apiService = ApiService(runner: syncRunner, client: client);
      final result = await apiService.getMapOverlays(
        minLat: 51,
        minLon: 3,
        maxLat: 53,
        maxLon: 5,
        metric: 'safety'
      );
      expect(result.first.id, 'o1');
    });

    test('getAiAnalysis returns summary on success', () async {
      final client = MockClient((request) async {
        return http.Response(json.encode({'summary': 'AI Analysis'}), 200);
      });
      final apiService = ApiService(runner: syncRunner, client: client);
      // Dummy report
      final report = ContextReport(
        location: ContextLocation(
          query: 'q',
          displayAddress: 'a',
          latitude: 0,
          longitude: 0,
        ),
        socialMetrics: [],
        crimeMetrics: [],
        demographicsMetrics: [],
        housingMetrics: [],
        mobilityMetrics: [],
        amenityMetrics: [],
        environmentMetrics: [],
        compositeScore: 0,
        categoryScores: {},
        sources: [],
        warnings: [],
      );
      final result = await apiService.getAiAnalysis(report);
      expect(result, 'AI Analysis');
    });

    test('getContextReport makes correct request', () async {
      final client = MockClient((request) async {
        expect(request.url.path, '/api/context/report');
        expect(request.method, 'POST');
        final body = json.decode(request.body) as Map<String, dynamic>;
        expect(body['input'], 'Damrak 1 Amsterdam');
        expect(body['radiusMeters'], 900);
        return http.Response(
          json.encode({
            'location': {
              'query': 'Damrak 1 Amsterdam',
              'displayAddress': 'Damrak 1, 1012LG Amsterdam',
              'latitude': 52.3771,
              'longitude': 4.8980,
            },
            'socialMetrics': [],
            'safetyMetrics': [],
            'amenityMetrics': [],
            'environmentMetrics': [],
            'compositeScore': 78.4,
            'sources': [],
            'warnings': [],
          }),
          200,
        );
      });

      final apiService = ApiService(runner: syncRunner, client: client);
      final report = await apiService.getContextReport(
        'Damrak 1 Amsterdam',
        radiusMeters: 900,
      );
      expect(report.location.displayAddress, 'Damrak 1, 1012LG Amsterdam');
      expect(report.compositeScore, 78.4);
    });

    test('getContextReport throws TransientHttpException on failure', () async {
      final client = MockClient((request) async {
        return http.Response('Error', 500);
      });

      final apiService = ApiService(runner: syncRunner, client: client);
      expect(
        () => apiService.getContextReport('Damrak 1 Amsterdam'),
        throwsA(isA<TransientHttpException>()),
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
        'hasPreviousPage': false,
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
        runner: syncRunner,
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
        runner: syncRunner,
        client: client,
        authToken: 'old_token',
        refreshTokenCallback: () async => null,
      );

      expect(
        () => apiService.getListings(const ListingFilter()),
        throwsA(isA<UnauthorizedException>()),
      );
    });

    test('deleteNotification sends DELETE request', () async {
      final client = MockClient((request) async {
        expect(request.method, 'DELETE');
        expect(request.url.path, '/api/notifications/123');
        return http.Response('', 200);
      });

      final apiService = ApiService(runner: syncRunner, client: client);
      await apiService.deleteNotification('123');
    });

    test('deleteNotification throws TransientHttpException on 500', () async {
      final client = MockClient((request) async {
        return http.Response('Error', 500);
      });

      final apiService = ApiService(runner: syncRunner, client: client);
      expect(
        () => apiService.deleteNotification('123'),
        throwsA(isA<TransientHttpException>()),
      );
    });

    test('getListings retries on 503 and succeeds', () async {
      int callCount = 0;
      final mockResponse = {
        'items': [],
        'pageIndex': 1,
        'totalPages': 1,
        'totalCount': 0,
        'hasNextPage': false,
        'hasPreviousPage': false,
      };

      final client = MockClient((request) async {
        callCount++;
        if (callCount == 1) {
          return http.Response('Service Unavailable', 503);
        }
        return http.Response(json.encode(mockResponse), 200);
      });

      final apiService = ApiService(
        runner: syncRunner,
        client: client,
        retryOptions: const RetryOptions(
          maxAttempts: 2,
          delayFactor: Duration.zero,
        ),
      );

      final result = await apiService.getListings(const ListingFilter());
      expect(result.items, isEmpty);
      expect(callCount, 2);
    });
  });
}
