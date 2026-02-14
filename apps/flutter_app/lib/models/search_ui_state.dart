import 'package:flutter/foundation.dart';

import 'listing.dart';

enum SearchViewStatus { idle, loading, results, empty, error }

@immutable
class SearchUiState {
  const SearchUiState({
    required this.status,
    required this.query,
    required this.listings,
    required this.hasNextPage,
    required this.isLoadingMore,
    required this.hasActiveFilters,
    required this.hasActiveFiltersOrSort,
    this.errorMessage,
    this.loadMoreErrorMessage,
  });

  const SearchUiState.initial()
    : status = SearchViewStatus.idle,
      query = '',
      listings = const <Listing>[],
      hasNextPage = false,
      isLoadingMore = false,
      hasActiveFilters = false,
      hasActiveFiltersOrSort = false,
      errorMessage = null,
      loadMoreErrorMessage = null;

  final SearchViewStatus status;
  final String query;
  final List<Listing> listings;
  final bool hasNextPage;
  final bool isLoadingMore;
  final bool hasActiveFilters;
  final bool hasActiveFiltersOrSort;
  final String? errorMessage;
  final String? loadMoreErrorMessage;

  bool get showIdleState =>
      status == SearchViewStatus.idle && !hasActiveFilters && query.isEmpty;

  bool get showNoResults => status == SearchViewStatus.empty;

  SearchUiState copyWith({
    SearchViewStatus? status,
    String? query,
    List<Listing>? listings,
    bool? hasNextPage,
    bool? isLoadingMore,
    bool? hasActiveFilters,
    bool? hasActiveFiltersOrSort,
    String? errorMessage,
    String? loadMoreErrorMessage,
    bool clearErrorMessage = false,
    bool clearLoadMoreErrorMessage = false,
  }) {
    return SearchUiState(
      status: status ?? this.status,
      query: query ?? this.query,
      listings: listings ?? this.listings,
      hasNextPage: hasNextPage ?? this.hasNextPage,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      hasActiveFilters: hasActiveFilters ?? this.hasActiveFilters,
      hasActiveFiltersOrSort:
          hasActiveFiltersOrSort ?? this.hasActiveFiltersOrSort,
      errorMessage: clearErrorMessage ? null : (errorMessage ?? this.errorMessage),
      loadMoreErrorMessage:
          clearLoadMoreErrorMessage
              ? null
              : (loadMoreErrorMessage ?? this.loadMoreErrorMessage),
    );
  }
}
