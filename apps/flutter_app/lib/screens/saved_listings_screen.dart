import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import '../providers/favorites_provider.dart';
import '../widgets/home_components.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/valora_filter_dialog.dart';
import 'listing_detail_screen.dart';

class SavedListingsScreen extends StatefulWidget {
  const SavedListingsScreen({super.key});

  @override
  State<SavedListingsScreen> createState() => _SavedListingsScreenState();
}

class _SavedListingsScreenState extends State<SavedListingsScreen> {
  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';

  // Advanced filters
  double? _minPrice;
  double? _maxPrice;
  String? _city;
  int? _minBedrooms;
  int? _minLivingArea;
  int? _maxLivingArea;

  // Sorting
  String? _sortBy = 'date';
  String? _sortOrder = 'desc';

  @override
  void initState() {
    super.initState();
    _searchController.addListener(() {
      setState(() {
        _searchQuery = _searchController.text.toLowerCase();
      });
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _confirmRemove(BuildContext context, Listing listing) async {
    final favoritesProvider = Provider.of<FavoritesProvider>(context, listen: false);

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => ValoraDialog(
        title: 'Remove Favorite?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(context, false),
          ),
          ValoraButton(
            label: 'Remove',
            variant: ValoraButtonVariant.primary,
            onPressed: () => Navigator.pop(context, true),
          ),
        ],
        child: const Text('Are you sure you want to remove this listing from your saved items?'),
      ),
    );

    if (confirmed == true) {
      await favoritesProvider.toggleFavorite(listing);
    }
  }

  Future<void> _openFilterDialog() async {
    final result = await showDialog<Map<String, dynamic>>(
      context: context,
      builder: (context) => ValoraFilterDialog(
        initialMinPrice: _minPrice,
        initialMaxPrice: _maxPrice,
        initialCity: _city,
        initialMinBedrooms: _minBedrooms,
        initialMinLivingArea: _minLivingArea,
        initialMaxLivingArea: _maxLivingArea,
        initialSortBy: _sortBy,
        initialSortOrder: _sortOrder,
      ),
    );

    if (!mounted) return;

    if (result != null) {
      setState(() {
        _minPrice = result['minPrice'];
        _maxPrice = result['maxPrice'];
        _city = result['city'];
        _minBedrooms = result['minBedrooms'];
        _minLivingArea = result['minLivingArea'];
        _maxLivingArea = result['maxLivingArea'];
        _sortBy = result['sortBy'];
        _sortOrder = result['sortOrder'];
      });
    }
  }

  bool get _hasActiveFilters {
    return _minPrice != null ||
        _maxPrice != null ||
        _city != null ||
        _minBedrooms != null ||
        _minLivingArea != null ||
        _maxLivingArea != null;
  }

  void _clearFilters() {
    setState(() {
      _minPrice = null;
      _maxPrice = null;
      _city = null;
      _minBedrooms = null;
      _minLivingArea = null;
      _maxLivingArea = null;
    });
  }

  String _getPriceLabel() {
    if (_minPrice != null && _maxPrice != null) {
      return 'Price: €${_minPrice!.toInt()} - €${_maxPrice!.toInt()}';
    } else if (_minPrice != null) {
      return 'Price: from €${_minPrice!.toInt()}';
    } else if (_maxPrice != null) {
      return 'Price: up to €${_maxPrice!.toInt()}';
    }
    return 'Price';
  }

