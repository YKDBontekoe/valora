import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';

import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../providers/search_listings_provider.dart';
import '../services/api_service.dart';
import '../widgets/home_components.dart';
import '../widgets/valora_filter_dialog.dart';
import '../widgets/valora_glass_container.dart';
import '../widgets/valora_widgets.dart';
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

  SearchListingsProvider? _searchProvider;

  @override
  void initState() {
    super.initState();
    _searchController.addListener(_onSearchChanged);
    _scrollController.addListener(_onScroll);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _searchProvider ??= SearchListingsProvider(
      apiService: context.read<ApiService>(),
    );
  }

  @override
  void dispose() {
    _searchController.dispose();
    _scrollController.dispose();
    _debounce?.cancel();
    _searchProvider?.dispose();
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
    _debounce = Timer(const Duration(milliseconds: 500), () {
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
    final isSelected = (currentSortBy == sortBy &&
            currentSortOrder == sortOrder) ||
        (sortBy == 'date' &&
            sortOrder == 'desc' &&
            (currentSortBy == null || currentSortBy == 'date') &&
            (currentSortOrder == null || currentSortOrder == 'desc'));

    return ListTile(
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
                            padding: const EdgeInsets.fromLTRB(24, 0, 24, 16),
                            child: ValoraTextField(
                              controller: _searchController,
                              label: '',
                              hint: 'City, address, or zip code...',
                              prefixIcon: Icons.search_rounded,
                              textInputAction: TextInputAction.search,
                            ),
                          ),
                          if (provider.hasActiveFiltersOrSort)
                            SizedBox(
                              height: 40,
                              child: ListView(
                                scrollDirection: Axis.horizontal,
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 24,
                                ),
                                children: [
                                  if (provider.minPrice != null ||
                                      provider.maxPrice != null)
                                    Padding(
                                      padding: const EdgeInsets.only(right: 8),
                                      child: ValoraChip(
                                        label:
                                            'Price: €${provider.minPrice?.toInt() ?? 0} - ${provider.maxPrice != null ? '€${provider.maxPrice!.toInt()}' : 'Any'}',
                                        isSelected: true,
                                        onSelected: (_) => _openFilterDialog(),
                                        onDeleted: provider.clearPriceFilter,
                                      ),
                                    ),
                                  if (provider.city != null)
                                    Padding(
                                      padding: const EdgeInsets.only(right: 8),
                                      child: ValoraChip(
                                        label: 'City: ${provider.city}',
                                        isSelected: true,
                                        onSelected: (_) => _openFilterDialog(),
                                        onDeleted: provider.clearCityFilter,
                                      ),
                                    ),
                                  if (provider.minBedrooms != null)
                                    Padding(
                                      padding: const EdgeInsets.only(right: 8),
                                      child: ValoraChip(
                                        label: '${provider.minBedrooms}+ Beds',
                                        isSelected: true,
                                        onSelected: (_) => _openFilterDialog(),
                                        onDeleted: provider.clearBedroomsFilter,
                                      ),
                                    ),
                                  if (provider.minLivingArea != null)
                                    Padding(
                                      padding: const EdgeInsets.only(right: 8),
                                      child: ValoraChip(
                                        label: '${provider.minLivingArea}+ m²',
                                        isSelected: true,
                                        onSelected: (_) => _openFilterDialog(),
                                        onDeleted: provider.clearLivingAreaFilter,
                                      ),
                                    ),
                                  if (provider.isSortActive)
                                    Padding(
                                      padding: const EdgeInsets.only(right: 8),
                                      child: ValoraChip(
                                        label: provider.sortBy == 'price'
                                            ? 'Price: ${provider.sortOrder == 'asc' ? 'Low to High' : 'High to Low'}'
                                            : (provider.sortBy == 'livingarea'
                                                ? 'Area: ${provider.sortOrder == 'asc' ? 'Small to Large' : 'Large to Small'}'
                                                : 'Sort'),
                                        isSelected: true,
                                        onSelected: (_) => _showSortOptions(),
                                        onDeleted: provider.clearSort,
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
                                        onPressed: provider.clearFilters,
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
                        horizontal: 24,
                        vertical: 16,
                      ),
                      sliver: SliverList(
                        delegate: SliverChildBuilderDelegate((context, index) {
                          if (index == provider.listings.length) {
                            if (provider.isLoadingMore) {
                              return const Padding(
                                padding: EdgeInsets.symmetric(vertical: 24),
                                child: Center(
                                  child: CircularProgressIndicator(),
                                ),
                              );
                            }
                            return const SizedBox(height: 80);
                          }

                          final listing = provider.listings[index];
                          return NearbyListingCard(
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
