import 'dart:async';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/exceptions/app_exceptions.dart';
import '../core/theme/valora_colors.dart';
import '../services/api_service.dart';
import '../models/listing.dart';
import '../models/listing_filter.dart';
import '../widgets/valora_filter_dialog.dart';
import '../widgets/home_components.dart';
import '../widgets/home/home_sliver_app_bar.dart';
import '../widgets/home/home_status_views.dart';
import 'listing_detail_screen.dart';
import 'saved_listings_screen.dart';
import 'search_screen.dart';
import 'settings_screen.dart';

class HomeScreen extends StatefulWidget {
  final ApiService? apiService;

  const HomeScreen({super.key, this.apiService});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  static const int _pageSize = 10;
  static const String _defaultScrapeRegion = 'amsterdam';
  static const int _featuredCount = 5;
  static const double _bottomListPadding = 160.0;

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
          _featuredListings = _listings.take(_featuredCount).toList();
          _nearbyListings = _listings.skip(_featuredCount).toList();
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
    // Get the bottom padding from the safe area (including gesture navigation bar)
    final bottomPadding = MediaQuery.of(context).padding.bottom;

    // Calculate the total offset needed for the FAB:
    // Nav bar height (~70) + bottom padding + margin (12) + extra spacing (16)
    final fabBottomOffset = 70.0 + bottomPadding + 28.0;

    return Scaffold(
      extendBody: true,
      bottomNavigationBar: HomeBottomNavBar(
        currentIndex: _currentNavIndex,
        onTap: (index) => setState(() => _currentNavIndex = index),
      ),
      body: _buildBody(),
      floatingActionButton: _currentNavIndex == 0
          ? Padding(
              padding: EdgeInsets.only(bottom: fabBottomOffset),
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
        return const SearchScreen();
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
      HomeSliverAppBar(
        searchController: _searchController,
        onSearchChanged: _onSearchChanged,
        onFilterPressed: _openFilterDialog,
        activeFilterCount: activeFilters,
        userInitials: null,
      ),
    ];

    if (_isLoading && _listings.isEmpty) {
      slivers.add(const HomeLoadingSliver());
    } else if (!_isConnected) {
      slivers.add(HomeDisconnectedSliver(onRetry: _checkConnection));
    } else if (_listings.isEmpty && _error != null) {
      slivers.add(HomeErrorSliver(
        error: _error!,
        onRetry: () => _loadListings(refresh: true),
      ));
    } else if (_listings.isEmpty) {
      slivers.add(HomeEmptySliver(
        hasFilters: hasFilters,
        onScrape: _triggerScrape,
        onClearFilters: () {
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
      ));
    } else {
      slivers.addAll(_buildListingSlivers());
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

  List<Widget> _buildListingSlivers() {
    final List<Widget> slivers = [];

    if (_featuredListings.isNotEmpty) {
      slivers.add(FeaturedListingsSection(
        listings: _featuredListings,
        onTap: _onListingTap,
      ));
    }

    if (_nearbyListings.isNotEmpty || _hasNextPage) {
      slivers.add(const NearbyListingsHeader());
      slivers.add(NearbyListingsList(
        listings: _nearbyListings,
        hasNextPage: _hasNextPage,
        bottomPadding: _bottomListPadding,
        onTap: _onListingTap,
      ));
    }
    return slivers;
  }
}
