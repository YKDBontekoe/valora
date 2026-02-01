import 'package:flutter/material.dart';
import '../models/filter_chip_model.dart';
import '../models/listing_filter.dart';

class HomeProvider extends ChangeNotifier {
  static const double _maxQuickPrice = 500000;
  static const int _minQuickBedrooms = 3;

  final Set<HomeQuickFilter> _activeQuickFilters = {};

  List<ValoraFilterChipModel> get quickFilterChips {
    return [
      ValoraFilterChipModel(
        filter: HomeQuickFilter.aiPick,
        label: 'AI Pick',
        icon: Icons.auto_awesome,
        isActive: _activeQuickFilters.contains(HomeQuickFilter.aiPick),
      ),
      ValoraFilterChipModel(
        filter: HomeQuickFilter.under500k,
        label: 'Under \$500k',
        isActive: _activeQuickFilters.contains(HomeQuickFilter.under500k),
      ),
      ValoraFilterChipModel(
        filter: HomeQuickFilter.threePlusBeds,
        label: '3+ Beds',
        isActive: _activeQuickFilters.contains(HomeQuickFilter.threePlusBeds),
      ),
      ValoraFilterChipModel(
        filter: HomeQuickFilter.nearSchools,
        label: 'Near Schools',
        isActive: _activeQuickFilters.contains(HomeQuickFilter.nearSchools),
      ),
    ];
  }

  bool isQuickFilterActive(HomeQuickFilter filter) {
    return _activeQuickFilters.contains(filter);
  }

  int get activeQuickFilterCount => _activeQuickFilters.length;

  void toggleQuickFilter(HomeQuickFilter filter) {
    if (_activeQuickFilters.contains(filter)) {
      _activeQuickFilters.remove(filter);
    } else {
      _activeQuickFilters.add(filter);
    }
    notifyListeners();
  }

  ListingFilter applyQuickFilters(ListingFilter baseFilter) {
    double? maxPrice = baseFilter.maxPrice;
    if (isQuickFilterActive(HomeQuickFilter.under500k)) {
      if (maxPrice == null || maxPrice > _maxQuickPrice) {
        maxPrice = _maxQuickPrice;
      }
    }

    int? minBedrooms = baseFilter.minBedrooms;
    if (isQuickFilterActive(HomeQuickFilter.threePlusBeds)) {
      if (minBedrooms == null || minBedrooms < _minQuickBedrooms) {
        minBedrooms = _minQuickBedrooms;
      }
    }

    return baseFilter.copyWith(
      maxPrice: maxPrice,
      minBedrooms: minBedrooms,
    );
  }
}
