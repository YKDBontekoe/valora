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
import '../providers/favorites_provider.dart';
import '../services/api_service.dart';
import '../services/property_photo_service.dart';
import '../widgets/valora_filter_dialog.dart';
import '../widgets/valora_glass_container.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/valora_listing_card.dart';
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
      // Photo enrichment is best-effort and should never block details.
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
      child: Consumer<SearchListingsProvider>(
        builder: (context, provider, _) {
          return Scaffold(
            body: RefreshIndicator(
              onRefresh: provider.refresh,
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
                  ),
                  if (provider.isLoading)
                    const SliverFillRemaining(
                      child: ValoraLoadingIndicator(message: 'Searching...'),
                    )
                  else if (provider.error != null && provider.listings.isEmpty)
                    SliverFillRemaining(
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
                    )
                  else if (provider.listings.isEmpty &&
                      (provider.query.isNotEmpty || provider.hasActiveFilters))
                    const SliverFillRemaining(
                      hasScrollBody: false,
                      child: Center(
                        child: ValoraEmptyState(
                          icon: Icons.search_off_rounded,
                          title: 'No results found',
                          subtitle:
                              'Try adjusting your filters or search terms.',
                        ),
                      ),
                    )
                  else if (provider.listings.isEmpty &&
                      provider.query.isEmpty &&
                      !provider.hasActiveFilters)
                    const SliverFillRemaining(
                      hasScrollBody: false,
                      child: Center(
                        child: ValoraEmptyState(
                          icon: Icons.search_rounded,
                          title: 'Find your home',
                          subtitle:
                              'Enter a location or use filters to start searching.',
                        ),
                      ),
                    )
                  else
                    SliverPadding(
                      padding: const EdgeInsets.symmetric(
                        horizontal: ValoraSpacing.lg,
                        vertical: ValoraSpacing.md,
                      ),
                      sliver: SliverList(
                        delegate: SliverChildBuilderDelegate((context, index) {
                          if (index == provider.listings.length) {
                            if (provider.isLoadingMore) {
                              return const Padding(
                                padding: EdgeInsets.symmetric(
                                  vertical: ValoraSpacing.lg,
                                ),
                                child: ValoraLoadingIndicator(),
                              );
                            }
                            return const SizedBox(height: 80);
                          }

                          final listing = provider.listings[index];
                          return Padding(
                            padding: const EdgeInsets.only(bottom: ValoraSpacing.md),
                            child: Consumer<FavoritesProvider>(
                              builder: (context, favorites, _) {
                                final isFavorite = favorites.isFavorite(listing.id);
                                return ValoraListingCard(
                                  listing: listing,
                                  onTap: () => _openListingDetail(listing),
                                  isFavorite: isFavorite,
                                  onFavorite: () => favorites.toggleFavorite(listing),
                                );
                              },
                            ),
                          )
                              .animate(delay: (50 * (index % 10)).ms)
                              .fade(duration: 400.ms)
                              .slideY(
                                begin: 0.1,
                                end: 0,
                                curve: Curves.easeOut,
                              );
                        }, childCount: provider.listings.length + 1),
                      ),
                    ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}
