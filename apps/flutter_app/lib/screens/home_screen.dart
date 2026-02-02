import 'dart:async';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/exceptions/app_exceptions.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../services/api_service.dart';
import '../models/listing.dart';
import '../models/listing_filter.dart';
import '../providers/favorites_provider.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/valora_filter_dialog.dart';
import '../widgets/valora_error_state.dart';
import '../widgets/home_components.dart';
import 'listing_detail_screen.dart';
import 'saved_listings_screen.dart';
import 'settings_screen.dart';

class HomeScreen extends StatefulWidget {
  final ApiService? apiService;

  const HomeScreen({super.key, this.apiService});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  late ApiService _apiService;
  late final ScrollController _scrollController;
  bool _isInit = false;

  bool _isConnected = false;
  List<Listing> _listings = [];
  List<Listing> _featuredListings = [];
  List<Listing> _nearbyListings = [];
  bool _isLoading = true;
  bool _isLoadingMore = false;
  Object? _error;

  // Pagination
  int _currentPage = 1;
  static const int _pageSize = 10;
  bool _hasNextPage = true;

  // Filters
  String _searchTerm = '';
  double? _minPrice;
  double? _maxPrice;
  String? _city;
  int? _minBedrooms;
  int? _minLivingArea;
  int? _maxLivingArea;
  String? _sortBy;
  String? _sortOrder;

  // Search
  final TextEditingController _searchController = TextEditingController();
  Timer? _debounce;

  static const String _defaultScrapeRegion = 'amsterdam';

  // Navigation
  int _currentNavIndex = 0;

  @override
  void initState() {
    super.initState();
    _scrollController = ScrollController()..addListener(_onScroll);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (!_isInit) {
      _apiService = widget.apiService ?? Provider.of<ApiService>(context, listen: false);
      _checkConnection();
      _isInit = true;
    }
  }

