import 'dart:developer' as developer;

import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';

import '../controllers/search_screen_controller.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import '../models/search_ui_state.dart';
import '../providers/search_listings_provider.dart';
import '../services/api_service.dart';
import '../services/notification_service.dart';
import '../services/pdok_service.dart';
import '../services/property_photo_service.dart';
import '../widgets/home_components.dart';
import '../widgets/search/active_filters_list.dart';
import '../widgets/search/search_input.dart';
import '../widgets/search/sort_options_sheet.dart';
import '../widgets/search/valora_filter_dialog.dart';
import '../widgets/valora_widgets.dart';
import 'listing_detail_screen.dart';
import 'notifications_screen.dart';

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
  late final SearchScreenController _controller;
  late final SearchListingsProvider _searchProvider;

  @override
  void initState() {
    super.initState();
    _pdokService = widget.pdokService ?? PdokService();
    _propertyPhotoService = widget.propertyPhotoService ?? PropertyPhotoService();
    _searchController.addListener(_onSearchChanged);
    _scrollController.addListener(_onScroll);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _controller = context.read<SearchScreenController>();
    _searchProvider = context.read<SearchListingsProvider>();
  }

  @override
  void dispose() {
    _searchController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_controller.shouldLoadMore(
      offset: _scrollController.position.pixels,
      maxExtent: _scrollController.position.maxScrollExtent,
    )) {
      _controller.loadMoreIfNeeded();
    }
  }

  void _onSearchChanged() {
    _controller.onQueryChanged(_searchController.text);
  }

  Future<Listing> _enrichListingWithRealPhotos(Listing listing) async {
    final bool hasPhotos =
        listing.imageUrls.isNotEmpty || (listing.imageUrl?.trim().isNotEmpty ?? false);
    if (hasPhotos || listing.latitude == null || listing.longitude == null) {
      return listing;
    }

    final List<String> photoUrls = _propertyPhotoService.getPropertyPhotos(
      latitude: listing.latitude!,
      longitude: listing.longitude!,
    );

    if (photoUrls.isEmpty) {
      return listing;
    }

    final Map<String, dynamic> serialized = listing.toJson();
    serialized['imageUrl'] = photoUrls.first;
    serialized['imageUrls'] = photoUrls;
    return Listing.fromJson(serialized);
  }

  Future<void> _openListingDetail(Listing listing) async {
    Listing listingToDisplay = listing;
    try {
      if (listing.description == null && listing.features.isEmpty && listing.url != null) {
        listingToDisplay = await context.read<ApiService>().getListing(listing.id);
      }

      listingToDisplay = await _enrichListingWithRealPhotos(listingToDisplay);
    } catch (e, stack) {
      developer.log(
        'Listing enrichment failed for listing ${listing.id}',
        name: 'SearchScreen',
        error: e,
        stackTrace: stack,
      );
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
    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) => SortOptionsSheet(
        provider: _searchProvider,
        onClose: () => Navigator.pop(context),
      ),
    );
  }

  Future<void> _openFilterDialog() async {
    final Map<String, dynamic>? result = await showDialog<Map<String, dynamic>>(
      context: context,
      builder: (context) => ValoraFilterDialog(
        initialMinPrice: _searchProvider.minPrice,
        initialMaxPrice: _searchProvider.maxPrice,
        initialCity: _searchProvider.city,
        initialMinBedrooms: _searchProvider.minBedrooms,
        initialMinLivingArea: _searchProvider.minLivingArea,
        initialMaxLivingArea: _searchProvider.maxLivingArea,
        initialMinSafetyScore: _searchProvider.minSafetyScore,
        initialMinCompositeScore: _searchProvider.minCompositeScore,
        initialSortBy: _searchProvider.sortBy,
        initialSortOrder: _searchProvider.sortOrder,
      ),
    );

    if (result == null) {
      return;
    }

    await _controller.applyFilters(result);
  }

  Future<void> _onSuggestionSelected(PdokSuggestion suggestion) async {
    _searchController.removeListener(_onSearchChanged);
    _searchController.text = suggestion.displayName;
    _searchController.addListener(_onSearchChanged);

    if (suggestion.type == 'adres') {
      if (!mounted) return;
      showDialog(
        context: context,
        barrierDismissible: false,
        builder: (context) => const Center(
          child: ValoraLoadingIndicator(message: 'Loading property details...'),
        ),
      );

      try {
        final Listing? listing = await context.read<ApiService>().getListingFromPdok(
          suggestion.id,
        );

        if (!mounted) return;
        Navigator.pop(context);

        if (listing != null) {
          await _openListingDetail(listing);
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Could not load property details')),
          );
        }
      } catch (e, stack) {
        if (!mounted) return;
        Navigator.pop(context);

        developer.log(
          'Error loading PDOK listing',
          name: 'SearchScreen',
          error: e,
          stackTrace: stack,
        );

        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Something went wrong. Please try again.')),
        );
      }
      return;
    }

    if (suggestion.type == 'woonplaats') {
      _controller.setCityFilter(suggestion.displayName);
      _searchController.removeListener(_onSearchChanged);
      _searchController.clear();
      _searchController.addListener(_onSearchChanged);
    } else {
      _controller.setQuery(suggestion.displayName);
    }
    await _controller.refresh();
  }

  @override
  Widget build(BuildContext context) {
    final bool isDark = Theme.of(context).brightness == Brightness.dark;

    return Consumer<SearchScreenController>(
      builder: (context, controller, _) {
        final SearchUiState state = controller.state;

        if (state.loadMoreErrorMessage != null) {
          WidgetsBinding.instance.addPostFrameCallback((_) {
            if (!mounted) {
              return;
            }
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(state.loadMoreErrorMessage!)),
            );
            controller.consumeLoadMoreError();
          });
        }

        return Scaffold(
          body: RefreshIndicator(
            onRefresh: () => controller.refresh(clearData: false),
            child: CustomScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              controller: _scrollController,
              slivers: [
                _SearchAppBarControls(
                  isDark: isDark,
                  provider: _searchProvider,
                  searchController: _searchController,
                  pdokService: _pdokService,
                  onSuggestionSelected: _onSuggestionSelected,
                  onSubmitted: controller.onQuerySubmitted,
                  onShowSort: _showSortOptions,
                  onShowFilters: _openFilterDialog,
                ),
                _SearchBody(
                  state: state,
                  onRetry: () => controller.refresh(),
                  onListingTap: _openListingDetail,
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}

class _SearchAppBarControls extends StatelessWidget {
  const _SearchAppBarControls({
    required this.isDark,
    required this.provider,
    required this.searchController,
    required this.pdokService,
    required this.onSuggestionSelected,
    required this.onSubmitted,
    required this.onShowSort,
    required this.onShowFilters,
  });

  final bool isDark;
  final SearchListingsProvider provider;
  final TextEditingController searchController;
  final PdokService pdokService;
  final Future<void> Function(PdokSuggestion suggestion) onSuggestionSelected;
  final VoidCallback onSubmitted;
  final VoidCallback onShowSort;
  final VoidCallback onShowFilters;

  @override
  Widget build(BuildContext context) {
    return SliverAppBar(
      pinned: true,
      backgroundColor:
          isDark
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
        Consumer<NotificationService>(
          builder: (context, notificationProvider, _) {
            return Stack(
              children: [
                IconButton(
                  onPressed: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => const NotificationsScreen(),
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
          onPressed: onShowSort,
          icon: const Icon(Icons.sort_rounded),
          tooltip: 'Sort',
        ),
        Stack(
          children: [
            IconButton(
              onPressed: onShowFilters,
              icon: const Icon(Icons.tune_rounded),
              tooltip: 'Filters',
            ),
            if (provider.hasActiveFilters)
              const Positioned(
                top: ValoraSpacing.sm,
                right: ValoraSpacing.sm,
                child: _FilterDot(),
              ),
          ],
        ),
        const SizedBox(width: ValoraSpacing.sm),
      ],
      bottom: PreferredSize(
        preferredSize: Size.fromHeight(provider.hasActiveFiltersOrSort ? 130 : 80),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SearchInput(
              controller: searchController,
              pdokService: pdokService,
              onSuggestionSelected: onSuggestionSelected,
              onSubmitted: onSubmitted,
            ),
            ActiveFiltersList(
              provider: provider,
              onFilterTap: onShowFilters,
              onSortTap: onShowSort,
            ),
            if (provider.hasActiveFiltersOrSort)
              const SizedBox(height: ValoraSpacing.radiusLg),
          ],
        ),
      ),
    );
  }
}

class _FilterDot extends StatelessWidget {
  const _FilterDot();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 10,
      height: 10,
      decoration: const BoxDecoration(
        color: ValoraColors.primary,
        shape: BoxShape.circle,
      ),
    );
  }
}

class _SearchBody extends StatelessWidget {
  const _SearchBody({
    required this.state,
    required this.onRetry,
    required this.onListingTap,
  });

  final SearchUiState state;
  final Future<void> Function() onRetry;
  final Future<void> Function(Listing listing) onListingTap;

  @override
  Widget build(BuildContext context) {
    switch (state.status) {
      case SearchViewStatus.loading:
        return const SliverFillRemaining(
          child: ValoraLoadingIndicator(message: 'Searching...'),
        );
      case SearchViewStatus.error:
        return SliverFillRemaining(
          hasScrollBody: false,
          child: Center(
            child: ValoraEmptyState(
              icon: Icons.error_outline_rounded,
              title: 'Search Failed',
              subtitle: state.errorMessage,
              action: ValoraButton(label: 'Retry', onPressed: onRetry),
            ),
          ),
        );
      case SearchViewStatus.empty:
        return const SliverFillRemaining(
          hasScrollBody: false,
          child: Center(
            child: ValoraEmptyState(
              icon: Icons.search_off_rounded,
              title: 'No results found',
              subtitle: 'Try adjusting your filters or search terms.',
            ),
          ),
        );
      case SearchViewStatus.idle:
        return const SliverFillRemaining(
          hasScrollBody: false,
          child: Center(
            child: ValoraEmptyState(
              icon: Icons.search_rounded,
              title: 'Find your home',
              subtitle: 'Enter a location or use filters to start searching.',
            ),
          ),
        );
      case SearchViewStatus.results:
        return _SearchResultsList(
          listings: state.listings,
          isLoadingMore: state.isLoadingMore,
          onListingTap: onListingTap,
        );
    }
  }
}

class _SearchResultsList extends StatelessWidget {
  const _SearchResultsList({
    required this.listings,
    required this.isLoadingMore,
    required this.onListingTap,
  });

  final List<Listing> listings;
  final bool isLoadingMore;
  final Future<void> Function(Listing listing) onListingTap;

  @override
  Widget build(BuildContext context) {
    return SliverPadding(
      padding: const EdgeInsets.symmetric(
        horizontal: ValoraSpacing.lg,
        vertical: ValoraSpacing.md,
      ),
      sliver: SliverList(
        delegate: SliverChildBuilderDelegate((context, index) {
          if (index == listings.length) {
            return _PaginationFooter(isLoadingMore: isLoadingMore);
          }

          final Listing listing = listings[index];
          return RepaintBoundary(
            child: NearbyListingCard(
              listing: listing,
              onTap: () => onListingTap(listing),
            ),
          );
        }, childCount: listings.length + 1),
      ),
    );
  }
}

class _PaginationFooter extends StatelessWidget {
  const _PaginationFooter({required this.isLoadingMore});

  final bool isLoadingMore;

  @override
  Widget build(BuildContext context) {
    if (isLoadingMore) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: ValoraSpacing.lg),
        child: ValoraLoadingIndicator(),
      );
    }

    return const SizedBox(height: 80);
  }
}
