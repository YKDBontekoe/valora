import 'dart:async';
import 'dart:developer' as developer;

import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';

import '../core/formatters/currency_formatter.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import '../providers/search_listings_provider.dart';
import '../services/api_service.dart';
import '../services/property_photo_service.dart';
import '../widgets/home_components.dart';
import '../widgets/valora_filter_dialog.dart';
import '../widgets/valora_glass_container.dart';
import '../widgets/valora_widgets.dart';
import 'package:flutter_typeahead/flutter_typeahead.dart';
import '../services/pdok_service.dart';
import 'listing_detail_screen.dart';

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
      listingToDisplay = await _enrichListingWithRealPhotos(listing);
    } catch (e, stack) {
      developer.log(
        'Photo enrichment failed for listing ${listing.id}',
        name: 'SearchScreen',
        error: e,
        stackTrace: stack,
      );
      listingToDisplay = listing;
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
      builder: (context) => ValoraGlassContainer(
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(ValoraSpacing.radiusXl),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Padding(
              padding: EdgeInsets.all(ValoraSpacing.lg),
              child: Text(
                'Sort By',
                style: ValoraTypography.titleLarge,
                textAlign: TextAlign.center,
              ),
            ),
            _buildSortOption(
              context,
              'Newest',
              'date',
              'desc',
              provider.sortBy,
              provider.sortOrder,
            ),
            _buildSortOption(
              context,
              'Price: Low to High',
              'price',
              'asc',
              provider.sortBy,
              provider.sortOrder,
            ),
            _buildSortOption(
              context,
              'Price: High to Low',
              'price',
              'desc',
              provider.sortBy,
              provider.sortOrder,
            ),
            _buildSortOption(
              context,
              'Area: Small to Large',
              'livingarea',
              'asc',
              provider.sortBy,
              provider.sortOrder,
            ),
            _buildSortOption(
              context,
              'Area: Large to Small',
              'livingarea',
              'desc',
              provider.sortBy,
              provider.sortOrder,
            ),
            _buildSortOption(
              context,
              'Composite Score: High to Low',
              'contextcompositescore',
              'desc',
              provider.sortBy,
              provider.sortOrder,
            ),
            _buildSortOption(
              context,
              'Safety Score: High to Low',
              'contextsafetyscore',
              'desc',
              provider.sortBy,
              provider.sortOrder,
            ),
            SizedBox(height: ValoraSpacing.xl),
          ],
        ),
      ),
    );
  }

  Widget _buildSortOption(
    BuildContext context,
    String label,
    String sortBy,
    String sortOrder,
    String? currentSortBy,
    String? currentSortOrder,
  ) {
    final effectiveSortBy = currentSortBy ?? 'date';
    final effectiveSortOrder = currentSortOrder ?? 'desc';
    final isSelected =
        effectiveSortBy == sortBy && effectiveSortOrder == sortOrder;

    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.lg),
      title: Text(
        label,
        style: isSelected
            ? ValoraTypography.bodyLarge.copyWith(
                color: ValoraColors.primary,
                fontWeight: FontWeight.bold,
              )
            : ValoraTypography.bodyLarge,
      ),
      trailing: isSelected
          ? const Icon(Icons.check_rounded, color: ValoraColors.primary)
          : null,
      onTap: () {
        _searchProvider!.applyFilters(
          minPrice: _searchProvider!.minPrice,
          maxPrice: _searchProvider!.maxPrice,
          city: _searchProvider!.city,
          minBedrooms: _searchProvider!.minBedrooms,
          minLivingArea: _searchProvider!.minLivingArea,
          maxLivingArea: _searchProvider!.maxLivingArea,
          minSafetyScore: _searchProvider!.minSafetyScore,
          minCompositeScore: _searchProvider!.minCompositeScore,
          sortBy: sortBy,
          sortOrder: sortOrder,
        );
        Navigator.pop(context);
      },
    );
  }

  Future<void> _openFilterDialog() async {
    final SearchListingsProvider provider = _searchProvider!;

    final Map<String, dynamic>? result = await showDialog<Map<String, dynamic>>(
      context: context,
      builder: (context) => ValoraFilterDialog(
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

  String _priceChipLabel(double? minPrice, double? maxPrice) {
    final min = CurrencyFormatter.formatEur(minPrice ?? 0);
    final max = maxPrice != null
        ? CurrencyFormatter.formatEur(maxPrice)
        : 'Any';
    return 'Price: $min - $max';
  }

  String _sortChipLabel(String? sortBy, String? sortOrder) {
    switch (sortBy) {
      case 'price':
        return 'Price: ${sortOrder == 'asc' ? 'Low to High' : 'High to Low'}';
      case 'livingarea':
        return 'Area: ${sortOrder == 'asc' ? 'Small to Large' : 'Large to Small'}';
      case 'contextcompositescore':
        return 'Composite';
      case 'contextsafetyscore':
        return 'Safety';
      default:
        return 'Sort';
    }
  }

  @override
  Widget build(BuildContext context) {
    final bool isDark = Theme.of(context).brightness == Brightness.dark;

    return ChangeNotifierProvider<SearchListingsProvider>.value(
      value: _searchProvider!,
      child: Scaffold(
        body: RefreshIndicator(
          onRefresh: _searchProvider!.refresh,
          child: CustomScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            controller: _scrollController,
            slivers: [
              // Selector for AppBar to avoid rebuilding listing list on filter changes
              Selector<SearchListingsProvider, _SearchAppBarState>(
                selector: (_, p) => _SearchAppBarState(
                  hasActiveFiltersOrSort: p.hasActiveFiltersOrSort,
                  minPrice: p.minPrice,
                  maxPrice: p.maxPrice,
                  city: p.city,
                  minBedrooms: p.minBedrooms,
                  minLivingArea: p.minLivingArea,
                  minCompositeScore: p.minCompositeScore,
                  minSafetyScore: p.minSafetyScore,
                  sortBy: p.sortBy,
                  sortOrder: p.sortOrder,
                ),
                builder: (context, value, child) {
                  final provider = _searchProvider!;
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
                          if (provider.hasActiveFilters)
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
                      preferredSize: Size.fromHeight(
                        provider.hasActiveFiltersOrSort ? 130 : 80,
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Padding(
                            padding: const EdgeInsets.fromLTRB(
                              ValoraSpacing.lg,
                              0,
                              ValoraSpacing.lg,
                              ValoraSpacing.md,
                            ),
                            child: TypeAheadField<PdokSuggestion>(
                              controller: _searchController,
                              debounceDuration: const Duration(
                                milliseconds: 400,
                              ),
                              suggestionsCallback: (pattern) async {
                                return await _pdokService.search(pattern);
                              },
                              builder: (context, controller, focusNode) {
                                return ValoraTextField(
                                  controller: controller,
                                  focusNode: focusNode,
                                  label: '',
                                  hint: 'City, address, or zip code...',
                                  prefixIcon: Icons.search_rounded,
                                  textInputAction: TextInputAction.search,
                                  onSubmitted: (_) {
                                    _debounce?.cancel();
                                    _searchProvider!.refresh();
                                  },
                                );
                              },
                              itemBuilder: (context, suggestion) {
                                return ListTile(
                                  leading: const Icon(
                                    Icons.location_on_outlined,
                                  ),
                                  title: Text(suggestion.displayName),
                                  subtitle: Text(suggestion.type),
                                );
                              },
                              onSelected: (suggestion) async {
                                _debounce?.cancel();

                                // Temporarily remove listener to avoid triggering _onSearchChanged
                                _searchController.removeListener(
                                  _onSearchChanged,
                                );
                                _searchController.text = suggestion.displayName;
                                _searchController.addListener(_onSearchChanged);

                                // If it is a specific address (bucket 'adres'), lookup directly
                                if (suggestion.type == 'adres') {
                                  // Show loading indicator
                                  if (!context.mounted) return;
                                  showDialog(
                                    context: context,
                                    barrierDismissible: false,
                                    builder: (context) => const Center(
                                      child: ValoraLoadingIndicator(
                                        message: 'Loading property details...',
                                      ),
                                    ),
                                  );

                                  try {
                                    final listing = await context
                                        .read<ApiService>()
                                        .getListingFromPdok(suggestion.id);

                                    if (!context.mounted) return;
                                    Navigator.pop(
                                      context,
                                    ); // Remove loading indicator

                                    if (listing != null) {
                                      await _openListingDetail(listing);
                                    } else {
                                      ScaffoldMessenger.of(
                                        context,
                                      ).showSnackBar(
                                        const SnackBar(
                                          content: Text(
                                            'Could not load property details',
                                          ),
                                        ),
                                      );
                                    }
                                  } catch (e, stack) {
                                    if (!context.mounted) return;
                                    Navigator.pop(
                                      context,
                                    ); // Remove loading indicator

                                    developer.log(
                                      'Error loading PDOK listing',
                                      name: 'SearchScreen',
                                      error: e,
                                      stackTrace: stack,
                                    );

                                    ScaffoldMessenger.of(context).showSnackBar(
                                      const SnackBar(
                                        content: Text(
                                          'Something went wrong. Please try again.',
                                        ),
                                      ),
                                    );
                                  }
                                }
                                // Otherwise (city, street, etc), just fall back to existing search behavior
                                else {
                                  if (suggestion.type == 'woonplaats') {
                                    _searchProvider!.setCity(
                                      suggestion.displayName,
                                    );
                                    _searchController.clear();
                                  } else {
                                    _searchProvider!.setQuery(
                                      suggestion.displayName,
                                    );
                                  }
                                  _searchProvider!.refresh();
                                }
                              },
                              emptyBuilder: (context) => const Padding(
                                padding: EdgeInsets.all(16.0),
                                child: Text(
                                  'No address found. Try entering a street and number.',
                                ),
                              ),
                            ),
                          ),
                          if (provider.hasActiveFiltersOrSort)
                            SizedBox(
                              height: 40,
                              child: ListView(
                                scrollDirection: Axis.horizontal,
                                padding: const EdgeInsets.symmetric(
                                  horizontal: ValoraSpacing.lg,
                                ),
                                children: [
                                  if (provider.minPrice != null ||
                                      provider.maxPrice != null)
                                    Padding(
                                      padding: const EdgeInsets.only(right: 8),
                                      child: ValoraChip(
                                        label: _priceChipLabel(
                                          provider.minPrice,
                                          provider.maxPrice,
                                        ),
                                        isSelected: true,
                                        onSelected: (_) => _openFilterDialog(),
                                        onDeleted: () => provider
                                            .clearPriceFilter()
                                            .catchError((_) {
                                              if (context.mounted) {
                                                ScaffoldMessenger.of(
                                                  context,
                                                ).showSnackBar(
                                                  const SnackBar(
                                                    content: Text(
                                                      'Failed to clear price filter',
                                                    ),
                                                  ),
                                                );
                                              }
                                            }),
                                      ),
                                    ),
                                  if (provider.city != null)
                                    Padding(
                                      padding: const EdgeInsets.only(right: 8),
                                      child: ValoraChip(
                                        label: 'City: ${provider.city}',
                                        isSelected: true,
                                        onSelected: (_) => _openFilterDialog(),
                                        onDeleted: () => provider
                                            .clearCityFilter()
                                            .catchError((_) {
                                              if (context.mounted) {
                                                ScaffoldMessenger.of(
                                                  context,
                                                ).showSnackBar(
                                                  const SnackBar(
                                                    content: Text(
                                                      'Failed to clear city filter',
                                                    ),
                                                  ),
                                                );
                                              }
                                            }),
                                      ),
                                    ),
                                  if (provider.minBedrooms != null)
                                    Padding(
                                      padding: const EdgeInsets.only(right: 8),
                                      child: ValoraChip(
                                        label: '${provider.minBedrooms}+ Beds',
                                        isSelected: true,
                                        onSelected: (_) => _openFilterDialog(),
                                        onDeleted: () => provider
                                            .clearBedroomsFilter()
                                            .catchError((_) {
                                              if (context.mounted) {
                                                ScaffoldMessenger.of(
                                                  context,
                                                ).showSnackBar(
                                                  const SnackBar(
                                                    content: Text(
                                                      'Failed to clear bedrooms filter',
                                                    ),
                                                  ),
                                                );
                                              }
                                            }),
                                      ),
                                    ),
                                  if (provider.minLivingArea != null)
                                    Padding(
                                      padding: const EdgeInsets.only(right: 8),
                                      child: ValoraChip(
                                        label: '${provider.minLivingArea}+ m²',
                                        isSelected: true,
                                        onSelected: (_) => _openFilterDialog(),
                                        onDeleted: () => provider
                                            .clearLivingAreaFilter()
                                            .catchError((_) {
                                              if (context.mounted) {
                                                ScaffoldMessenger.of(
                                                  context,
                                                ).showSnackBar(
                                                  const SnackBar(
                                                    content: Text(
                                                      'Failed to clear area filter',
                                                    ),
                                                  ),
                                                );
                                              }
                                            }),
                                      ),
                                    ),
                                  if (provider.minCompositeScore != null)
                                    Padding(
                                      padding: const EdgeInsets.only(right: 8),
                                      child: ValoraChip(
                                        label:
                                            'Composite: ${provider.minCompositeScore}+',
                                        isSelected: true,
                                        onSelected: (_) => _openFilterDialog(),
                                        onDeleted: () => provider
                                            .clearCompositeScoreFilter()
                                            .catchError((_) {
                                              if (context.mounted) {
                                                ScaffoldMessenger.of(
                                                  context,
                                                ).showSnackBar(
                                                  const SnackBar(
                                                    content: Text(
                                                      'Failed to clear composite score filter',
                                                    ),
                                                  ),
                                                );
                                              }
                                            }),
                                      ),
                                    ),
                                  if (provider.minSafetyScore != null)
                                    Padding(
                                      padding: const EdgeInsets.only(right: 8),
                                      child: ValoraChip(
                                        label:
                                            'Safety: ${provider.minSafetyScore}+',
                                        isSelected: true,
                                        onSelected: (_) => _openFilterDialog(),
                                        onDeleted: () => provider
                                            .clearSafetyScoreFilter()
                                            .catchError((_) {
                                              if (context.mounted) {
                                                ScaffoldMessenger.of(
                                                  context,
                                                ).showSnackBar(
                                                  const SnackBar(
                                                    content: Text(
                                                      'Failed to clear safety score filter',
                                                    ),
                                                  ),
                                                );
                                              }
                                            }),
                                      ),
                                    ),
                                  if (provider.isSortActive)
                                    Padding(
                                      padding: const EdgeInsets.only(right: 8),
                                      child: ValoraChip(
                                        label: _sortChipLabel(
                                          provider.sortBy,
                                          provider.sortOrder,
                                        ),
                                        isSelected: true,
                                        onSelected: (_) => _showSortOptions(),
                                        onDeleted: () => provider
                                            .clearSort()
                                            .catchError((_) {
                                              if (context.mounted) {
                                                ScaffoldMessenger.of(
                                                  context,
                                                ).showSnackBar(
                                                  const SnackBar(
                                                    content: Text(
                                                      'Failed to clear sort',
                                                    ),
                                                  ),
                                                );
                                              }
                                            }),
                                      ),
                                    ),
                                  if (provider.hasActiveFiltersOrSort)
                                    Padding(
                                      padding: const EdgeInsets.only(left: 4),
                                      child: IconButton(
                                        icon: const Icon(
                                          Icons.clear_all_rounded,
                                          size: 20,
                                        ),
                                        tooltip: 'Clear Filters',
                                        style: IconButton.styleFrom(
                                          backgroundColor: isDark
                                              ? ValoraColors.surfaceVariantDark
                                              : ValoraColors
                                                    .surfaceVariantLight,
                                        ),
                                        onPressed: () =>
                                            provider.clearFilters(),
                                      ),
                                    ),
                                ],
                              ),
                            ),
                          if (provider.hasActiveFiltersOrSort)
                            const SizedBox(height: 12),
                        ],
                      ),
                    ),
                  );
                  },
                ),
              // Selector for Loading/Error states
              Selector<SearchListingsProvider, int>(
                selector: (_, p) =>
                    p.isLoading.hashCode ^
                    p.error.hashCode ^
                    p.listings.length.hashCode ^
                    p.query.hashCode ^
                    p.hasActiveFilters.hashCode,
                builder: (context, value, child) {
                  final provider = _searchProvider!;
                  if (provider.isLoading) {
                    return const SliverFillRemaining(
                      child: ValoraLoadingIndicator(message: 'Searching...'),
                    );
                  } else if (provider.error != null &&
                      provider.listings.isEmpty) {
                    return SliverFillRemaining(
                      hasScrollBody: false,
                      child: Center(
                        child: ValoraEmptyState(
                          icon: Icons.error_outline_rounded,
                          title: 'Search Failed',
                          subtitle: provider.error,
                          action: ValoraButton(
                            label: 'Retry',
                            onPressed: provider.refresh,
                          ),
                        ),
                      ),
                    );
                  } else if (provider.listings.isEmpty &&
                      (provider.query.isNotEmpty ||
                          provider.hasActiveFilters)) {
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
                  } else if (provider.listings.isEmpty &&
                      provider.query.isEmpty &&
                      !provider.hasActiveFilters) {
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
                  return const SliverPadding(padding: EdgeInsets.zero);
                },
              ),
              // Selector for Listings
              Selector<SearchListingsProvider, _ListingListState>(
                selector: (_, p) => _ListingListState(
                  listings: p.listings,
                  isLoadingMore: p.isLoadingMore,
                ),
                builder: (context, state, child) {
                  final listings = state.listings;
                  if (listings.isEmpty) {
                    // Handled by the state selector above
                    return const SliverPadding(padding: EdgeInsets.zero);
                  }
                  return SliverPadding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: ValoraSpacing.lg,
                      vertical: ValoraSpacing.md,
                    ),
                    sliver: SliverList(
                      delegate: SliverChildBuilderDelegate((context, index) {
                        if (index == listings.length) {
                          if (state.isLoadingMore) {
                            return const Padding(
                              padding: EdgeInsets.symmetric(
                                vertical: ValoraSpacing.lg,
                              ),
                              child: ValoraLoadingIndicator(),
                            );
                          }
                          return const SizedBox(height: 80);
                        }

                        final listing = listings[index];
                        return NearbyListingCard(
                              listing: listing,
                              onTap: () => _openListingDetail(listing),
                            )
                            .animate(delay: (50 * (index % 10)).ms)
                            .fade(duration: 400.ms)
                            .slideY(
                              begin: 0.1,
                              end: 0,
                              curve: Curves.easeOut,
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

class _ListingListState {
  final List<Listing> listings;
  final bool isLoadingMore;

  const _ListingListState({
    required this.listings,
    required this.isLoadingMore,
  });

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is _ListingListState &&
          runtimeType == other.runtimeType &&
          listings == other.listings &&
          isLoadingMore == other.isLoadingMore;

  @override
  int get hashCode => listings.hashCode ^ isLoadingMore.hashCode;
}

class _SearchAppBarState {
  final bool hasActiveFiltersOrSort;
  final double? minPrice;
  final double? maxPrice;
  final String? city;
  final int? minBedrooms;
  final int? minLivingArea;
  final double? minCompositeScore;
  final double? minSafetyScore;
  final String? sortBy;
  final String? sortOrder;

  const _SearchAppBarState({
    required this.hasActiveFiltersOrSort,
    this.minPrice,
    this.maxPrice,
    this.city,
    this.minBedrooms,
    this.minLivingArea,
    this.minCompositeScore,
    this.minSafetyScore,
    this.sortBy,
    this.sortOrder,
  });

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is _SearchAppBarState &&
          runtimeType == other.runtimeType &&
          hasActiveFiltersOrSort == other.hasActiveFiltersOrSort &&
          minPrice == other.minPrice &&
          maxPrice == other.maxPrice &&
          city == other.city &&
          minBedrooms == other.minBedrooms &&
          minLivingArea == other.minLivingArea &&
          minCompositeScore == other.minCompositeScore &&
          minSafetyScore == other.minSafetyScore &&
          sortBy == other.sortBy &&
          sortOrder == other.sortOrder;

  @override
  int get hashCode =>
      hasActiveFiltersOrSort.hashCode ^
      minPrice.hashCode ^
      maxPrice.hashCode ^
      city.hashCode ^
      minBedrooms.hashCode ^
      minLivingArea.hashCode ^
      minCompositeScore.hashCode ^
      minSafetyScore.hashCode ^
      sortBy.hashCode ^
      sortOrder.hashCode;
}
