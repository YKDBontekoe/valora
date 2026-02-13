import 'dart:convert';
import 'dart:developer' as developer;

import 'package:flutter/foundation.dart';

import '../core/exceptions/app_exceptions.dart';
import '../models/listing.dart';
import '../models/listing_filter.dart';
import '../services/api_service.dart';

class SearchListingsProvider extends ChangeNotifier {
  SearchListingsProvider({required ApiService apiService})
    : _apiService = apiService;

  static const int pageSize = 20;

  final ApiService _apiService;

  final List<Listing> _listings = <Listing>[];
  List<Listing>? _cachedListings;

  bool _isLoading = false;
  bool _isLoadingMore = false;
  String? _error;
  String _query = '';
  int _currentPage = 1;
  bool _hasNextPage = false;
  int _requestSequence = 0;

  double? _minPrice;
  double? _maxPrice;
  String? _city;
  int? _minBedrooms;
  int? _minLivingArea;
  int? _maxLivingArea;
  double? _minCompositeScore;
  double? _minSafetyScore;
  String? _sortBy;
  String? _sortOrder;

  List<Listing> get listings => _cachedListings ??= List<Listing>.unmodifiable(_listings);
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  String? get error => _error;
  bool get hasNextPage => _hasNextPage;
  String get query => _query;
  double? get minPrice => _minPrice;
  double? get maxPrice => _maxPrice;
  String? get city => _city;
  int? get minBedrooms => _minBedrooms;
  int? get minLivingArea => _minLivingArea;
  int? get maxLivingArea => _maxLivingArea;
  double? get minCompositeScore => _minCompositeScore;
  double? get minSafetyScore => _minSafetyScore;
  String? get sortBy => _sortBy;
  String? get sortOrder => _sortOrder;

  bool get hasActiveFilters {
    return _minPrice != null ||
        _maxPrice != null ||
        _city != null ||
        _minBedrooms != null ||
        _minLivingArea != null ||
        _maxLivingArea != null ||
        _minCompositeScore != null ||
        _minSafetyScore != null;
  }

  bool get hasActiveFiltersOrSort {
    return hasActiveFilters || isSortActive;
  }

  bool get isSortActive {
    return _sortBy != null && (_sortBy != 'date' || _sortOrder != 'desc');
  }

  void setQuery(String value) {
    _query = value;
  }

  void setCity(String? value) {
    _city = value;
    notifyListeners();
  }

  Future<void> refresh({bool clearData = true}) async {
    await _loadListings(refresh: true, clearData: clearData);
  }

  Future<void> loadMore() async {
    if (_isLoading || _isLoadingMore || !_hasNextPage) {
      return;
    }

    _isLoadingMore = true;
    notifyListeners();

    final int nextPage = _currentPage + 1;
    final int requestId = ++_requestSequence;

    try {
      final response = await _apiService.getListings(
        _buildFilter(page: nextPage),
      );
      if (requestId != _requestSequence) {
        return;
      }

      _currentPage = nextPage;
      _listings.addAll(response.items);
      _cachedListings = null;
      _hasNextPage = response.hasNextPage;
      _error = null;
    } catch (e) {
      if (requestId != _requestSequence) {
        return;
      }
      _error = e is AppException ? e.message : 'Failed to load more items';
      _logProviderFailure(operation: 'pagination', error: e);
    } finally {
      if (requestId == _requestSequence) {
        _isLoadingMore = false;
        notifyListeners();
      }
    }
  }

  Future<void> applyFilters({
    required double? minPrice,
    required double? maxPrice,
    required String? city,
    required int? minBedrooms,
    required int? minLivingArea,
    required int? maxLivingArea,
    required double? minCompositeScore,
    required double? minSafetyScore,
    required String? sortBy,
    required String? sortOrder,
  }) async {
    _minPrice = minPrice;
    _maxPrice = maxPrice;
    _city = city;
    _minBedrooms = minBedrooms;
    _minLivingArea = minLivingArea;
    _maxLivingArea = maxLivingArea;
    _minCompositeScore = minCompositeScore;
    _minSafetyScore = minSafetyScore;
    _sortBy = sortBy;
    _sortOrder = sortOrder;
    notifyListeners();
    await refresh(clearData: true);
  }

