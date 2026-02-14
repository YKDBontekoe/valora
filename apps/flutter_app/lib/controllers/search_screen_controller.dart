import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/listing_filter.dart';
import '../models/search_ui_state.dart';
import '../providers/search_listings_provider.dart';

class SearchScreenController extends ChangeNotifier {
  SearchScreenController({required SearchListingsProvider searchProvider})
    : _searchProvider = searchProvider {
    _searchProvider.addListener(_syncFromProvider);
    _syncFromProvider();
  }

  static const Duration queryDebounceDuration = Duration(milliseconds: 750);
  static const double loadMoreThresholdPixels = 200;

  final SearchListingsProvider _searchProvider;
  Timer? _debounce;

  SearchUiState _state = const SearchUiState.initial();

  SearchUiState get state => _state;
  SearchListingsProvider get searchProvider => _searchProvider;

  void onQueryChanged(String query) {
    if (query == _searchProvider.query) {
      return;
    }

    _searchProvider.setQuery(query);
    _debounce?.cancel();
    _debounce = Timer(queryDebounceDuration, () {
      _searchProvider.refresh();
    });
  }

  void onQuerySubmitted() {
    _debounce?.cancel();
    _searchProvider.refresh();
  }

  Future<void> refresh({bool clearData = true}) {
    return _searchProvider.refresh(clearData: clearData);
  }

  Future<void> applyFilters(Map<String, dynamic> rawFilterResult) {
    return _searchProvider.applyFilters(
      minPrice: rawFilterResult['minPrice'] as double?,
      maxPrice: rawFilterResult['maxPrice'] as double?,
      city: rawFilterResult['city'] as String?,
      minBedrooms: rawFilterResult['minBedrooms'] as int?,
      minLivingArea: rawFilterResult['minLivingArea'] as int?,
      maxLivingArea: rawFilterResult['maxLivingArea'] as int?,
      minSafetyScore: rawFilterResult['minSafetyScore'] as double?,
      minCompositeScore: rawFilterResult['minCompositeScore'] as double?,
      sortBy: rawFilterResult['sortBy'] as String?,
      sortOrder: rawFilterResult['sortOrder'] as String?,
    );
  }

  bool shouldLoadMore({required double offset, required double maxExtent}) {
    return offset >= maxExtent - loadMoreThresholdPixels &&
        !_searchProvider.isLoadingMore &&
        _searchProvider.hasNextPage &&
        (_searchProvider.listings.isNotEmpty || _searchProvider.query.isNotEmpty);
  }

  Future<void> loadMoreIfNeeded() async {
    if (_searchProvider.isLoading || _searchProvider.isLoadingMore) {
      return;
    }

    final String? previousError = _searchProvider.error;
    await _searchProvider.loadMore();

    final String? currentError = _searchProvider.error;
    if (currentError != null && currentError != previousError) {
      _state = _state.copyWith(loadMoreErrorMessage: 'Failed to load more items');
      notifyListeners();
    }
  }

  void setCityFilter(String? city) {
    _searchProvider.setCity(city);
  }

  void setQuery(String query) {
    _searchProvider.setQuery(query);
  }

  void consumeLoadMoreError() {
    if (_state.loadMoreErrorMessage == null) {
      return;
    }
    _state = _state.copyWith(clearLoadMoreErrorMessage: true);
    notifyListeners();
  }

  ListingFilter get currentFilter => ListingFilter(
    searchTerm: _searchProvider.query,
    minPrice: _searchProvider.minPrice,
    maxPrice: _searchProvider.maxPrice,
    city: _searchProvider.city,
    minBedrooms: _searchProvider.minBedrooms,
    minLivingArea: _searchProvider.minLivingArea,
    maxLivingArea: _searchProvider.maxLivingArea,
    minSafetyScore: _searchProvider.minSafetyScore,
    minCompositeScore: _searchProvider.minCompositeScore,
    sortBy: _searchProvider.sortBy,
    sortOrder: _searchProvider.sortOrder,
  );

  void _syncFromProvider() {
    final bool hasSearchInput =
        _searchProvider.query.isNotEmpty || _searchProvider.hasActiveFilters;

    SearchViewStatus status = SearchViewStatus.results;
    if (_searchProvider.isLoading) {
      status = SearchViewStatus.loading;
    } else if (_searchProvider.error != null && _searchProvider.listings.isEmpty) {
      status = SearchViewStatus.error;
    } else if (_searchProvider.listings.isEmpty) {
      status = hasSearchInput ? SearchViewStatus.empty : SearchViewStatus.idle;
    }

    _state = _state.copyWith(
      status: status,
      query: _searchProvider.query,
      listings: _searchProvider.listings,
      hasNextPage: _searchProvider.hasNextPage,
      isLoadingMore: _searchProvider.isLoadingMore,
      hasActiveFilters: _searchProvider.hasActiveFilters,
      hasActiveFiltersOrSort: _searchProvider.hasActiveFiltersOrSort,
      errorMessage: _searchProvider.error,
      clearLoadMoreErrorMessage: _searchProvider.isLoadingMore,
    );

    notifyListeners();
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _searchProvider.removeListener(_syncFromProvider);
    super.dispose();
  }
}
