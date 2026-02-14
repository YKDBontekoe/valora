import 'dart:convert';

import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:retry/retry.dart';
import 'package:valora_app/core/exceptions/app_exceptions.dart';
import 'package:valora_app/models/context_report.dart';
import 'package:valora_app/models/listing_filter.dart';
import 'package:valora_app/services/http/clients/ai_api_client.dart';
import 'package:valora_app/services/http/clients/context_report_api_client.dart';
import 'package:valora_app/services/http/clients/listings_api_client.dart';
import 'package:valora_app/services/http/clients/map_api_client.dart';
import 'package:valora_app/services/http/clients/notifications_api_client.dart';
import 'package:valora_app/services/http/core/http_transport.dart';

import '../../helpers/test_runners.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUpAll(() async {
    await dotenv.load(fileName: '.env.example');
  });

  HttpTransport buildTransport(http.Client client) {
    return HttpTransport(
      client: client,
      authTokenReader: () => 'token-123',
      retryOptions: const RetryOptions(maxAttempts: 1),
    );
  }

  group('ListingsApiClient contract', () {
    test('getListings sends query params and parses listing response', () async {
      final client = MockClient((request) async {
        expect(request.method, 'GET');
        expect(request.url.path, '/api/listings');
        expect(request.url.queryParameters['page'], '1');
        expect(request.url.queryParameters['pageSize'], '20');
        expect(request.headers['Authorization'], 'Bearer token-123');
        return http.Response(
          json.encode(<String, dynamic>{
            'items': <dynamic>[],
            'pageIndex': 1,
            'totalPages': 1,
            'totalCount': 0,
            'hasNextPage': false,
            'hasPreviousPage': false,
          }),
          200,
        );
      });

      final sut = ListingsApiClient(transport: buildTransport(client), runner: syncRunner);
      final result = await sut.getListings(const ListingFilter());
      expect(result.items, isEmpty);
    });

    test('getListingFromPdok returns null on 404', () async {
      final client = MockClient((request) async {
        expect(request.url.path, '/api/listings/lookup');
        expect(request.url.queryParameters['id'], 'abc');
        return http.Response('', 404);
      });

      final sut = ListingsApiClient(transport: buildTransport(client), runner: syncRunner);
      expect(await sut.getListingFromPdok('abc'), isNull);
    });
  });

  group('ContextReportApiClient contract', () {
    test('getContextReport sends expected payload and parses response', () async {
      final client = MockClient((request) async {
        expect(request.url.path, '/api/context/report');
        expect(request.method, 'POST');
        final payload = json.decode(request.body) as Map<String, dynamic>;
        expect(payload['input'], 'Damrak 1 Amsterdam');
        expect(payload['radiusMeters'], 500);

        return http.Response(
          json.encode(<String, dynamic>{
            'location': <String, dynamic>{
              'query': 'Damrak 1 Amsterdam',
              'displayAddress': 'Damrak 1, 1012LG Amsterdam',
              'latitude': 52.3771,
              'longitude': 4.898,
            },
            'socialMetrics': <dynamic>[],
            'safetyMetrics': <dynamic>[],
            'amenityMetrics': <dynamic>[],
            'environmentMetrics': <dynamic>[],
            'compositeScore': 78.4,
            'sources': <dynamic>[],
            'warnings': <dynamic>[],
          }),
          200,
        );
      });

      final sut = ContextReportApiClient(transport: buildTransport(client), runner: syncRunner);
      final report = await sut.getContextReport('Damrak 1 Amsterdam', radiusMeters: 500);
      expect(report.location.displayAddress, 'Damrak 1, 1012LG Amsterdam');
    });
  });

  group('MapApiClient contract', () {
    test('getMapOverlays locks path and query parameters', () async {
      final client = MockClient((request) async {
        expect(request.url.path, '/api/map/overlays');
        expect(request.url.queryParameters['metric'], 'CrimeRate');
        expect(request.url.queryParameters['minLat'], '1.0');
        return http.Response(json.encode(<dynamic>[]), 200);
      });

      final sut = MapApiClient(transport: buildTransport(client));
      final overlays = await sut.getMapOverlays(
        minLat: 1,
        minLon: 2,
        maxLat: 3,
        maxLon: 4,
        metric: 'CrimeRate',
      );
      expect(overlays, isEmpty);
    });
  });

  group('NotificationsApiClient contract', () {
    test('markNotificationAsRead uses POST endpoint', () async {
      final client = MockClient((request) async {
        expect(request.method, 'POST');
        expect(request.url.path, '/api/notifications/123/read');
        return http.Response('', 200);
      });

      final sut = NotificationsApiClient(transport: buildTransport(client));
      await sut.markNotificationAsRead('123');
    });

    test('getUnreadNotificationCount parses integer response', () async {
      final client = MockClient((request) async {
        expect(request.url.path, '/api/notifications/unread-count');
        return http.Response(json.encode(<String, dynamic>{'count': 4}), 200);
      });

      final sut = NotificationsApiClient(transport: buildTransport(client));
      expect(await sut.getUnreadNotificationCount(), 4);
    });
  });

  group('AiApiClient contract', () {
    test('getAiAnalysis sends report body and parses summary', () async {
      final client = MockClient((request) async {
        expect(request.url.path, '/api/ai/analyze-report');
        final payload = json.decode(request.body) as Map<String, dynamic>;
        expect(payload.containsKey('report'), isTrue);
        return http.Response(json.encode(<String, dynamic>{'summary': 'analysis'}), 200);
      });

      final sut = AiApiClient(transport: buildTransport(client));
      final report = _reportFixture();

      expect(await sut.getAiAnalysis(report), 'analysis');
    });

    test('maps 500 responses to ServerException', () async {
      final client = MockClient((request) async => http.Response('oops', 500));
      final sut = AiApiClient(transport: buildTransport(client));

      expect(() => sut.getAiAnalysis(_reportFixture()), throwsA(isA<ServerException>()));
    });
  });
}

ContextReport _reportFixture() {
  return ContextReport.fromJson(<String, dynamic>{
    'location': <String, dynamic>{
      'query': 'x',
      'displayAddress': 'x',
      'latitude': 0,
      'longitude': 0,
    },
    'socialMetrics': <dynamic>[],
    'safetyMetrics': <dynamic>[],
    'amenityMetrics': <dynamic>[],
    'environmentMetrics': <dynamic>[],
    'compositeScore': 1,
    'sources': <dynamic>[],
    'warnings': <dynamic>[],
  });
}
