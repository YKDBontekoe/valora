import 'dart:async';
import 'dart:developer' as developer;

import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';

import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import '../providers/search_listings_provider.dart';
import '../services/api_service.dart';
import '../services/property_photo_service.dart';
import '../widgets/home_components.dart';
import '../widgets/search/active_filters_list.dart';
import '../widgets/search/search_input.dart';
import '../widgets/search/sort_options_sheet.dart';
import '../widgets/search/valora_filter_dialog.dart';
import '../widgets/valora_widgets.dart';
import '../services/pdok_service.dart';
import 'listing_detail_screen.dart';
import 'notifications_screen.dart';
import '../services/notification_service.dart';

class SearchScreen extends StatefulWidget {
  final PdokService? pdokService;
  final PropertyPhotoService? propertyPhotoService;

  const SearchScreen({super.key, this.pdokService, this.propertyPhotoService});

  @override
  State<SearchScreen> createState() => _SearchScreenState();
}

class _SearchScreenState extends State<SearchScreen> {
  final TextEditingController _searchController = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  late final PdokService _pdokService;
  late final PropertyPhotoService _propertyPhotoService;
  Timer? _debounce;

  SearchListingsProvider? _searchProvider;
  bool _ownsProvider = false;

  @override
  void initState() {
    super.initState();
    _pdokService = widget.pdokService ?? PdokService();
    _propertyPhotoService =
        widget.propertyPhotoService ?? PropertyPhotoService();
    _searchController.addListener(_onSearchChanged);
    _scrollController.addListener(_onScroll);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    try {
      _searchProvider ??= context.read<SearchListingsProvider>();
    } catch (_) {
      final created = SearchListingsProvider(
        apiService: context.read<ApiService>(),
      );
      _searchProvider ??= created;
      _ownsProvider = true;
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    _scrollController.dispose();
    _debounce?.cancel();
    if (_ownsProvider) {
      _searchProvider?.dispose();
    }
    super.dispose();
  }

  void _onScroll() {
    final SearchListingsProvider provider = _searchProvider!;
    if (_scrollController.position.pixels >=
            _scrollController.position.maxScrollExtent - 200 &&
        !provider.isLoadingMore &&
        provider.hasNextPage &&
        provider.error == null &&
        (provider.listings.isNotEmpty || provider.query.isNotEmpty)) {
      _loadMoreListings();
    }
  }

  void _onSearchChanged() {
    final String query = _searchController.text;
    if (query == _searchProvider!.query) {
      return;
    }

    _searchProvider!.setQuery(query);

    if (_debounce?.isActive ?? false) {
      _debounce!.cancel();
    }
    _debounce = Timer(const Duration(milliseconds: 750), () {
      _searchProvider!.refresh();
    });
  }

  Future<void> _loadMoreListings() async {
    final SearchListingsProvider provider = _searchProvider!;
    final String? previousError = provider.error;
    await provider.loadMore();

    if (!mounted) {
      return;
    }

    if (provider.error != null && provider.error != previousError) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Failed to load more items')),
      );
    }
  }

  Future<Listing> _enrichListingWithRealPhotos(Listing listing) async {
    final hasPhotos =
        listing.imageUrls.isNotEmpty ||
        (listing.imageUrl?.trim().isNotEmpty ?? false);
    if (hasPhotos || listing.latitude == null || listing.longitude == null) {
      return listing;
    }

    final photoUrls = _propertyPhotoService.getPropertyPhotos(
      latitude: listing.latitude!,
      longitude: listing.longitude!,
    );

    if (photoUrls.isEmpty) {
      return listing;
    }

    final serialized = listing.toJson();
    serialized['imageUrl'] = photoUrls.first;
    serialized['imageUrls'] = photoUrls;
    return Listing.fromJson(serialized);
  }

  Future<void> _openListingDetail(Listing listing) async {
    Listing listingToDisplay = listing;
    try {
      // If the listing is a summary (missing description or features), fetch full details
      // Only do this if we have a URL, which indicates a DB-backed listing (PDOK lookups lack URLs)
      if (listing.description == null &&
          listing.features.isEmpty &&
          listing.url != null) {
        final fullListing = await context.read<ApiService>().getListing(
          listing.id,
        );
        listingToDisplay = fullListing;
      }

      listingToDisplay = await _enrichListingWithRealPhotos(listingToDisplay);
    } catch (e, stack) {
      developer.log(
        'Listing enrichment failed for listing ${listing.id}',
        name: 'SearchScreen',
        error: e,
        stackTrace: stack,
      );
      // Fallback to what we have
    }

    if (!mounted) {
      return;
    }

    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => ListingDetailScreen(listing: listingToDisplay),
      ),
    );
  }

  void _showSortOptions() {
    final SearchListingsProvider provider = _searchProvider!;
    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.transparent,
      builder:
          (context) => SortOptionsSheet(
            provider: provider,
            onClose: () => Navigator.pop(context),
          ),
    );
  }

  Future<void> _openFilterDialog() async {
    final SearchListingsProvider provider = _searchProvider!;

    final Map<String, dynamic>? result = await showDialog<Map<String, dynamic>>(
      context: context,
      builder:
          (context) => ValoraFilterDialog(
            initialMinPrice: provider.minPrice,
            initialMaxPrice: provider.maxPrice,
            initialCity: provider.city,
            initialMinBedrooms: provider.minBedrooms,
            initialMinLivingArea: provider.minLivingArea,
            initialMaxLivingArea: provider.maxLivingArea,
            initialMinSafetyScore: provider.minSafetyScore,
            initialMinCompositeScore: provider.minCompositeScore,
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
      minSafetyScore: result['minSafetyScore'] as double?,
      minCompositeScore: result['minCompositeScore'] as double?,
      sortBy: result['sortBy'] as String?,
      sortOrder: result['sortOrder'] as String?,
    );
  }

  Future<void> _onSuggestionSelected(PdokSuggestion suggestion) async {
    _debounce?.cancel();

    // Temporarily remove listener to avoid triggering _onSearchChanged
    _searchController.removeListener(_onSearchChanged);
    _searchController.text = suggestion.displayName;
    _searchController.addListener(_onSearchChanged);

    // If it is a specific address (bucket 'adres'), lookup directly
    if (suggestion.type == 'adres') {
      if (!mounted) return;
      showDialog(
        context: context,
        barrierDismissible: false,
        builder:
            (context) => const Center(
              child: ValoraLoadingIndicator(
                message: 'Loading property details...',
              ),
            ),
      );

      try {
        final listing = await context
            .read<ApiService>()
            .getListingFromPdok(suggestion.id);

        if (!mounted) return;
        Navigator.pop(context); // Remove loading indicator

        if (listing != null) {
          await _openListingDetail(listing);
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Could not load property details')),
          );
        }
      } catch (e, stack) {
        if (!mounted) return;
        Navigator.pop(context); // Remove loading indicator

        developer.log(
          'Error loading PDOK listing',
          name: 'SearchScreen',
          error: e,
          stackTrace: stack,
        );

        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Something went wrong. Please try again.'),
          ),
        );
      }
    }
    // Otherwise (city, street, etc), just fall back to existing search behavior
    else {
      if (suggestion.type == 'woonplaats') {
        _searchProvider!.setCity(suggestion.displayName);
        _searchController.removeListener(_onSearchChanged);
        _searchController.clear();
        _searchController.addListener(_onSearchChanged);
      } else {
        _searchProvider!.setQuery(suggestion.displayName);
      }
      _searchProvider!.refresh();
    }
  }

  @override
  Widget build(BuildContext context) {
    final bool isDark = Theme.of(context).brightness == Brightness.dark;

    return ChangeNotifierProvider<SearchListingsProvider>.value(
      value: _searchProvider!,
      child: Scaffold(
        body: RefreshIndicator(
          onRefresh: () => _searchProvider!.refresh(clearData: false),
          child: CustomScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            controller: _scrollController,
            slivers: [
              Selector<SearchListingsProvider, bool>(
                selector: (_, p) => p.hasActiveFiltersOrSort,
                builder: (context, hasActiveFiltersOrSort, _) {
                  return SliverAppBar(
                    pinned: true,
                    backgroundColor: isDark
                        ? ValoraColors.backgroundDark.withValues(alpha: 0.95)
                        : ValoraColors.backgroundLight.withValues(alpha: 0.95),
                    surfaceTintColor: Colors.transparent,
                    title: Text(
                      'Search',
                      style: ValoraTypography.headlineMedium.copyWith(
                        color: isDark
                            ? ValoraColors.neutral50
                            : ValoraColors.neutral900,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    centerTitle: false,
                    actions: [
                      Consumer<NotificationService>(
                        builder: (context, notificationProvider, _) {
                          return Stack(
                            children: [
                              IconButton(
                                onPressed: () {
                                  Navigator.push(
                                    context,
                                    MaterialPageRoute(
                                      builder: (context) =>
                                          const NotificationsScreen(),
                                    ),
                                  );
                                },
                                icon: const Icon(Icons.notifications_outlined),
                                tooltip: 'Notifications',
                              ),
                              if (notificationProvider.unreadCount > 0)
                                Positioned(
                                  top: ValoraSpacing.radiusLg,
                                  right: ValoraSpacing.radiusLg,
                                  child: Container(
                                    width: ValoraSpacing.sm,
                                    height: ValoraSpacing.sm,
                                    decoration: const BoxDecoration(
                                      color: ValoraColors.error,
                                      shape: BoxShape.circle,
                                    ),
                                  ),
                                ).animate().scale(curve: Curves.elasticOut),
                            ],
                          );
                        },
                      ),
                      IconButton(
                        onPressed: _showSortOptions,
                        icon: const Icon(Icons.sort_rounded),
                        tooltip: 'Sort',
                      ),
                      Stack(
                        children: [
                          IconButton(
                            onPressed: _openFilterDialog,
                            icon: const Icon(Icons.tune_rounded),
                            tooltip: 'Filters',
                          ),
                          Selector<SearchListingsProvider, bool>(
                            selector: (_, p) => p.hasActiveFilters,
                            builder: (context, hasActiveFilters, _) {
                              if (hasActiveFilters) {
                                return Positioned(
                                  top: ValoraSpacing.sm,
                                  right: ValoraSpacing.sm,
                                  child: Container(
                                    width: 10,
                                    height: 10,
                                    decoration: const BoxDecoration(
                                      color: ValoraColors.primary,
                                      shape: BoxShape.circle,
                                    ),
                                  ),
                                );
                              }
                              return const SizedBox.shrink();
                            },
                          ),
                        ],
                      ),
                      const SizedBox(width: ValoraSpacing.sm),
                    ],
                    bottom: PreferredSize(
                      preferredSize: Size.fromHeight(
                        hasActiveFiltersOrSort ? 130 : 80,
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          SearchInput(
                            controller: _searchController,
                            pdokService: _pdokService,
                            onSuggestionSelected: _onSuggestionSelected,
                            onSubmitted: () {
                              _debounce?.cancel();
                              _searchProvider!.refresh();
                            },
                          ),
                          Consumer<SearchListingsProvider>(
                            builder: (context, provider, _) => ActiveFiltersList(
                              provider: provider,
                              onFilterTap: _openFilterDialog,
                              onSortTap: _showSortOptions,
                            ),
                          ),
                          if (hasActiveFiltersOrSort)
                            const SizedBox(height: ValoraSpacing.radiusLg),
                        ],
                      ),
                    ),
                  );
                },
              ),
              Selector<SearchListingsProvider, bool>(
                selector: (_, p) => p.isLoading,
                builder: (context, isLoading, _) {
                  if (isLoading) {
                    return const SliverFillRemaining(
                      child: ValoraLoadingIndicator(message: 'Searching...'),
                    );
                  }
                  return const SliverToBoxAdapter(child: SizedBox.shrink());
                },
              ),
              Selector<SearchListingsProvider, String?>(
                selector: (_, p) => p.error,
                builder: (context, error, _) {
                  if (error != null && _searchProvider!.listings.isEmpty) {
                    return SliverFillRemaining(
                      hasScrollBody: false,
                      child: Center(
                        child: ValoraEmptyState(
                          icon: Icons.error_outline_rounded,
                          title: 'Search Failed',
                          subtitle: error,
                          actionLabel: 'Retry',
                          onAction: _searchProvider!.refresh,
                        ),
                      ),
                    );
                  }
                  return const SliverToBoxAdapter(child: SizedBox.shrink());
                },
              ),
              Selector<
                SearchListingsProvider,
                (bool isEmpty, String query, bool hasFilters)
              >(
                selector:
                    (_, p) => (
                      p.listings.isEmpty && !p.isLoading && p.error == null,
                      p.query,
                      p.hasActiveFilters,
                    ),
                builder: (context, state, _) {
                  final isEmpty = state.$1;
                  final query = state.$2;
                  final hasFilters = state.$3;

                  if (!isEmpty) {
                    return const SliverToBoxAdapter(child: SizedBox.shrink());
                  }

                  if (query.isNotEmpty || hasFilters) {
                    return const SliverFillRemaining(
                      hasScrollBody: false,
                      child: Center(
                        child: ValoraEmptyState(
                          icon: Icons.search_off_rounded,
                          title: 'No results found',
                          subtitle:
                              'Try adjusting your filters or search terms.',
                        ),
                      ),
                    );
                  } else {
                    return const SliverFillRemaining(
                      hasScrollBody: false,
                      child: Center(
                        child: ValoraEmptyState(
                          icon: Icons.search_rounded,
                          title: 'Find your home',
                          subtitle:
                              'Enter a location or use filters to start searching.',
                        ),
                      ),
                    );
                  }
                },
              ),
              Selector<SearchListingsProvider, List<Listing>>(
                selector: (_, p) => p.listings,
                shouldRebuild: (prev, next) => prev != next,
                builder: (context, listings, _) {
                  if (listings.isEmpty) {
                    return const SliverToBoxAdapter(child: SizedBox.shrink());
                  }
                  return SliverPadding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: ValoraSpacing.lg,
                      vertical: ValoraSpacing.md,
                    ),
                    sliver: SliverList(
                      delegate: SliverChildBuilderDelegate((context, index) {
                        if (index == listings.length) {
                          return Selector<SearchListingsProvider, bool>(
                            selector: (_, p) => p.isLoadingMore,
                            builder: (context, isLoadingMore, _) {
                              if (isLoadingMore) {
                                return const Padding(
                                  padding: EdgeInsets.symmetric(
                                    vertical: ValoraSpacing.lg,
                                  ),
                                  child: ValoraLoadingIndicator(),
                                );
                              }
                              return const SizedBox(height: 80);
                            },
                          );
                        }

                        final listing = listings[index];
                        return RepaintBoundary(
                          child: ValoraListingCardHorizontal(
                            listing: listing,
                            onTap: () => _openListingDetail(listing),
                          ),
                        );
                      }, childCount: listings.length + 1),
                    ),
                  );
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}
