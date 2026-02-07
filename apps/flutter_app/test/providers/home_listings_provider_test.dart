import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:retry/retry.dart';
import 'package:valora_app/providers/home_listings_provider.dart';
import 'package:valora_app/services/api_service.dart';

import '../helpers/test_runners.dart';

Map<String, dynamic> _listing(String id, String address) {
  return <String, dynamic>{
    'id': id,
    'fundaId': id,
    'address': address,
    'city': 'Amsterdam',
    'price': 100000,
  };
}

http.Response _response(
  List<Map<String, dynamic>> items, {
  bool hasNextPage = false,
}) {
  return http.Response(
    json.encode(<String, dynamic>{
      'items': items,
      'pageIndex': 1,
      'totalPages': hasNextPage ? 2 : 1,
      'totalCount': items.length,
      'hasNextPage': hasNextPage,
      'hasPreviousPage': false,
    }),
    200,
  );
}

void main() {
  setUpAll(() async {
    await dotenv.load(fileName: '.env.example');
  });

  test('sets disconnected state when health check fails', () async {
    final MockClient client = MockClient((request) async {
      if (request.url.path.endsWith('/health')) {
        return http.Response('error', 500);
      }
      return http.Response('not-found', 404);
    });

    final HomeListingsProvider provider = HomeListingsProvider(
      apiService: ApiService(
        client: client,
        runner: syncRunner,
        retryOptions: const RetryOptions(maxAttempts: 1),
      ),
    );

    await provider.checkConnectionAndLoad();

    expect(provider.isConnected, isFalse);
    expect(provider.isLoading, isFalse);
  });

  test('ignores stale refresh responses for home listings', () async {
    final MockClient client = MockClient((request) async {
      if (request.url.path.endsWith('/health')) {
        return http.Response('ok', 200);
      }

      final String searchTerm = request.url.queryParameters['searchTerm'] ?? '';
      if (searchTerm == 'old') {
        await Future<void>.delayed(const Duration(milliseconds: 120));
        return _response(<Map<String, dynamic>>[_listing('1', 'Old Home')]);
      }

      return _response(<Map<String, dynamic>>[_listing('2', 'New Home')]);
    });

    final HomeListingsProvider provider = HomeListingsProvider(
      apiService: ApiService(
        client: client,
        runner: syncRunner,
        retryOptions: const RetryOptions(maxAttempts: 1),
      ),
    );

    await provider.checkConnectionAndLoad();

    final Future<void> staleFuture = provider.setSearchTerm('old');
    await Future<void>.delayed(const Duration(milliseconds: 10));
    await provider.setSearchTerm('new');
    await staleFuture;

    expect(provider.listings, hasLength(1));
    expect(provider.listings.first.address, 'New Home');
  });

  test('triggerScrape posts selected region and limit', () async {
    String? seenPath;
    String? seenRegion;
    String? seenLimit;

    final MockClient client = MockClient((request) async {
      if (request.url.path.endsWith('/health')) {
        return http.Response('ok', 200);
      }

      if (request.url.path.endsWith('/scraper/trigger-limited')) {
        seenPath = request.url.path;
        seenRegion = request.url.queryParameters['region'];
        seenLimit = request.url.queryParameters['limit'];
        return http.Response('{}', 200);
      }

      return _response(<Map<String, dynamic>>[]);
    });

    final HomeListingsProvider provider = HomeListingsProvider(
      apiService: ApiService(
        client: client,
        runner: syncRunner,
        retryOptions: const RetryOptions(maxAttempts: 1),
      ),
    );

    await provider.triggerScrape(region: 'utrecht', limit: 25);

    expect(seenPath, endsWith('/scraper/trigger-limited'));
    expect(seenRegion, 'utrecht');
    expect(seenLimit, '25');
  });
}