  String _getLivingAreaLabel() {
    if (_minLivingArea != null && _maxLivingArea != null) {
      return '${_minLivingArea} - ${_maxLivingArea} m²';
    } else if (_minLivingArea != null) {
      return 'from ${_minLivingArea} m²';
    } else if (_maxLivingArea != null) {
      return 'up to ${_maxLivingArea} m²';
    }
    return 'Living Area';
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Consumer<FavoritesProvider>(
      builder: (context, favoritesProvider, child) {
        var listings = favoritesProvider.favorites;

        // 1. Search Filter
        if (_searchQuery.isNotEmpty) {
          listings = listings.where((l) {
            return l.address.toLowerCase().contains(_searchQuery) ||
                   (l.city?.toLowerCase().contains(_searchQuery) ?? false) ||
                   (l.postalCode?.toLowerCase().contains(_searchQuery) ?? false);
          }).toList();
        }

        // 2. Advanced Filters
        if (_minPrice != null) {
          listings = listings.where((l) => (l.price ?? 0) >= _minPrice!).toList();
        }
        if (_maxPrice != null) {
          listings = listings.where((l) => (l.price ?? 0) <= _maxPrice!).toList();
        }
        if (_city != null && _city!.isNotEmpty) {
          listings = listings.where((l) => l.city?.toLowerCase().contains(_city!.toLowerCase()) ?? false).toList();
        }
        if (_minBedrooms != null) {
          listings = listings.where((l) => (l.bedrooms ?? 0) >= _minBedrooms!).toList();
        }
        if (_minLivingArea != null) {
          listings = listings.where((l) => (l.livingAreaM2 ?? 0) >= _minLivingArea!).toList();
        }
        if (_maxLivingArea != null) {
          listings = listings.where((l) => (l.livingAreaM2 ?? 0) <= _maxLivingArea!).toList();
        }

        // 3. Sort
        listings = List.from(listings); // Copy to sort

        switch (_sortBy) {
          case 'price':
            listings.sort((a, b) {
              final priceA = a.price ?? 0;
              final priceB = b.price ?? 0;
              return _sortOrder == 'asc'
                  ? priceA.compareTo(priceB)
                  : priceB.compareTo(priceA);
            });
            break;

          case 'livingarea':
            listings.sort((a, b) {
              final areaA = a.livingAreaM2 ?? 0;
              final areaB = b.livingAreaM2 ?? 0;
              return _sortOrder == 'asc'
                  ? areaA.compareTo(areaB)
                  : areaB.compareTo(areaA);
            });
            break;

          case 'city':
            listings.sort((a, b) {
              final cityA = a.city ?? '';
              final cityB = b.city ?? '';
              return _sortOrder == 'asc'
                  ? cityA.compareTo(cityB)
                  : cityB.compareTo(cityA);
            });
            break;

          case 'date':
          default:
            // Assuming the list from provider is already in order of addition (or reverse)
            // If provider appends, then last is newest.
            // If we sort by 'desc' (newest), we reverse the list.
            if (_sortOrder == 'desc') {
               listings = listings.reversed.toList();
            }
            // If 'asc' (oldest), we keep original order.
            break;
        }

        return CustomScrollView(
          slivers: [
            SliverAppBar(
              pinned: true,
              backgroundColor: isDark
                  ? ValoraColors.backgroundDark.withValues(alpha: 0.95)
                  : ValoraColors.backgroundLight.withValues(alpha: 0.95),
              surfaceTintColor: Colors.transparent,
              title: Text(
                'Saved Listings',
                style: ValoraTypography.headlineMedium.copyWith(
                  color: isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
                  fontWeight: FontWeight.bold,
                ),
              ),
              centerTitle: false,
              actions: [
                Stack(
                  children: [
                    IconButton(
                      onPressed: _openFilterDialog,
                      icon: const Icon(Icons.tune_rounded),
                      tooltip: 'Filters',
                    ),
                    if (_hasActiveFilters)
                      Positioned(
                        top: 8,
                        right: 8,
                        child: Container(
                          width: 10,
                          height: 10,
                          decoration: const BoxDecoration(
                            color: ValoraColors.primary,
                            shape: BoxShape.circle,
                          ),
                        ),
                      ),
                  ],
                ),
                const SizedBox(width: 8),
              ],
            ),
             SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(24, 8, 24, 16),
                child: Column(
                  children: [
                    ValoraTextField(
                      controller: _searchController,
                      label: '',
                      hint: 'Search saved listings...',
                      prefixIcon: Icons.search_rounded,
                    ),
                    if (_hasActiveFilters) ...[
                      const SizedBox(height: 12),
                       SizedBox(
                        height: 40,
                        child: ListView(
                          scrollDirection: Axis.horizontal,
                          children: [
                            if (_minPrice != null || _maxPrice != null)
                              Padding(
                                padding: const EdgeInsets.only(right: 8),
                                child: ValoraChip(
                                  label: _getPriceLabel(),
                                  isSelected: true,
                                  onSelected: (_) => _openFilterDialog(),
                                ),
                              ),
                            if (_city != null)
                               Padding(
                                padding: const EdgeInsets.only(right: 8),
                                child: ValoraChip(
                                  label: 'City: $_city',
                                  isSelected: true,
                                  onSelected: (_) => _openFilterDialog(),
                                ),
                              ),
                             if (_minBedrooms != null)
                               Padding(
                                padding: const EdgeInsets.only(right: 8),
                                child: ValoraChip(
                                  label: '$_minBedrooms+ Beds',
                                  isSelected: true,
                                  onSelected: (_) => _openFilterDialog(),
                                ),
                              ),
                             if (_minLivingArea != null || _maxLivingArea != null)
                               Padding(
                                padding: const EdgeInsets.only(right: 8),
                                child: ValoraChip(
                                  label: _getLivingAreaLabel(),
                                  isSelected: true,
                                  onSelected: (_) => _openFilterDialog(),
                                ),
                              ),
                             Padding(
                                padding: const EdgeInsets.only(left: 4),
                                child: IconButton(
                                  icon: const Icon(Icons.clear_all_rounded, size: 20),
                                  tooltip: 'Clear Filters',
                                  style: IconButton.styleFrom(
                                    backgroundColor: isDark
                                      ? ValoraColors.surfaceVariantDark
                                      : ValoraColors.surfaceVariantLight,
                                  ),
                                  onPressed: _clearFilters,
                                ),
                              ),
                          ],
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ),
            if (favoritesProvider.isLoading)
              const SliverFillRemaining(
                child: Center(
                  child: CircularProgressIndicator(),
                ),
              )
            else if (listings.isEmpty)
              SliverFillRemaining(
                hasScrollBody: false,
                child: Center(
                  child: ValoraEmptyState(
                    icon: _searchQuery.isNotEmpty || _hasActiveFilters ? Icons.search_off_rounded : Icons.favorite_border_rounded,
                    title: _searchQuery.isNotEmpty || _hasActiveFilters ? 'No matches found' : 'No saved listings',
                    subtitle: _searchQuery.isNotEmpty || _hasActiveFilters
                        ? 'Try adjusting your filters or search terms.'
                        : 'Listings you save will appear here.',
                     action: _searchQuery.isNotEmpty || _hasActiveFilters
                        ? ValoraButton(
                            label: 'Clear Filters',
                            variant: ValoraButtonVariant.secondary,
                            onPressed: () {
                              _searchController.clear();
                              _clearFilters();
                            },
                          )
                        : const SizedBox.shrink(),
                  ),
                ),
              )
            else
              SliverPadding(
                padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
                sliver: SliverList(
                  delegate: SliverChildBuilderDelegate(
                    (context, index) {
                      final listing = listings[index];
                      return NearbyListingCard(
                        listing: listing,
                        onFavoriteTap: () => _confirmRemove(context, listing),
                        onTap: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => ListingDetailScreen(listing: listing),
                            ),
                          );
                        },
                      );
                    },
                    childCount: listings.length,
                  ),
                ),
              ),
            // Add extra padding at bottom
             const SliverToBoxAdapter(child: SizedBox(height: 80)),
          ],
        );
      },
    );
  }
}
