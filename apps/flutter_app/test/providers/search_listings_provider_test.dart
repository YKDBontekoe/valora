import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:retry/retry.dart';
import 'package:valora_app/providers/search_listings_provider.dart';
import 'package:valora_app/services/api_service.dart';

import '../helpers/test_runners.dart';

Map<String, dynamic> _listing({required String id, required String address}) {
  return <String, dynamic>{
    'id': id,
    'fundaId': id,
    'address': address,
    'city': 'Amsterdam',
    'price': 100000,
  };
}

http.Response _listingResponse({
  required List<Map<String, dynamic>> items,
  required bool hasNextPage,
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

  test('ignores stale responses when a newer search completes first', () async {
    final MockClient client = MockClient((request) async {
      final String searchTerm = request.url.queryParameters['searchTerm'] ?? '';

      if (searchTerm == 'old') {
        await Future<void>.delayed(const Duration(milliseconds: 120));
        return _listingResponse(
          items: <Map<String, dynamic>>[
            _listing(id: '1', address: 'Old Result'),
          ],
          hasNextPage: false,
        );
      }

      return _listingResponse(
        items: <Map<String, dynamic>>[_listing(id: '2', address: 'New Result')],
        hasNextPage: false,
      );
    });

    final ApiService apiService = ApiService(
      client: client,
      runner: syncRunner,
      retryOptions: const RetryOptions(maxAttempts: 1),
    );

    final SearchListingsProvider provider = SearchListingsProvider(
      apiService: apiService,
    );

    provider.setQuery('old');
    final Future<void> staleFuture = provider.refresh();

    await Future<void>.delayed(const Duration(milliseconds: 10));
    provider.setQuery('new');
    await provider.refresh();
    await staleFuture;

    expect(provider.listings, hasLength(1));
    expect(provider.listings.first.address, 'New Result');
  });

  test('keeps current page listings when pagination fails', () async {
    final MockClient client = MockClient((request) async {
      final int page = int.parse(request.url.queryParameters['page'] ?? '1');
      if (page == 1) {
        return _listingResponse(
          items: <Map<String, dynamic>>[
            _listing(id: '1', address: 'Primary Result'),
          ],
          hasNextPage: true,
        );
      }

      return http.Response('Server Error', 500);
    });

    final ApiService apiService = ApiService(
      client: client,
      runner: syncRunner,
      retryOptions: const RetryOptions(maxAttempts: 1),
    );

    final SearchListingsProvider provider = SearchListingsProvider(
      apiService: apiService,
    );
    provider.setQuery('amsterdam');

    await provider.refresh();
    expect(provider.listings, hasLength(1));

    await provider.loadMore();

    expect(provider.listings, hasLength(1));
    expect(provider.error, isNotNull);
  });

  test('clearing specific filter resets value and refreshes', () async {
    final MockClient client = MockClient((request) async {
      return _listingResponse(items: <Map<String, dynamic>>[], hasNextPage: false);
    });

    final ApiService apiService = ApiService(
      client: client,
      runner: syncRunner,
      retryOptions: const RetryOptions(maxAttempts: 1),
    );

    final SearchListingsProvider provider = SearchListingsProvider(
      apiService: apiService,
    );

    // Initial state with filters
    await provider.applyFilters(
      minPrice: 100,
      maxPrice: 200,
      city: 'Amsterdam',
      minBedrooms: 2,
      minLivingArea: 50,
      maxLivingArea: 100,
      minSafetyScore: null,
      minCompositeScore: null,
      sortBy: 'price',
      sortOrder: 'asc',
    );

    expect(provider.minPrice, 100);
    expect(provider.city, 'Amsterdam');
    expect(provider.minBedrooms, 2);
    expect(provider.minLivingArea, 50);
    expect(provider.sortBy, 'price');

    // Clear Price
    await provider.clearPriceFilter();
    expect(provider.minPrice, isNull);
    expect(provider.maxPrice, isNull);
    expect(provider.city, 'Amsterdam'); // Others remain

    // Clear City
    await provider.clearCityFilter();
    expect(provider.city, isNull);

    // Clear Bedrooms
    await provider.clearBedroomsFilter();
    expect(provider.minBedrooms, isNull);

    // Clear Living Area
    await provider.clearLivingAreaFilter();
    expect(provider.minLivingArea, isNull);
    expect(provider.maxLivingArea, isNull);

    // Clear Sort
    await provider.clearSort();
    expect(provider.sortBy, isNull);
    expect(provider.sortOrder, isNull);
  });

  test('refresh(clearData: false) keeps existing listings during load', () async {
    final MockClient client = MockClient((request) async {
      final String searchTerm = request.url.queryParameters['searchTerm'] ?? '';

      // Simulate network delay
      await Future<void>.delayed(const Duration(milliseconds: 50));

      if (searchTerm == 'initial') {
        return _listingResponse(
          items: <Map<String, dynamic>>[
            _listing(id: '1', address: 'Initial Result'),
          ],
          hasNextPage: false,
        );
      }

      return _listingResponse(
        items: <Map<String, dynamic>>[
          _listing(id: '2', address: 'Refreshed Result'),
        ],
        hasNextPage: false,
      );
    });

    final ApiService apiService = ApiService(
      client: client,
      runner: syncRunner,
      retryOptions: const RetryOptions(maxAttempts: 1),
    );

    final SearchListingsProvider provider = SearchListingsProvider(
      apiService: apiService,
    );

    // Initial load
    provider.setQuery('initial');
    await provider.refresh();
    expect(provider.listings, hasLength(1));
    expect(provider.listings.first.address, 'Initial Result');

    // Trigger refresh without clearing data
    provider.setQuery('refreshed');
    final Future<void> refreshFuture = provider.refresh(clearData: false);

    // Verify listings are still present while loading
    expect(provider.isLoading, true);
    expect(provider.listings, hasLength(1));
    expect(provider.listings.first.address, 'Initial Result');

    await refreshFuture;

    // Verify final state
    expect(provider.isLoading, false);
    expect(provider.listings, hasLength(1));
    expect(provider.listings.first.address, 'Refreshed Result');
  });

  test('listings returns cached unmodifiable instance until refresh', () async {
    final MockClient client = MockClient((request) async {
      return _listingResponse(
        items: <Map<String, dynamic>>[
          _listing(id: '1', address: 'Cached Result'),
        ],
        hasNextPage: false,
      );
    });

    final ApiService apiService = ApiService(
      client: client,
      runner: syncRunner,
      retryOptions: const RetryOptions(maxAttempts: 1),
    );

    final SearchListingsProvider provider = SearchListingsProvider(
      apiService: apiService,
    );

    // Initial load
    provider.setQuery('cache');
    await provider.refresh();

    // First access creates cache
    final list1 = provider.listings;
    // Second access returns same instance
    final list2 = provider.listings;

    expect(identical(list1, list2), isTrue);

    // Refresh invalidates cache
    await provider.refresh();
    final list3 = provider.listings;

    expect(identical(list1, list3), isFalse);
  });
}
