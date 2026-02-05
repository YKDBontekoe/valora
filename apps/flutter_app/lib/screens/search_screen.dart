import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../core/exceptions/app_exceptions.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import '../models/listing_filter.dart';
import '../services/api_service.dart';
import '../widgets/home_components.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/valora_filter_dialog.dart';
import 'listing_detail_screen.dart';

class SearchScreen extends StatefulWidget {
  const SearchScreen({super.key});

  @override
  State<SearchScreen> createState() => _SearchScreenState();
}

class _SearchScreenState extends State<SearchScreen> {
  final TextEditingController _searchController = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  Timer? _debounce;

  ApiService? _apiService;
  List<Listing> _listings = [];
  bool _isLoading = false;
  bool _isLoadingMore = false;
  String? _error;
  String _currentQuery = '';

  // Filters & Pagination
  int _currentPage = 1;
  bool _hasNextPage = true;
  static const int _pageSize = 20;

  double? _minPrice;
  double? _maxPrice;
  String? _city;
  int? _minBedrooms;
  int? _minLivingArea;
  int? _maxLivingArea;
  String? _sortBy;
  String? _sortOrder;

  @override
  void initState() {
    super.initState();
    _searchController.addListener(_onSearchChanged);
    _scrollController.addListener(_onScroll);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _apiService ??= Provider.of<ApiService>(context);
  }