  @override
  void dispose() {
    _scrollController.dispose();
    _searchController.dispose();
    _debounce?.cancel();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollController.position.pixels >= _scrollController.position.maxScrollExtent - 200 &&
        !_isLoadingMore &&
        _hasNextPage &&
        _error == null) {
      _loadMoreListings();
    }
  }

  Future<void> _checkConnection() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    final connected = await _apiService.healthCheck();
    if (mounted) {
      setState(() {
        _isConnected = connected;
      });
      if (connected) {
        _loadListings(refresh: true);
      } else {
        setState(() => _isLoading = false);
      }
    }
  }

  Future<void> _loadListings({bool refresh = false}) async {
    if (!mounted) return;

    if (refresh) {
      setState(() {
        _isLoading = true;
        _error = null;
        _currentPage = 1;
        _listings = [];
        _hasNextPage = true;
      });
    }

    try {
      final response = await _apiService.getListings(
        ListingFilter(
          page: _currentPage,
          pageSize: _pageSize,
          searchTerm: _searchTerm,
          minPrice: _minPrice,
          maxPrice: _maxPrice,
          city: _city,
          minBedrooms: _minBedrooms,
          minLivingArea: _minLivingArea,
          maxLivingArea: _maxLivingArea,
          sortBy: _sortBy,
          sortOrder: _sortOrder,
        )
      );

      if (mounted) {
        setState(() {
          if (refresh) {
            _listings = response.items;
          } else {
            _listings.addAll(response.items);
          }
          _featuredListings = _listings.take(5).toList();
          _nearbyListings = _listings.skip(5).toList();
          _hasNextPage = response.hasNextPage;
          _error = null;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e;
        });

        if (_listings.isNotEmpty) {
           String message = e is AppException ? e.message : e.toString().replaceAll('Exception: ', '');
           ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(message),
              backgroundColor: Theme.of(context).colorScheme.error,
              action: SnackBarAction(
                label: 'Retry',
                textColor: Colors.white,
                onPressed: () => _loadListings(refresh: refresh),
              ),
            ),
          );
        }
      }
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _isLoadingMore = false;
        });
      }
    }
  }

  Future<void> _loadMoreListings() async {
    if (_isLoadingMore) return;
    setState(() => _isLoadingMore = true);
    _currentPage++;
    await _loadListings();
  }

  void _onSearchChanged(String query) {
    if (_debounce?.isActive ?? false) _debounce!.cancel();
    _debounce = Timer(const Duration(milliseconds: 500), () {
      setState(() {
        _searchTerm = query;
      });
      _loadListings(refresh: true);
    });
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

  Future<void> _triggerScrape() async {
    setState(() => _isLoading = true);
    try {
      await _apiService.triggerLimitedScrape(_defaultScrapeRegion, 10);

      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Scraping started... Please wait a moment.'),
          duration: Duration(seconds: 2),
        ),
      );

      await Future.delayed(const Duration(seconds: 3));

      if (mounted) {
        _loadListings(refresh: true);
      }
    } catch (e) {
      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Failed to trigger scrape: ${e is AppException ? e.message : e}'),
          backgroundColor: Theme.of(context).colorScheme.error,
        ),
      );
      setState(() => _isLoading = false);
    }
  }

  void _onListingTap(Listing listing) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => ListingDetailScreen(listing: listing),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBody: true, // Allow body to extend behind bottom nav if needed, but we have a custom one
      // The design has a fixed bottom nav. Scaffold.bottomNavigationBar works well.
      bottomNavigationBar: HomeBottomNavBar(
        currentIndex: _currentNavIndex,
        onTap: (index) => setState(() => _currentNavIndex = index),
      ),
      body: _buildBody(),
      floatingActionButton: _currentNavIndex == 0
          ? Padding(
              padding: const EdgeInsets.only(bottom: 16.0), // Adjust for bottom nav
              child: FloatingActionButton.extended(
                onPressed: _triggerScrape,
                backgroundColor: ValoraColors.primary,
                foregroundColor: Colors.white,
                elevation: 8,
                icon: const Icon(Icons.auto_awesome_rounded),
                label: Row(
                  children: const [
                    Text('Compare with AI'),
                  ],
                ),
              ),
            )
          : null,
    );
  }

  Widget _buildBody() {
    switch (_currentNavIndex) {
      case 0:
        return _buildHomeView();
      case 1:
        return const Center(child: Text('Search View Placeholder'));
      case 2:
        return const SavedListingsScreen();
      case 3:
        return const SettingsScreen();
      default:
        return _buildHomeView();
    }
  }

  Widget _buildHomeView() {
    int activeFilters = 0;
    if (_minPrice != null) activeFilters++;
    if (_maxPrice != null) activeFilters++;
    if (_city != null) activeFilters++;
    if (_minBedrooms != null) activeFilters++;
    if (_minLivingArea != null) activeFilters++;
    if (_maxLivingArea != null) activeFilters++;

    final bool hasFilters = activeFilters > 0 || _searchTerm.isNotEmpty;

    List<Widget> slivers = [
      _buildSliverAppBar(activeFilters),
    ];

    if (_isLoading && _listings.isEmpty) {
      slivers.add(const SliverFillRemaining(
        child: Center(child: ValoraLoadingIndicator(message: 'Loading listings...')),
      ));
    } else if (!_isConnected) {
      slivers.add(SliverFillRemaining(
        hasScrollBody: false,
        child: Center(
          child: ValoraEmptyState(
            icon: Icons.cloud_off_outlined,
            title: 'Backend not connected',
            subtitle: 'Unable to connect to Valora Server.',
            action: ValoraButton(
              label: 'Retry',
              variant: ValoraButtonVariant.primary,
              icon: Icons.refresh,
              onPressed: _checkConnection,
            ),
          ),
        ),
      ));
    } else if (_listings.isEmpty && _error != null) {
      slivers.add(SliverFillRemaining(
        hasScrollBody: false,
        child: Center(
          child: ValoraErrorState(
            error: _error!,
            onRetry: () => _loadListings(refresh: true),
          ),
        ),
      ));
    } else if (_listings.isEmpty) {
      slivers.add(SliverFillRemaining(
        hasScrollBody: false,
        child: Center(
            child: !hasFilters
                ? ValoraEmptyState(
                    icon: Icons.home_work_outlined,
                    title: 'No listings yet',
                    subtitle: 'Get started by scraping some listings.',
                    action: ValoraButton(
                      label: 'Scrape 10 Items',
                      variant: ValoraButtonVariant.primary,
                      icon: Icons.cloud_download,
                      onPressed: _triggerScrape,
                    ),
                  )
                : ValoraEmptyState(
                    icon: Icons.search_off,
                    title: 'No listings found',
                    subtitle: 'Try adjusting your filters or search term.',
                    action: ValoraButton(
                      label: 'Clear Filters',
                      variant: ValoraButtonVariant.outline,
                      onPressed: () {
                        setState(() {
                          _minPrice = null;
                          _maxPrice = null;
                          _city = null;
                          _minBedrooms = null;
                          _minLivingArea = null;
                          _maxLivingArea = null;
                          _searchTerm = '';
                          _searchController.clear();
                        });
                        _loadListings(refresh: true);
                      },
                    ),
                  ),
        ),
      ));
    } else {
      if (_featuredListings.isNotEmpty) {
        slivers.add(SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(24, 24, 24, 16),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Featured for You',
                      style: ValoraTypography.titleLarge.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'Curated by Valora AI based on your taste',
                      style: ValoraTypography.bodySmall.copyWith(
                        color: Theme.of(context).colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                ),
                Text(
                  'See All',
                  style: ValoraTypography.labelSmall.copyWith(
                    color: ValoraColors.primary,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
          ),
        ));

        slivers.add(SliverToBoxAdapter(
          child: SizedBox(
            height: 320,
            child: ListView.builder(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
              scrollDirection: Axis.horizontal,
              itemCount: _featuredListings.length,
              itemBuilder: (context, index) {
                final listing = _featuredListings[index];
                return FeaturedListingCard(
                  listing: listing,
                  onTap: () => _onListingTap(listing),
                );
              },
            ),
          ),
        ));
      }

      if (_nearbyListings.isNotEmpty || _hasNextPage) {
        slivers.add(SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(24, 32, 24, 16),
            child: Text(
              'Nearby Listings',
              style: ValoraTypography.titleLarge.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
        ));

        slivers.add(SliverPadding(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          sliver: SliverList(
            delegate: SliverChildBuilderDelegate(
              (context, index) {
                if (index == _nearbyListings.length) {
                  if (_hasNextPage) {
                    return const Padding(
                      padding: EdgeInsets.symmetric(vertical: 24),
                      child: Center(child: CircularProgressIndicator()),
                    );
                  }
                  return const SizedBox(height: 80);
                }

                final listing = _nearbyListings[index];
                return NearbyListingCard(
                  listing: listing,
                  onTap: () => _onListingTap(listing),
                );
              },
              childCount: _nearbyListings.length + (_hasNextPage ? 1 : 0),
            ),
          ),
        ));
      }
    }

    return RefreshIndicator(
      onRefresh: () => _loadListings(refresh: true),
      color: ValoraColors.primary,
      edgeOffset: 120,
      child: CustomScrollView(
        controller: _scrollController,
        slivers: slivers,
      ),
    );
  }

  Widget _buildSliverAppBar(int activeFilters) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return SliverAppBar(
      pinned: true,
      backgroundColor: isDark ? ValoraColors.backgroundDark.withValues(alpha: 0.95) : ValoraColors.backgroundLight.withValues(alpha: 0.95),
      surfaceTintColor: Colors.transparent,
      titleSpacing: 24,
      toolbarHeight: 70, // Height for the top row (Title + Profile)
      title: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            'Valora',
            style: ValoraTypography.headlineMedium.copyWith(
              color: ValoraColors.primary,
              fontWeight: FontWeight.bold,
              letterSpacing: -0.5,
            ),
          ),
          Row(
            children: [
              IconButton(
                onPressed: () {},
                icon: Icon(
                  Icons.notifications_none_rounded,
                  color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500
                ),
              ),
              const SizedBox(width: 8),
              Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  gradient: const LinearGradient(
                    colors: [ValoraColors.primary, ValoraColors.primaryLight],
                    begin: Alignment.bottomLeft,
                    end: Alignment.topRight,
                  ),
                  border: Border.all(
                      color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
                      width: 2),
                ),
                child: const Center(
                  child: Text(
                    'JD',
                    style: TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.bold,
                      fontSize: 14,
                    ),
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
      bottom: PreferredSize(
        preferredSize: const Size.fromHeight(140),
        child: HomeHeader(
          searchController: _searchController,
          onSearchChanged: _onSearchChanged,
          onFilterPressed: _openFilterDialog,
          activeFilterCount: activeFilters,
        ),
      ),
    );
  }
}
