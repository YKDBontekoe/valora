import 'package:flutter/foundation.dart';
import '../models/listing.dart';
import '../models/listing_search_request.dart';
import '../repositories/listing_repository.dart';

class DiscoveryProvider extends ChangeNotifier {
  final ListingRepository _repository;

  DiscoveryProvider(this._repository) {
    _performSearch(); // Initial search
  }

  bool _isLoading = false;
  bool get isLoading => _isLoading;

  Object? _error;
  Object? get error => _error;

  List<Listing> _listings = [];
  List<Listing> get listings => _listings;

  // Filter State
  String? _city;
  String? get city => _city;

  double? _minPrice;
  double? get minPrice => _minPrice;

  double? _maxPrice;
  double? get maxPrice => _maxPrice;

  int? _minArea;
  int? get minArea => _minArea;

  String? _propertyType;
  String? get propertyType => _propertyType;

  String? _energyLabel;
  String? get energyLabel => _energyLabel;

  int? _minYearBuilt;
  int? get minYearBuilt => _minYearBuilt;

  String _sortBy = 'newest';
  String get sortBy => _sortBy;

  // Map Bounds State
  double? _minLat;
  double? _minLon;
  double? _maxLat;
  double? _maxLon;

  void setCity(String? city) {
    _city = city;
    _performSearch();
  }

  void setPriceRange(double? min, double? max) {
    _minPrice = min;
    _maxPrice = max;
    _performSearch();
  }

  void setMinArea(int? area) {
    _minArea = area;
    _performSearch();
  }

  void setPropertyType(String? type) {
    _propertyType = type;
    _performSearch();
  }

  void setEnergyLabel(String? label) {
    _energyLabel = label;
    _performSearch();
  }

  void setMinYearBuilt(int? year) {
    _minYearBuilt = year;
    _performSearch();
  }

  void setSortBy(String sort) {
    _sortBy = sort;
    _performSearch();
  }

  void updateMapBounds(
    double minLat,
    double minLon,
    double maxLat,
    double maxLon,
  ) {
    _minLat = minLat;
    _minLon = minLon;
    _maxLat = maxLat;
    _maxLon = maxLon;
    _performSearch();
  }

  void clearFilters() {
    _city = null;
    _minPrice = null;
    _maxPrice = null;
    _minArea = null;
    _propertyType = null;
    _energyLabel = null;
    _minYearBuilt = null;
    _minLat = null;
    _minLon = null;
    _maxLat = null;
    _maxLon = null;
    _sortBy = 'newest';
    _performSearch();
  }

  Future<void> _performSearch() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final request = ListingSearchRequest(
        city: _city,
        minPrice: _minPrice,
        maxPrice: _maxPrice,
        minArea: _minArea,
        propertyType: _propertyType,
        energyLabel: _energyLabel,
        minYearBuilt: _minYearBuilt,
        minLat: _minLat,
        minLon: _minLon,
        maxLat: _maxLat,
        maxLon: _maxLon,
        sortBy: _sortBy,
      );

      _listings = await _repository.searchListings(request);
    } catch (e) {
      _error = e;
      _listings = [];
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
}
