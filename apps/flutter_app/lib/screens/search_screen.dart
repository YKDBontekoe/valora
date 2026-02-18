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

    // Load initial listings (Recent Intelligence)
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _searchProvider?.refresh();
    });
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
        provider.error == null) {
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

    _debounce = Timer(const Duration(milliseconds: 500), () {
      _searchProvider!.refresh();
    });
  }

  Future<void> _loadMoreListings() async {
    await _searchProvider!.loadMore();
  }

  void _openFilterDialog() async {
    final result = await showModalBottomSheet<Map<String, dynamic>>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => ValoraFilterDialog(
        initialMinPrice: _searchProvider!.minPrice,
        initialMaxPrice: _searchProvider!.maxPrice,
        initialCity: _searchProvider!.city,
        initialMinBedrooms: _searchProvider!.minBedrooms,
        initialMinLivingArea: _searchProvider!.minLivingArea,
        initialMaxLivingArea: _searchProvider!.maxLivingArea,
        initialMinCompositeScore: _searchProvider!.minCompositeScore,
        initialMinSafetyScore: _searchProvider!.minSafetyScore,
        initialSortBy: _searchProvider!.sortBy,
        initialSortOrder: _searchProvider!.sortOrder,
      ),
    );

    if (result != null && mounted) {
      await _searchProvider!.applyFilters(
        minPrice: result['minPrice'],
        maxPrice: result['maxPrice'],
        city: result['city'],
        minBedrooms: result['minBedrooms'],
        minLivingArea: result['minLivingArea'],
        maxLivingArea: result['maxLivingArea'],
        minCompositeScore: result['minCompositeScore'],
        minSafetyScore: result['minSafetyScore'],
        sortBy: result['sortBy'],
        sortOrder: result['sortOrder'],
      );
    }
  }

  void _showSortOptions() {
    showModalBottomSheet<void>(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) => SortOptionsSheet(
        provider: _searchProvider!,
        onClose: () => Navigator.pop(context),
      ),
    );
  }

  void _onSuggestionSelected(PdokSuggestion suggestion) {
    _searchController.text = suggestion.displayName;
    _searchProvider!.setQuery(suggestion.displayName);

    // Navigate to lookup
    _openListingLookup(suggestion.id);
  }

  Future<void> _openListingLookup(String pdokId) async {
    // Show loading
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => const Center(child: ValoraLoadingIndicator(message: 'Analyzing property...')),
    );

    try {
      final listing = await context.read<ApiService>().lookupListing(pdokId);
      if (mounted) {
        Navigator.pop(context); // Close loading
        _openListingDetail(listing);
      }
    } catch (e) {
      if (mounted) {
        Navigator.pop(context); // Close loading
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to analyze property: $e')),
        );
      }
    }
  }

  void _openListingDetail(Listing listing) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => ListingDetailScreen(listing: listing),
      ),
    ).then((_) {
      // Refresh to show newly analyzed property in "Recent" list
      _searchProvider?.refresh();
    });
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () => _searchProvider!.refresh(),
          child: CustomScrollView(
            controller: _scrollController,
            physics: const AlwaysScrollableScrollPhysics(),
            slivers: [
              Consumer<SearchListingsProvider>(
                builder: (context, provider, _) {
                  final hasActiveFiltersOrSort = provider.hasActiveFiltersOrSort;
                  return SliverAppBar(
                    floating: true,
                    pinned: false,
                    snap: true,
                    backgroundColor: Theme.of(context).scaffoldBackgroundColor,
                    title: Text(
                      'Market Intelligence',
                      style: ValoraTypography.headlineMedium.copyWith(
                        color: colorScheme.onSurface,
                      ),
                    ),
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
                          icon: Icons.analytics_outlined,
                          title: 'Property Analysis',
                          subtitle:
                              'Enter any address in the Netherlands to get a full Market Intelligence report.',
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

                  final isHubMode = _searchController.text.isEmpty && !_searchProvider!.hasActiveFilters;

                  return SliverPadding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: ValoraSpacing.lg,
                      vertical: ValoraSpacing.md,
                    ),
                    sliver: SliverList(
                      delegate: SliverChildBuilderDelegate((context, index) {
                        if (index == 0 && isHubMode) {
                           return Padding(
                             padding: const EdgeInsets.only(bottom: ValoraSpacing.md),
                             child: Text(
                               'Recent Intelligence',
                               style: ValoraTypography.titleLarge,
                             ),
                           );
                        }

                        final itemIndex = isHubMode ? index - 1 : index;

                        if (itemIndex == listings.length) {
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

                        final listing = listings[itemIndex];
                        return RepaintBoundary(
                          child: ValoraListingCardHorizontal(
                            listing: listing,
                            onTap: () => _openListingDetail(listing),
                          ),
                        );
                      }, childCount: listings.length + (isHubMode ? 2 : 1)),
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
