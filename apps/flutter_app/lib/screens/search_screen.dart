import 'dart:async';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../providers/favorites_provider.dart';
import '../providers/search_provider.dart';
import '../widgets/home_components.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/valora_filter_dialog.dart';
import 'listing_detail_screen.dart';

class SearchScreen extends StatefulWidget {
  const SearchScreen({super.key});

  @override
  State<SearchScreen> createState() => _SearchScreenState();
}

class _SearchScreenState extends State<SearchScreen> {
  final TextEditingController _searchController = TextEditingController();
  final ScrollController _scrollController = ScrollController();

  // No local state for search logic anymore

  @override
  void initState() {
    super.initState();
    _searchController.addListener(_onSearchChanged);
    _scrollController.addListener(_onScroll);

    // Initialize query from provider if returning
    WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) {
             final provider = Provider.of<SearchProvider>(context, listen: false);
             if (_searchController.text != provider.currentQuery) {
                 _searchController.text = provider.currentQuery;
             }
        }
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    final provider = Provider.of<SearchProvider>(context, listen: false);
    if (_scrollController.position.pixels >= _scrollController.position.maxScrollExtent - 200 &&
        !provider.isLoadingMore &&
        provider.hasNextPage &&
        provider.error == null &&
        (provider.listings.isNotEmpty || provider.currentQuery.isNotEmpty)) {
      provider.loadMoreListings();
    }
  }

  void _onSearchChanged() {
    final provider = Provider.of<SearchProvider>(context, listen: false);
    provider.setQuery(_searchController.text);
  }

  Future<void> _openFilterDialog() async {
    final provider = Provider.of<SearchProvider>(context, listen: false);

    final result = await showDialog<Map<String, dynamic>>(
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

    if (result != null) {
      provider.updateFilters(
        minPrice: result['minPrice'],
        maxPrice: result['maxPrice'],
        city: result['city'],
        minBedrooms: result['minBedrooms'],
        minLivingArea: result['minLivingArea'],
        maxLivingArea: result['maxLivingArea'],
        sortBy: result['sortBy'],
        sortOrder: result['sortOrder'],
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final favoritesProvider = Provider.of<FavoritesProvider>(context);
    final provider = Provider.of<SearchProvider>(context);

    return Scaffold(
      body: CustomScrollView(
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
                color: isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
                fontWeight: FontWeight.bold,
              ),
            ),
            centerTitle: false,
            actions: [
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
              preferredSize: Size.fromHeight(provider.hasActiveFilters ? 130 : 80),
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
                  if (provider.hasActiveFilters)
                    SizedBox(
                      height: 40,
                      child: ListView(
                        scrollDirection: Axis.horizontal,
                        padding: const EdgeInsets.symmetric(horizontal: 24),
                        children: [
                          if (provider.minPrice != null || provider.maxPrice != null)
                            Padding(
                              padding: const EdgeInsets.only(right: 8),
                              child: ValoraChip(
                                label: 'Price: €${provider.minPrice?.toInt() ?? 0} - ${provider.maxPrice != null ? '€${provider.maxPrice!.toInt()}' : 'Any'}',
                                isSelected: true,
                                onSelected: (_) => _openFilterDialog(),
                              ),
                            ),
                          if (provider.city != null)
                             Padding(
                              padding: const EdgeInsets.only(right: 8),
                              child: ValoraChip(
                                label: 'City: ${provider.city}',
                                isSelected: true,
                                onSelected: (_) => _openFilterDialog(),
                              ),
                            ),
                           if (provider.minBedrooms != null)
                             Padding(
                              padding: const EdgeInsets.only(right: 8),
                              child: ValoraChip(
                                label: '${provider.minBedrooms}+ Beds',
                                isSelected: true,
                                onSelected: (_) => _openFilterDialog(),
                              ),
                            ),
                           if (provider.minLivingArea != null)
                             Padding(
                              padding: const EdgeInsets.only(right: 8),
                              child: ValoraChip(
                                label: '${provider.minLivingArea}+ m²',
                                isSelected: true,
                                onSelected: (_) => _openFilterDialog(),
                              ),
                            ),
                        ],
                      ),
                    ),
                  if (provider.hasActiveFilters) const SizedBox(height: 12),
                ],
              ),
            ),
          ),

          if (provider.isLoading)
            const SliverFillRemaining(
              child: ValoraLoadingIndicator(message: 'Searching...'),
            )
          else if (provider.error != null)
             SliverFillRemaining(
              hasScrollBody: false,
              child: Center(
                child: ValoraEmptyState(
                  icon: Icons.error_outline_rounded,
                  title: 'Search Failed',
                  subtitle: provider.error!,
                  action: ValoraButton(
                    label: 'Retry',
                    onPressed: () => provider.loadListings(refresh: true),
                  ),
                ),
              ),
            )
          else if (provider.listings.isEmpty && (provider.currentQuery.isNotEmpty || provider.hasActiveFilters))
            const SliverFillRemaining(
              hasScrollBody: false,
              child: Center(
                child: ValoraEmptyState(
                  icon: Icons.search_off_rounded,
                  title: 'No results found',
                  subtitle: 'Try adjusting your filters or search terms.',
                ),
              ),
            )
          else if (provider.listings.isEmpty && provider.currentQuery.isEmpty && !provider.hasActiveFilters)
             const SliverFillRemaining(
              hasScrollBody: false,
              child: Center(
                child: ValoraEmptyState(
                  icon: Icons.search_rounded,
                  title: 'Find your home',
                  subtitle: 'Enter a location or use filters to start searching.',
                ),
              ),
            )
          else
            SliverPadding(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
              sliver: SliverList(
                delegate: SliverChildBuilderDelegate(
                  (context, index) {
                    if (index == provider.listings.length) {
                       if (provider.isLoadingMore) {
                          return const Padding(
                            padding: EdgeInsets.symmetric(vertical: 24),
                            child: Center(child: CircularProgressIndicator()),
                          );
                       } else {
                         return const SizedBox(height: 80);
                       }
                    }

                    final listing = provider.listings[index];
                    return NearbyListingCard(
                      listing: listing,
                      isFavorite: favoritesProvider.isFavorite(listing.id),
                      onFavoriteToggle: () => favoritesProvider.toggleFavorite(listing),
                      onTap: () {
                        Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) => ListingDetailScreen(listing: listing),
                          ),
                        );
                      },
                    );
                  },
                  childCount: provider.listings.length + 1, // +1 for loader/padding
                ),
              ),
            ),
        ],
      ),
    );
  }
}
