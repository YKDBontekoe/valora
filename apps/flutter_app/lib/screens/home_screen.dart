import 'dart:async';
import 'dart:ui' as ui;
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../core/exceptions/app_exceptions.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../services/api_service.dart';
import '../models/listing.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/valora_listing_card.dart';
import '../widgets/valora_filter_dialog.dart';
import '../widgets/valora_error_state.dart';
import 'listing_detail_screen.dart';

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
  bool _isSearching = false;
  final TextEditingController _searchController = TextEditingController();
  Timer? _debounce;

  static const String _defaultScrapeRegion = 'amsterdam';

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
      );

      if (mounted) {
        setState(() {
          if (refresh) {
            _listings = response.items;
          } else {
            _listings.addAll(response.items);
          }
          _hasNextPage = response.hasNextPage;
          _error = null;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e;
        });

        // Show snackbar only if we have data (pagination error)
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
      // Trigger a limited scrape for 10 items in Amsterdam
      await _apiService.triggerLimitedScrape(_defaultScrapeRegion, 10);

      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Scraping started... Please wait a moment.'),
          duration: Duration(seconds: 2),
        ),
      );

      // Wait a bit to allow the background job to start and maybe finish some items
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBodyBehindAppBar: true, // Allow body to scroll behind glass app bar
      body: _buildContent(),
      floatingActionButton: _isConnected && _listings.isEmpty && _error == null
          ? FloatingActionButton.extended(
              onPressed: () => _loadListings(refresh: true),
              backgroundColor: ValoraColors.primary,
              foregroundColor: Colors.white,
              icon: const Icon(Icons.refresh),
              label: const Text('Refresh'),
              elevation: ValoraSpacing.elevationLg,
            )
          : null,
    );
  }

  Widget _buildContent() {
    Widget contentSliver;

    if (_isLoading) {
      contentSliver = const SliverFillRemaining(
        child: ValoraLoadingIndicator(message: 'Loading listings...'),
      );
    } else if (!_isConnected) {
      contentSliver = SliverFillRemaining(
        hasScrollBody: false,
        child: Center(
          child: ValoraEmptyState(
            icon: Icons.cloud_off_outlined,
            title: 'Backend not connected',
            subtitle:
                'Unable to connect to Valora Server. Please ensure the backend is running.',
            action: ValoraButton(
              label: 'Retry',
              variant: ValoraButtonVariant.primary,
              icon: Icons.refresh,
              onPressed: _checkConnection,
            ),
          ),
        ),
      );
    } else if (_listings.isEmpty && _error != null) {
      contentSliver = SliverFillRemaining(
        hasScrollBody: false,
        child: Center(
          child: ValoraErrorState(
            error: _error!,
            onRetry: () => _loadListings(refresh: true),
          ),
        ),
      );
    } else if (_listings.isEmpty) {
      final bool hasFilters = _minPrice != null ||
          _maxPrice != null ||
          _city != null ||
          _minBedrooms != null ||
          _minLivingArea != null ||
          _maxLivingArea != null ||
          _searchTerm.isNotEmpty;

      contentSliver = SliverFillRemaining(
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
                        _isSearching = false;
                      });
                      _loadListings(refresh: true);
                    },
                  ),
                ),
        ),
      );
    } else {
      contentSliver = SliverPadding(
        padding: const EdgeInsets.fromLTRB(
          ValoraSpacing.screenPadding,
          ValoraSpacing.screenPadding,
          ValoraSpacing.screenPadding,
          ValoraSpacing.xxl, // Bottom padding
        ),
        sliver: SliverList(
          delegate: SliverChildBuilderDelegate(
            (context, index) {
              if (index == _listings.length) {
                if (_hasNextPage) {
                  return const Padding(
                    padding: EdgeInsets.symmetric(vertical: ValoraSpacing.md),
                    child: Center(
                      child: CircularProgressIndicator(),
                    ),
                  );
                }
                return const SizedBox.shrink();
              }

              final listing = _listings[index];
              // Only animate first page
              final int delay =
                  (index < _pageSize && _currentPage == 1) ? index * 100 : 0;

              return Padding(
                padding:
                    const EdgeInsets.only(bottom: ValoraSpacing.listItemGap),
                child: RepaintBoundary(
                  child: _SlideInItem(
                    delay: Duration(milliseconds: delay),
                    child: ValoraListingCard(
                      listing: listing,
                      onTap: () {
                        Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) =>
                                ListingDetailScreen(listing: listing),
                          ),
                        );
                      },
                    ),
                  ),
                ),
              );
            },
            childCount: _listings.length + (_hasNextPage ? 1 : 0),
          ),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => _loadListings(refresh: true),
      color: ValoraColors.primary,
      edgeOffset: 100, // Offset for glass app bar
      child: CustomScrollView(
        controller: _scrollController,
        slivers: [
          _buildSliverAppBar(floating: true),
          contentSliver,
        ],
      ),
    );
  }

  Widget _buildSliverAppBar({bool floating = false}) {
    return SliverAppBar(
      pinned: true,
      floating: floating,
      backgroundColor: Colors.transparent,
      surfaceTintColor: Colors.transparent,
      flexibleSpace: ClipRRect(
        child: BackdropFilter(
          filter: ui.ImageFilter.blur(sigmaX: 10, sigmaY: 10),
          child: Container(
            color: Theme.of(context).brightness == Brightness.dark
                ? ValoraColors.glassBlack
                : ValoraColors.glassWhite,
          ),
        ),
      ),
      title: _isSearching
          ? TextField(
              controller: _searchController,
              autofocus: true,
              style: Theme.of(context).textTheme.bodyLarge,
              decoration: InputDecoration(
                hintText: 'Search address, city...',
                hintStyle: Theme.of(context).textTheme.bodyLarge?.copyWith(
                      color: Theme.of(context).colorScheme.onSurfaceVariant,
                    ),
                border: InputBorder.none,
                enabledBorder: InputBorder.none,
                focusedBorder: InputBorder.none,
                filled: false,
              ),
              onChanged: _onSearchChanged,
            )
          : Text(
              'Valora',
              style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: Theme.of(context).colorScheme.primary,
                  ),
            ),
      actions: [
        IconButton(
          icon: const Icon(Icons.logout),
          onPressed: () {
            final parentContext = context;
            showDialog(
              context: context,
              builder: (dialogContext) => ValoraDialog(
                title: 'Logout',
                actions: [
                  ValoraButton(
                    label: 'Cancel',
                    variant: ValoraButtonVariant.ghost,
                    onPressed: () => Navigator.pop(dialogContext),
                  ),
                  ValoraButton(
                    label: 'Logout',
                    variant: ValoraButtonVariant.primary,
                    onPressed: () {
                      Navigator.pop(dialogContext);
                      parentContext.read<AuthProvider>().logout();
                    },
                  ),
                ],
                child: const Text('Are you sure you want to logout?'),
              ),
            );
          },
          tooltip: 'Logout',
        ),
        IconButton(
          icon: Icon(_isSearching ? Icons.close : Icons.search),
          onPressed: () {
            setState(() {
              if (_isSearching) {
                _isSearching = false;
                _searchController.clear();
                _searchTerm = '';
                _loadListings(refresh: true);
              } else {
                _isSearching = true;
              }
            });
          },
        ),
        IconButton(
          icon: Stack(
            children: [
              const Icon(Icons.filter_list),
              if (_minPrice != null ||
                  _maxPrice != null ||
                  _city != null ||
                  _minBedrooms != null ||
                  _minLivingArea != null ||
                  _maxLivingArea != null)
                Positioned(
                  right: 0,
                  top: 0,
                  child: Container(
                    padding: const EdgeInsets.all(2),
                    decoration: const BoxDecoration(
                      color: ValoraColors.error,
                      shape: BoxShape.circle,
                    ),
                    constraints: const BoxConstraints(
                      minWidth: 8,
                      minHeight: 8,
                    ),
                  ),
                ),
            ],
          ),
          onPressed: _openFilterDialog,
          tooltip: 'Filter',
        ),
        const SizedBox(width: ValoraSpacing.sm),
      ],
    );
  }
}

class _SlideInItem extends StatefulWidget {
  const _SlideInItem({
    required this.child,
    this.delay = Duration.zero,
  });

  final Widget child;
  final Duration delay;

  @override
  State<_SlideInItem> createState() => _SlideInItemState();
}

class _SlideInItemState extends State<_SlideInItem>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _fadeAnimation;
  late Animation<Offset> _slideAnimation;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 600),
    );

    _fadeAnimation = CurvedAnimation(
      parent: _controller,
      curve: Curves.easeOut,
    );

    _slideAnimation = Tween<Offset>(
      begin: const Offset(0, 0.1),
      end: Offset.zero,
    ).animate(CurvedAnimation(
      parent: _controller,
      curve: Curves.easeOutCubic,
    ));

    if (widget.delay == Duration.zero) {
      _controller.forward();
    } else {
      Future.delayed(widget.delay, () {
        if (mounted) {
          _controller.forward();
        }
      });
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return FadeTransition(
      opacity: _fadeAnimation,
      child: SlideTransition(
        position: _slideAnimation,
        child: widget.child,
      ),
    );
  }
}