  Future<void> clearPriceFilter() async {
    _minPrice = null;
    _maxPrice = null;
    notifyListeners();
    await refresh();
  }

  Future<void> clearCityFilter() async {
    _city = null;
    notifyListeners();
    await refresh();
  }

  Future<void> clearBedroomsFilter() async {
    _minBedrooms = null;
    notifyListeners();
    await refresh();
  }

  Future<void> clearLivingAreaFilter() async {
    _minLivingArea = null;
    _maxLivingArea = null;
    notifyListeners();
    await refresh();
  }

  Future<void> clearCompositeScoreFilter() async {
    _minCompositeScore = null;
    notifyListeners();
    await refresh();
  }

  Future<void> clearSafetyScoreFilter() async {
    _minSafetyScore = null;
    notifyListeners();
    await refresh();
  }

  Future<void> clearSort() async {
    _sortBy = null;
    _sortOrder = null;
    notifyListeners();
    await refresh();
  }

  Future<void> clearFilters() async {
    _minPrice = null;
    _maxPrice = null;
    _city = null;
    _minBedrooms = null;
    _minLivingArea = null;
    _maxLivingArea = null;
    _minCompositeScore = null;
    _minSafetyScore = null;
    _sortBy = null;
    _sortOrder = null;
    notifyListeners();
    await refresh();
  }

  Future<void> _loadListings({required bool refresh, bool clearData = true}) async {
    final int requestId = ++_requestSequence;

    if (refresh) {
      _isLoading = true;
      _error = null;
      _currentPage = 1;
      _hasNextPage = false;
      if (clearData) {
        _listings.clear();
        _cachedListings = null;
      }
      notifyListeners();
    }

    if (_query.isEmpty && !hasActiveFiltersOrSort) {
      if (requestId != _requestSequence) return;

      _isLoading = false;
      _isLoadingMore = false;
      _error = null;
      _listings.clear();
      _cachedListings = null;
      _currentPage = 1;
      _hasNextPage = false;
      notifyListeners();
      return;
    }

    try {
      final response = await _apiService.getListings(
        _buildFilter(page: _currentPage),
      );
      if (requestId != _requestSequence) {
        return;
      }

      _listings
        ..clear()
        ..addAll(response.items);
      _cachedListings = null;
      _hasNextPage = response.hasNextPage;
      _error = null;
    } catch (e) {
      if (requestId != _requestSequence) {
        return;
      }
      _error = e is AppException ? e.message : 'Failed to search listings';
      _logProviderFailure(operation: 'search', error: e);
    } finally {
      if (requestId == _requestSequence) {
        _isLoading = false;
        _isLoadingMore = false;
        notifyListeners();
      }
    }
  }

  ListingFilter _buildFilter({required int page}) {
    return ListingFilter(
      searchTerm: _query,
      page: page,
      pageSize: pageSize,
      minPrice: _minPrice,
      maxPrice: _maxPrice,
      city: _city,
      minBedrooms: _minBedrooms,
      minLivingArea: _minLivingArea,
      maxLivingArea: _maxLivingArea,
      minSafetyScore: _minSafetyScore,
      minCompositeScore: _minCompositeScore,
      sortBy: _sortBy,
      sortOrder: _sortOrder,
    );
  }

  void _logProviderFailure({required String operation, required Object error}) {
    developer.log(
      jsonEncode(<String, String>{
        'event': 'provider_failure',
        'provider': 'SearchListingsProvider',
        'operation': operation,
        'errorType': error.runtimeType.toString(),
      }),
      name: 'SearchListingsProvider',
    );
  }
}
