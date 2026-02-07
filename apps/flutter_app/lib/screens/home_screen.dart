import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';

import '../core/exceptions/app_exceptions.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import '../providers/auth_provider.dart';
import '../providers/home_listings_provider.dart';
import '../services/api_service.dart';
import '../widgets/home/featured_listing_card.dart';
import '../widgets/home/home_bottom_nav_bar.dart';
import '../widgets/home/home_sliver_app_bar.dart';
import '../widgets/home/home_status_views.dart';
import '../widgets/home/nearby_listing_card.dart';
import '../widgets/valora_filter_dialog.dart';
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
  static const int _featuredCount = 5;
  static const double _bottomListPadding = 160.0;
  static const String _defaultScrapeRegion = 'amsterdam';

  late final ScrollController _scrollController;
  final TextEditingController _searchController = TextEditingController();
  Timer? _debounce;

  HomeListingsProvider? _homeProvider;
  int _currentNavIndex = 0;

  @override
  void initState() {
    super.initState();
    _scrollController = ScrollController()..addListener(_onScroll);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_homeProvider == null) {
      final ApiService apiService =
          widget.apiService ?? context.read<ApiService>();
      _homeProvider = HomeListingsProvider(apiService: apiService);
      _homeProvider!.initialize();
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
    final HomeListingsProvider provider = _homeProvider!;
    if (_scrollController.position.pixels >=
            _scrollController.position.maxScrollExtent - 200 &&
        !provider.isLoadingMore &&
        provider.hasNextPage &&
        provider.error == null) {
      _loadMoreListings();
    }
  }

  Future<void> _loadMoreListings() async {
    final HomeListingsProvider provider = _homeProvider!;
    final Object? previousError = provider.error;
    await provider.loadMore();

    if (!mounted) {
      return;
    }

    if (provider.error != null && provider.error != previousError) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: const Text('Unable to load more listings right now.'),
          backgroundColor: Theme.of(context).colorScheme.error,
        ),
      );
    }
  }

  void _onSearchChanged(String query) {
    if (_debounce?.isActive ?? false) {
      _debounce!.cancel();
    }
    _debounce = Timer(const Duration(milliseconds: 500), () {
      _homeProvider!.setSearchTerm(query);
    });
  }

  Future<void> _openFilterDialog() async {
    final HomeListingsProvider provider = _homeProvider!;

    final Map<String, dynamic>? result = await showDialog<Map<String, dynamic>>(
      context: context,
      builder: (context) => ValoraFilterDialog(
        initialMinPrice: provider.minPrice,
        initialMaxPrice: provider.maxPrice,
        initialCity: provider.city,
        initialMinBedrooms: provider.minBedrooms,
        initialMinLivingArea: provider.minLivingArea,
        initialMaxLivingArea: provider.maxLivingArea,
        initialSortBy: provider.sortBy,
        initialSortOrder: provider.sortOrder,
      ),
    );

    if (result == null) {
      return;
    }

    await provider.applyFilters(
      minPrice: result['minPrice'] as double?,
      maxPrice: result['maxPrice'] as double?,
      city: result['city'] as String?,
      minBedrooms: result['minBedrooms'] as int?,
      minLivingArea: result['minLivingArea'] as int?,
      maxLivingArea: result['maxLivingArea'] as int?,
      sortBy: result['sortBy'] as String?,
      sortOrder: result['sortOrder'] as String?,
    );
  }

  Future<void> _triggerScrape() async {
    final HomeListingsProvider provider = _homeProvider!;
    final _ScrapeSettings? scrapeSettings = await showDialog<_ScrapeSettings>(
      context: context,
      builder: (context) =>
          _ScrapeDialog(initialRegion: _deriveDefaultRegion(provider)),
    );

    if (scrapeSettings == null) {
      return;
    }

    setState(() {});

    try {
      await provider.triggerScrape(
        region: scrapeSettings.region,
        limit: scrapeSettings.limit,
      );

      if (!mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Scraping started for ${scrapeSettings.region}. Refreshing shortly...',
          ),
          duration: const Duration(seconds: 2),
        ),
      );

      await Future<void>.delayed(const Duration(seconds: 2));
      await provider.refresh();
    } catch (e) {
      if (!mounted) {
        return;
      }

      final String message = e is AppException ? e.message : e.toString();
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Failed to trigger scrape: $message'),
          backgroundColor: Theme.of(context).colorScheme.error,
        ),
      );
    }
  }

  String _deriveDefaultRegion(HomeListingsProvider provider) {
    if (provider.city != null && provider.city!.trim().isNotEmpty) {
      return provider.city!.trim().toLowerCase();
    }

    if (provider.searchTerm.trim().isNotEmpty) {
      final String normalized = provider.searchTerm.trim().toLowerCase();
      final List<String> words = normalized.split(RegExp(r'\s+'));
      return words.first;
    }

    return _defaultScrapeRegion;
  }

  Future<void> _openNotifications(BuildContext context) async {
    await showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (context) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: const [
              Text(
                'Notifications',
                style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
              ),
              SizedBox(height: 12),
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: Icon(Icons.auto_awesome_rounded),
                title: Text('AI insights are enabled'),
                subtitle: Text('You will see new recommendation updates here.'),
              ),
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: Icon(Icons.trending_down_rounded),
                title: Text('Price drop alerts are on'),
                subtitle: Text(
                  'Saved listings with price drops will appear here.',
                ),
              ),
            ],
          ),
        ),
      ),
    );
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
    final double bottomPadding = MediaQuery.of(context).padding.bottom;
    final double fabBottomOffset = 70.0 + bottomPadding + 28.0;

    return ChangeNotifierProvider<HomeListingsProvider>.value(
      value: _homeProvider!,
      child: Scaffold(
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
                  label: const Text('Fetch Listings'),
                ),
              )
            : null,
      ),
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
    return Consumer<HomeListingsProvider>(
      builder: (context, provider, _) {
        final List<Widget> slivers = [
          HomeSliverAppBar(
            searchController: _searchController,
            onSearchChanged: _onSearchChanged,
            onFilterPressed: _openFilterDialog,
            activeFilterCount: provider.activeFilterCount,
            onNotificationsPressed: () => _openNotifications(context),
            userInitials: _userInitials(context),
          ),
        ];

        if (provider.isLoading && provider.listings.isEmpty) {
          slivers.add(const HomeLoadingSliver());
        } else if (!provider.isConnected) {
          slivers.add(
            HomeDisconnectedSliver(onRetry: provider.checkConnectionAndLoad),
          );
        } else if (provider.listings.isEmpty && provider.error != null) {
          slivers.add(
            HomeErrorSliver(error: provider.error!, onRetry: provider.refresh),
          );
        } else if (provider.listings.isEmpty) {
          slivers.add(
            HomeEmptySliver(
              hasFilters: provider.hasFilters,
              onScrape: _triggerScrape,
              onClearFilters: () {
                _searchController.clear();
                provider.clearFiltersAndSearch();
              },
            ),
          );
        } else {
          slivers.addAll(_buildListingSlivers(provider));
        }

        return RefreshIndicator(
          onRefresh: provider.refresh,
          color: ValoraColors.primary,
          edgeOffset: 120,
          child: CustomScrollView(
            controller: _scrollController,
            slivers: slivers,
          ),
        );
      },
    );
  }

  List<Widget> _buildListingSlivers(HomeListingsProvider provider) {
    final featuredListings = provider.listings.take(_featuredCount).toList();
    final nearbyListings = provider.listings.skip(_featuredCount).toList();
    final List<Widget> slivers = [];

    if (featuredListings.isNotEmpty) {
      slivers.add(
        SliverToBoxAdapter(
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
            ).animate().fade().slideX(begin: -0.2, end: 0, duration: 400.ms),
          ),
        ),
      );

      slivers.add(
        SliverToBoxAdapter(
          child: SizedBox(
            height: 320,
            child: ListView.builder(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
              scrollDirection: Axis.horizontal,
              itemCount: featuredListings.length,
              itemBuilder: (context, index) {
                final listing = featuredListings[index];
                return FeaturedListingCard(
                      key: ValueKey(listing.id),
                      listing: listing,
                      onTap: () => _onListingTap(listing),
                    )
                    .animate()
                    .fade(duration: 400.ms)
                    .slideX(begin: 0.1, end: 0, delay: (50 * index).ms);
              },
            ),
          ),
        ),
      );
    }

    if (nearbyListings.isNotEmpty || provider.hasNextPage) {
      slivers.add(
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(24, 32, 24, 16),
            child: Text(
              'Nearby Listings',
              style: ValoraTypography.titleLarge.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ).animate().fade().slideY(begin: 0.2, end: 0, delay: 200.ms),
          ),
        ),
      );

      slivers.add(
        SliverPadding(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          sliver: SliverList(
            delegate: SliverChildBuilderDelegate((context, index) {
              if (index == nearbyListings.length) {
                if (provider.error != null) {
                  return Padding(
                    padding: const EdgeInsets.symmetric(vertical: 24),
                    child: Padding(
                      padding: const EdgeInsets.only(bottom: _bottomListPadding),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(
                            'Unable to load more listings',
                            style: ValoraTypography.bodyMedium.copyWith(
                              color: Theme.of(context).colorScheme.error,
                            ),
                          ),
                          TextButton(
                            onPressed: provider.loadMore,
                            child: const Text('Retry'),
                          ),
                        ],
                      ),
                    ),
                  );
                }
                if (provider.hasNextPage) {
                  return const Padding(
                    padding: EdgeInsets.symmetric(vertical: 24),
                    child: Padding(
                      padding: EdgeInsets.only(bottom: _bottomListPadding),
                      child: Center(child: CircularProgressIndicator()),
                    ),
                  );
                }
                return const SizedBox(height: _bottomListPadding);
              }

              final listing = nearbyListings[index];
              return NearbyListingCard(
                    key: ValueKey(listing.id),
                    listing: listing,
                    onTap: () => _onListingTap(listing),
                  )
                  .animate()
                  .fade(duration: 400.ms)
                  .slideY(begin: 0.1, end: 0, delay: (50 * (index % 10)).ms);
            }, childCount: nearbyListings.length + 1),
          ),
        ),
      );
    }

    return slivers;
  }

  String? _userInitials(BuildContext context) {
    final AuthProvider? authProvider = Provider.of<AuthProvider?>(
      context,
      listen: false,
    );
    final String? email = authProvider?.email;
    if (email == null || email.trim().isEmpty) {
      return null;
    }

    final String localPart = email.split('@').first;
    final List<String> names = localPart.split(RegExp(r'[._-]+'));
    if (names.length >= 2) {
      return '${names[0][0]}${names[1][0]}'.toUpperCase();
    }

    return localPart.substring(0, localPart.length >= 2 ? 2 : 1).toUpperCase();
  }
}

