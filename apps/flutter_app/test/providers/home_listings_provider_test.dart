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

  test('clearFiltersAndSearch resets filters and search', () async {
    final MockClient client = MockClient((request) async {
      if (request.url.path.endsWith('/health')) {
        return http.Response('ok', 200);
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

    await provider.applyFilters(
      minPrice: 100000,
      maxPrice: 300000,
      city: 'Amsterdam',
      minBedrooms: 2,
      minLivingArea: 50,
      maxLivingArea: 120,
      sortBy: 'price',
      sortOrder: 'asc',
    );
    await provider.setSearchTerm('damrak');
    await provider.clearFiltersAndSearch();

    expect(provider.searchTerm, '');
    expect(provider.minPrice, isNull);
    expect(provider.maxPrice, isNull);
    expect(provider.city, isNull);
    expect(provider.minBedrooms, isNull);
    expect(provider.minLivingArea, isNull);
    expect(provider.maxLivingArea, isNull);
  });

  test('caches listings using UnmodifiableListView to prevent unnecessary rebuilds', () async {
    final MockClient client = MockClient((request) async {
      if (request.url.path.endsWith('/health')) {
        return http.Response('ok', 200);
      }
      return _response(<Map<String, dynamic>>[
        _listing('1', 'Cached Home')
      ]);
    });

    final HomeListingsProvider provider = HomeListingsProvider(
      apiService: ApiService(
        client: client,
        runner: syncRunner,
        retryOptions: const RetryOptions(maxAttempts: 1),
      ),
    );

    // Initial load
    await provider.checkConnectionAndLoad();

    // First access
    final list1 = provider.listings;
    expect(list1, hasLength(1));
    expect(list1.first.address, 'Cached Home');

    // Second access - should be same instance (identity equality)
    final list2 = provider.listings;
    expect(list2, same(list1));

    // Refresh - should invalidate cache
    await provider.refresh();
    final list3 = provider.listings;

    // Content is same, but instance should be new because cache was cleared on refresh
    expect(list3, hasLength(1));
    expect(list3.first.address, 'Cached Home');
    expect(list3, isNot(same(list1)));
  });
}
