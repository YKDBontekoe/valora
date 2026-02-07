import 'dart:developer' as developer;

import 'package:flutter/foundation.dart';

import '../models/listing.dart';
import '../models/listing_filter.dart';
import '../services/api_service.dart';

class HomeListingsProvider extends ChangeNotifier {
  HomeListingsProvider({required ApiService apiService})
    : _apiService = apiService;

  static const int pageSize = 10;

  final ApiService _apiService;

  final List<Listing> _listings = <Listing>[];
  bool _isConnected = false;
  bool _isLoading = true;
  bool _isLoadingMore = false;
  Object? _error;
  int _currentPage = 1;
  bool _hasNextPage = true;
  int _requestSequence = 0;

  String _searchTerm = '';
  double? _minPrice;
  double? _maxPrice;
  String? _city;
  int? _minBedrooms;
  int? _minLivingArea;
  int? _maxLivingArea;
  String? _sortBy;
  String? _sortOrder;

  List<Listing> get listings => List<Listing>.unmodifiable(_listings);
  bool get isConnected => _isConnected;
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  Object? get error => _error;
  bool get hasNextPage => _hasNextPage;
  String get searchTerm => _searchTerm;
  double? get minPrice => _minPrice;
  double? get maxPrice => _maxPrice;
  String? get city => _city;
  int? get minBedrooms => _minBedrooms;
  int? get minLivingArea => _minLivingArea;
  int? get maxLivingArea => _maxLivingArea;
  String? get sortBy => _sortBy;
  String? get sortOrder => _sortOrder;

  int get activeFilterCount {
    int count = 0;
    if (_minPrice != null) count++;
    if (_maxPrice != null) count++;
    if (_city != null) count++;
    if (_minBedrooms != null) count++;
    if (_minLivingArea != null) count++;
    if (_maxLivingArea != null) count++;
    return count;
  }

  bool get hasFilters => activeFilterCount > 0 || _searchTerm.isNotEmpty;

  Future<void> initialize() async {
    await checkConnectionAndLoad();
  }

  Future<void> checkConnectionAndLoad() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    final connected = await _apiService.healthCheck();
    _isConnected = connected;

    if (!connected) {
      _isLoading = false;
      notifyListeners();
      return;
    }

    await refresh();
  }

  Future<void> refresh() async {
    await _loadListings(refresh: true);
  }

  Future<void> loadMore() async {
    if (_isLoading || _isLoadingMore || !_hasNextPage || _error != null) {
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
      _error = e;
      developer.log(
        'Home pagination failed',
        name: 'HomeListingsProvider',
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

  Future<void> setSearchTerm(String value) async {
    _searchTerm = value;
    notifyListeners();
    await refresh();
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

  Future<void> clearFiltersAndSearch() async {
    _searchTerm = '';
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

  Future<void> triggerScrape({
    required String region,
    required int limit,
  }) async {
    await _apiService.triggerLimitedScrape(region, limit);
  }

  Future<void> _loadListings({required bool refresh}) async {
    if (refresh) {
      _isLoading = true;
      _error = null;
      _currentPage = 1;
      _listings.clear();
      _hasNextPage = true;
      notifyListeners();
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
      _error = e;
      developer.log(
        'Home listings load failed',
        name: 'HomeListingsProvider',
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
      page: page,
      pageSize: pageSize,
      searchTerm: _searchTerm,
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