class _ScrapeDialog extends StatefulWidget {
  const _ScrapeDialog({required this.initialRegion});

  final String initialRegion;

  @override
  State<_ScrapeDialog> createState() => _ScrapeDialogState();
}

class _ScrapeDialogState extends State<_ScrapeDialog> {
  late TextEditingController _regionController;
  int _limit = 10;

  @override
  void initState() {
    super.initState();
    _regionController = TextEditingController(text: widget.initialRegion);
  }

  @override
  void dispose() {
    _regionController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Fetch listings'),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          TextField(
            controller: _regionController,
            decoration: const InputDecoration(
              labelText: 'Region',
              hintText: 'e.g. amsterdam',
            ),
          ),
          const SizedBox(height: 16),
          DropdownButtonFormField<int>(
            initialValue: _limit,
            decoration: const InputDecoration(labelText: 'Number of listings'),
            items: const [
              DropdownMenuItem(value: 10, child: Text('10')),
              DropdownMenuItem(value: 25, child: Text('25')),
              DropdownMenuItem(value: 50, child: Text('50')),
            ],
            onChanged: (value) {
              if (value != null) {
                setState(() => _limit = value);
              }
            },
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Cancel'),
        ),
        FilledButton(
          onPressed: () {
            final String region = _regionController.text.trim().toLowerCase();
            if (region.isEmpty) {
              return;
            }
            Navigator.pop(
              context,
              _ScrapeSettings(region: region, limit: _limit),
            );
          },
          child: const Text('Start'),
        ),
      ],
    );
  }
}

class _ScrapeSettings {
  const _ScrapeSettings({required this.region, required this.limit});

  final String region;
  final int limit;
}
