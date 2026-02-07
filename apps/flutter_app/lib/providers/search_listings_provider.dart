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
  String? _sortBy;
  String? _sortOrder;

  List<Listing> get listings => List<Listing>.unmodifiable(_listings);
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
  String? get sortBy => _sortBy;
  String? get sortOrder => _sortOrder;

  bool get hasActiveFilters {
    return _minPrice != null ||
        _maxPrice != null ||
        _city != null ||
        _minBedrooms != null ||
        _minLivingArea != null ||
        _maxLivingArea != null;
  }

  void setQuery(String value) {
    _query = value;
  }

  Future<void> refresh() async {
    await _loadListings(refresh: true);
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
      _hasNextPage = response.hasNextPage;
      _error = null;
    } catch (e, stackTrace) {
      if (requestId != _requestSequence) {
        return;
      }
      _error = e is AppException ? e.message : 'Failed to load more items';
      developer.log(
        'Pagination failed',
        name: 'SearchListingsProvider',
        error: e,
        stackTrace: stackTrace,
      );
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
    required String? sortBy,
    required String? sortOrder,
  }) async {
    _minPrice = minPrice;
    _maxPrice = maxPrice;
    _city = city;
    _minBedrooms = minBedrooms;
    _minLivingArea = minLivingArea;
    _maxLivingArea = maxLivingArea;
    _sortBy = sortBy;
    _sortOrder = sortOrder;
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
    _sortBy = null;
    _sortOrder = null;
    notifyListeners();
    await refresh();
  }

  Future<void> _loadListings({required bool refresh}) async {
    if (refresh) {
      _isLoading = true;
      _error = null;
      _currentPage = 1;
      _hasNextPage = false;
      _listings.clear();
      notifyListeners();
    }

    if (_query.isEmpty && !hasActiveFilters) {
      _isLoading = false;
      _isLoadingMore = false;
      _error = null;
      _listings.clear();
      _currentPage = 1;
      _hasNextPage = false;
      notifyListeners();
      return;
    }

    final int requestId = ++_requestSequence;

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
      _hasNextPage = response.hasNextPage;
      _error = null;
    } catch (e, stackTrace) {
      if (requestId != _requestSequence) {
        return;
      }
      _error = e is AppException ? e.message : 'Failed to search listings';
      developer.log(
        'Search failed',
        name: 'SearchListingsProvider',
        error: e,
        stackTrace: stackTrace,
      );
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
      sortBy: _sortBy,
      sortOrder: _sortOrder,
    );
  }
}
