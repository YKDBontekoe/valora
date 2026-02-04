import 'dart:async';
import 'package:flutter/material.dart';
import '../core/exceptions/app_exceptions.dart';
import '../models/listing.dart';
import '../models/listing_filter.dart';
import '../services/api_service.dart';

class SearchProvider extends ChangeNotifier {
  final ApiService apiService;

  SearchProvider({required this.apiService});

  List<Listing> _listings = [];
  bool _isLoading = false;
  bool _isLoadingMore = false;
  String? _error;
  String _currentQuery = '';

  // Pagination
  int _currentPage = 1;
  bool _hasNextPage = true;
  static const int _pageSize = 20;

  // Filters
  double? _minPrice;
  double? _maxPrice;
  String? _city;
  int? _minBedrooms;
  int? _minLivingArea;
  int? _maxLivingArea;
  String? _sortBy;
  String? _sortOrder;

  List<Listing> get listings => _listings;
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  String? get error => _error;
  String get currentQuery => _currentQuery;
  bool get hasNextPage => _hasNextPage;

  // Getters for filters
  double? get minPrice => _minPrice;
  double? get maxPrice => _maxPrice;
  String? get city => _city;
  int? get minBedrooms => _minBedrooms;
  int? get minLivingArea => _minLivingArea;
  int? get maxLivingArea => _maxLivingArea;
  String? get sortBy => _sortBy;
  String? get sortOrder => _sortOrder;

  bool get hasActiveFilters =>
      _minPrice != null ||
      _maxPrice != null ||
      _city != null ||
      _minBedrooms != null ||
      _minLivingArea != null ||
      _maxLivingArea != null ||
      _sortBy != null ||
      _sortOrder != null;

  Timer? _debounce;

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }

  void setQuery(String query) {
    if (query == _currentQuery) return;
    _currentQuery = query;

    if (_debounce?.isActive ?? false) _debounce!.cancel();
    _debounce = Timer(const Duration(milliseconds: 500), () {
      loadListings(refresh: true);
    });
  }

  Future<void> loadListings({bool refresh = false}) async {
    if (refresh) {
      _isLoading = true;
      _error = null;
      _currentPage = 1;
      if (refresh) _listings = [];
      notifyListeners();
    }

    if (_currentQuery.isEmpty && !hasActiveFilters) {
      _listings = [];
      _isLoading = false;
      _hasNextPage = false;
      notifyListeners();
      return;
    }

    try {
      final response = await apiService.getListings(
        ListingFilter(
          searchTerm: _currentQuery,
          page: _currentPage,
          pageSize: _pageSize,
          minPrice: _minPrice,
          maxPrice: _maxPrice,
          city: _city,
          minBedrooms: _minBedrooms,
          minLivingArea: _minLivingArea,
          maxLivingArea: _maxLivingArea,
          sortBy: _sortBy,
          sortOrder: _sortOrder,
        ),
      );

      if (refresh) {
        _listings = response.items;
      } else {
        _listings.addAll(response.items);
      }
      _hasNextPage = response.hasNextPage;
      _isLoading = false;
      notifyListeners();
    } catch (e) {
      _error = e is AppException ? e.message : 'Failed to search listings';
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadMoreListings() async {
    if (_isLoading || _isLoadingMore || !_hasNextPage) return;

    _isLoadingMore = true;
    notifyListeners();

    final nextPage = _currentPage + 1;
    try {
       final response = await apiService.getListings(
        ListingFilter(
          searchTerm: _currentQuery,
          page: nextPage,
          pageSize: _pageSize,
          minPrice: _minPrice,
          maxPrice: _maxPrice,
          city: _city,
          minBedrooms: _minBedrooms,
          minLivingArea: _minLivingArea,
          maxLivingArea: _maxLivingArea,
          sortBy: _sortBy,
          sortOrder: _sortOrder,
        ),
      );

      _currentPage = nextPage;
      _listings.addAll(response.items);
      _hasNextPage = response.hasNextPage;
    } catch (e) {
       // Ideally we might want to expose this error specifically for the "load more" action
       // For now, we just swallow it or maybe set a transient error state?
       // Let's just log it effectively by doing nothing (UI handles silence) or maybe a toast triggers from UI side if they listen.
    } finally {
      _isLoadingMore = false;
      notifyListeners();
    }
  }
    }
  }

  void updateFilters({
    double? minPrice,
    double? maxPrice,
    String? city,
    int? minBedrooms,
    int? minLivingArea,
    int? maxLivingArea,
    String? sortBy,
    String? sortOrder,
  }) {
    _minPrice = minPrice;
    _maxPrice = maxPrice;
    _city = city;
    _minBedrooms = minBedrooms;
    _minLivingArea = minLivingArea;
    _maxLivingArea = maxLivingArea;
    _sortBy = sortBy;
    _sortOrder = sortOrder;

    loadListings(refresh: true);
  }
}
