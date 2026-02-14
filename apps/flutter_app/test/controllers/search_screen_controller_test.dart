import 'dart:convert';

import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:retry/retry.dart';
import 'package:valora_app/controllers/search_screen_controller.dart';
import 'package:valora_app/providers/search_listings_provider.dart';
import 'package:valora_app/services/api_service.dart';

import '../helpers/test_runners.dart';

http.Response _response({
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

Map<String, dynamic> _listing(String id) => <String, dynamic>{
  'id': id,
  'fundaId': id,
  'address': 'Address $id',
  'city': 'Amsterdam',
  'price': 100000,
};

void main() {
  setUpAll(() async {
    await dotenv.load(fileName: '.env.example');
  });

  test('uses 750ms debounce before refreshing query', () async {
    final List<String> capturedTerms = <String>[];
    final MockClient client = MockClient((request) async {
      capturedTerms.add(request.url.queryParameters['searchTerm'] ?? '');
      return _response(
        items: <Map<String, dynamic>>[_listing('1')],
        hasNextPage: false,
      );
    });

    final SearchListingsProvider provider = SearchListingsProvider(
      apiService: ApiService(
        client: client,
        runner: syncRunner,
        retryOptions: const RetryOptions(maxAttempts: 1),
      ),
    );
    final SearchScreenController controller = SearchScreenController(
      searchProvider: provider,
    );

    controller.onQueryChanged('ams');
    await Future<void>.delayed(const Duration(milliseconds: 700));
    expect(capturedTerms, isEmpty);

    await Future<void>.delayed(const Duration(milliseconds: 80));
    expect(capturedTerms, <String>['ams']);

    controller.dispose();
  });

  test('uses 200px threshold for infinite scroll trigger', () async {
    final MockClient client = MockClient((request) async {
      return _response(
        items: <Map<String, dynamic>>[_listing('1')],
        hasNextPage: true,
      );
    });

    final SearchListingsProvider provider = SearchListingsProvider(
      apiService: ApiService(
        client: client,
        runner: syncRunner,
        retryOptions: const RetryOptions(maxAttempts: 1),
      ),
    );
    final SearchScreenController controller = SearchScreenController(
      searchProvider: provider,
    );

    provider.setQuery('amsterdam');
    await provider.refresh();

    expect(controller.shouldLoadMore(offset: 799, maxExtent: 1000), isFalse);
    expect(controller.shouldLoadMore(offset: 800, maxExtent: 1000), isTrue);

    controller.dispose();
  });

  test('emits load more error as transient ui effect', () async {
    int callCount = 0;
    final MockClient client = MockClient((request) async {
      callCount += 1;
      final int page = int.parse(request.url.queryParameters['page'] ?? '1');
      if (page == 1) {
        return _response(
          items: <Map<String, dynamic>>[_listing('1')],
          hasNextPage: true,
        );
      }
      return http.Response('fail', 500);
    });

    final SearchListingsProvider provider = SearchListingsProvider(
      apiService: ApiService(
        client: client,
        runner: syncRunner,
        retryOptions: const RetryOptions(maxAttempts: 1),
      ),
    );
    final SearchScreenController controller = SearchScreenController(
      searchProvider: provider,
    );

    provider.setQuery('amsterdam');
    await provider.refresh();
    await controller.loadMoreIfNeeded();

    expect(callCount, 2);
    expect(controller.state.loadMoreErrorMessage, 'Failed to load more items');

    controller.consumeLoadMoreError();
    expect(controller.state.loadMoreErrorMessage, isNull);

    controller.dispose();
  });
}