  @override
  void dispose() {
    _searchController.dispose();
    _scrollController.dispose();
    _debounce?.cancel();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollController.position.pixels >= _scrollController.position.maxScrollExtent - 200 &&
        !_isLoadingMore &&
        _hasNextPage &&
        _error == null &&
        (_listings.isNotEmpty || _currentQuery.isNotEmpty)) { // Only load more if we have initial results or a query
      _loadMoreListings();
    }
  }

  void _onSearchChanged() {
    final query = _searchController.text;
    if (query == _currentQuery) return;
    _currentQuery = query;

    if (_debounce?.isActive ?? false) _debounce!.cancel();
    _debounce = Timer(const Duration(milliseconds: 500), () {
      _loadListings(refresh: true);
    });
  }

  Future<void> _loadListings({bool refresh = false}) async {
    if (refresh) {
      setState(() {
        _isLoading = true;
        _error = null;
        _currentPage = 1;
        if (refresh) _listings = [];
      });
    }

    // Don't search if query is empty and no filters are set
    // Exception: If we just want to explore listings, we might allow empty search
    // But typically search screen starts empty.
    if (_currentQuery.isEmpty && !_hasActiveFilters) {
      setState(() {
        _listings = [];
        _isLoading = false;
        _hasNextPage = false;
      });
      return;
    }

    try {
      final response = await _apiService!.getListings(
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

      if (mounted) {
        setState(() {
          if (refresh) {
            _listings = response.items;
          } else {
            _listings.addAll(response.items);
          }
          _hasNextPage = response.hasNextPage;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e is AppException ? e.message : 'Failed to search listings';
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _loadMoreListings() async {
    if (_isLoadingMore) return;
    setState(() => _isLoadingMore = true);
    _currentPage++;
    try {
       final response = await _apiService!.getListings(
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
      if (mounted) {
        setState(() {
          _listings.addAll(response.items);
          _hasNextPage = response.hasNextPage;
        });
      }
    } catch (e) {
      // Quietly fail pagination or show snackbar
      if (mounted) {
         ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to load more items')),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isLoadingMore = false);
      }
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
      _loadListings(refresh: true);
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

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      body: RefreshIndicator(
        onRefresh: () => _loadListings(refresh: true),
        child: CustomScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          controller: _scrollController,
          slivers: [
            SliverAppBar(
              pinned: true,
            backgroundColor: isDark
                ? ValoraColors.backgroundDark.withValues(alpha: 0.95)
                : ValoraColors.backgroundLight.withValues(alpha: 0.95),
            surfaceTintColor: Colors.transparent,
            title: Text(
              'Search',
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
            bottom: PreferredSize(
              preferredSize: Size.fromHeight(_hasActiveFilters ? 130 : 80),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Padding(
                    padding: const EdgeInsets.fromLTRB(24, 0, 24, 16),
                    child: ValoraTextField(
                      controller: _searchController,
                      label: '',
                      hint: 'City, address, or zip code...',
                      prefixIcon: Icons.search_rounded,
                      textInputAction: TextInputAction.search,
                    ),
                  ),
                  if (_hasActiveFilters)
                    SizedBox(
                      height: 40,
                      child: ListView(
                        scrollDirection: Axis.horizontal,
                        padding: const EdgeInsets.symmetric(horizontal: 24),
                        children: [
                          if (_minPrice != null || _maxPrice != null)
                            Padding(
                              padding: const EdgeInsets.only(right: 8),
                              child: ValoraChip(
                                label: 'Price: €${_minPrice?.toInt() ?? 0} - ${_maxPrice != null ? '€${_maxPrice!.toInt()}' : 'Any'}',
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
                           if (_minLivingArea != null)
                             Padding(
                              padding: const EdgeInsets.only(right: 8),
                              child: ValoraChip(
                                label: '$_minLivingArea+ m²',
                                isSelected: true,
                                onSelected: (_) => _openFilterDialog(),
                              ),
                            ),
                            if (_hasActiveFilters)
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
                                  onPressed: () {
                                    setState(() {
                                      _minPrice = null;
                                      _maxPrice = null;
                                      _city = null;
                                      _minBedrooms = null;
                                      _minLivingArea = null;
                                      _maxLivingArea = null;
                                    });
                                    _loadListings(refresh: true);
                                  },
                                ),
                              ),
                        ],
                      ),
                    ),
                  if (_hasActiveFilters) const SizedBox(height: 12),
                ],
              ),
            ),
          ),

          if (_isLoading)
            const SliverFillRemaining(
              child: ValoraLoadingIndicator(message: 'Searching...'),
            )
          else if (_error != null)
             SliverFillRemaining(
              hasScrollBody: false,
              child: Center(
                child: ValoraEmptyState(
                  icon: Icons.error_outline_rounded,
                  title: 'Search Failed',
                  subtitle: _error,
                  action: ValoraButton(
                    label: 'Retry',
                    onPressed: () => _loadListings(refresh: true),
                  ),
                ),
              ),
            )
          else if (_listings.isEmpty && (_currentQuery.isNotEmpty || _hasActiveFilters))
            const SliverFillRemaining(
              hasScrollBody: false,
              child: Center(
                child: ValoraEmptyState(
                  icon: Icons.search_off_rounded,
                  title: 'No results found',
                  subtitle: 'Try adjusting your filters or search terms.',
                ),
              ),
            )
          else if (_listings.isEmpty && _currentQuery.isEmpty && !_hasActiveFilters)
             const SliverFillRemaining(
              hasScrollBody: false,
              child: Center(
                child: ValoraEmptyState(
                  icon: Icons.search_rounded,
                  title: 'Find your home',
                  subtitle: 'Enter a location or use filters to start searching.',
                ),
              ),
            )
          else
            SliverPadding(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
              sliver: SliverList(
                delegate: SliverChildBuilderDelegate(
                  (context, index) {
                    if (index == _listings.length) {
                       if (_isLoadingMore) {
                          return const Padding(
                            padding: EdgeInsets.symmetric(vertical: 24),
                            child: Center(child: CircularProgressIndicator()),
                          );
                       } else {
                         return const SizedBox(height: 80);
                       }
                    }

                    final listing = _listings[index];
                    return NearbyListingCard(
                      listing: listing,
                      onTap: () {
                        Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) => ListingDetailScreen(listing: listing),
                          ),
                        );
                      },
                    )
                    .animate(delay: (50 * (index % 10)).ms)
                    .fade(duration: 400.ms)
                    .slideY(begin: 0.1, end: 0, curve: Curves.easeOut);
                  },
                  childCount: _listings.length + 1, // +1 for loader/padding
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
