import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import '../providers/favorites_provider.dart';
import '../widgets/home_components.dart';
import '../widgets/valora_widgets.dart';
import 'listing_detail_screen.dart';

class SavedListingsScreen extends StatefulWidget {
  const SavedListingsScreen({super.key});

  @override
  State<SavedListingsScreen> createState() => _SavedListingsScreenState();
}

class _SavedListingsScreenState extends State<SavedListingsScreen> {
  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';
  String _sortOrder = 'date_added'; // 'date_added', 'price_asc', 'price_desc'

  @override
  void initState() {
    super.initState();
    _searchController.addListener(() {
      setState(() {
        _searchQuery = _searchController.text.toLowerCase();
      });
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _confirmRemove(BuildContext context, Listing listing) async {
    final favoritesProvider = Provider.of<FavoritesProvider>(
      context,
      listen: false,
    );

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => ValoraDialog(
        title: 'Remove Favorite?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(context, false),
          ),
          ValoraButton(
            label: 'Remove',
            variant: ValoraButtonVariant.primary,
            onPressed: () => Navigator.pop(context, true),
          ),
        ],
        child: const Text(
          'Are you sure you want to remove this listing from your saved items?',
        ),
      ),
    );

    if (confirmed == true) {
      await favoritesProvider.toggleFavorite(listing);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Consumer<FavoritesProvider>(
      builder: (context, favoritesProvider, child) {
        var listings = favoritesProvider.favorites;

        // Filter
        if (_searchQuery.isNotEmpty) {
          listings = listings.where((l) {
            return l.address.toLowerCase().contains(_searchQuery) ||
                (l.city?.toLowerCase().contains(_searchQuery) ?? false) ||
                (l.postalCode?.toLowerCase().contains(_searchQuery) ?? false);
          }).toList();
        }

        // Sort
        // Create a copy to sort
        listings = List.from(listings);
        switch (_sortOrder) {
          case 'price_asc':
            listings.sort((a, b) => (a.price ?? 0).compareTo(b.price ?? 0));
            break;
          case 'price_desc':
            listings.sort((a, b) => (b.price ?? 0).compareTo(a.price ?? 0));
            break;
          case 'city_asc':
            listings.sort((a, b) => (a.city ?? '').compareTo(b.city ?? ''));
            break;
          case 'city_desc':
            listings.sort((a, b) => (b.city ?? '').compareTo(a.city ?? ''));
            break;
          case 'date_added':
          default:
            listings.sort((a, b) {
              final DateTime aDate =
                  favoritesProvider.savedAtFor(a.id) ??
                  DateTime.fromMillisecondsSinceEpoch(0);
              final DateTime bDate =
                  favoritesProvider.savedAtFor(b.id) ??
                  DateTime.fromMillisecondsSinceEpoch(0);
              return bDate.compareTo(aDate);
            });
            break;
        }

        return CustomScrollView(
          slivers: [
            SliverAppBar(
              pinned: true,
              backgroundColor: isDark
                  ? ValoraColors.backgroundDark.withValues(alpha: 0.95)
                  : ValoraColors.backgroundLight.withValues(alpha: 0.95),
              surfaceTintColor: Colors.transparent,
              title: Text(
                'Saved Listings',
                style: ValoraTypography.headlineMedium.copyWith(
                  color: isDark
                      ? ValoraColors.neutral50
                      : ValoraColors.neutral900,
                  fontWeight: FontWeight.bold,
                ),
              ),
              centerTitle: false,
            ),
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(24, 8, 24, 16),
                child: Column(
                  children: [
                    ValoraTextField(
                      controller: _searchController,
                      label: '',
                      hint: 'Search saved listings...',
                      prefixIcon: Icons.search_rounded,
                      suffixIcon: _searchController.text.isNotEmpty
                          ? IconButton(
                              icon: const Icon(Icons.clear_rounded),
                              onPressed: () => _searchController.clear(),
                            )
                          : null,
                    ),
                    const SizedBox(height: 12),
                    SingleChildScrollView(
                      scrollDirection: Axis.horizontal,
                      child: Row(
                        children: [
                          _buildSortChip('Newest', 'date_added'),
                          const SizedBox(width: 8),
                          _buildSortChip('Price: Low to High', 'price_asc'),
                          const SizedBox(width: 8),
                          _buildSortChip('Price: High to Low', 'price_desc'),
                          const SizedBox(width: 8),
                          _buildSortChip('City: A-Z', 'city_asc'),
                          const SizedBox(width: 8),
                          _buildSortChip('City: Z-A', 'city_desc'),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
            if (favoritesProvider.isLoading)
              const SliverFillRemaining(
                child: Center(child: CircularProgressIndicator()),
              )
            else if (listings.isEmpty)
              SliverFillRemaining(
                hasScrollBody: false,
                child: Center(
                  child: ValoraEmptyState(
                    icon: _searchQuery.isNotEmpty
                        ? Icons.search_off_rounded
                        : Icons.favorite_border_rounded,
                    title: _searchQuery.isNotEmpty
                        ? 'No matches found'
                        : 'No saved listings',
                    subtitle: _searchQuery.isNotEmpty
                        ? 'Try adjusting your search terms.'
                        : 'Listings you save will appear here.',
                    action: _searchQuery.isNotEmpty
                        ? ValoraButton(
                            label: 'Clear Search',
                            variant: ValoraButtonVariant.secondary,
                            onPressed: () => _searchController.clear(),
                          )
                        : const SizedBox.shrink(),
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
                    final listing = listings[index];
                    return NearbyListingCard(
                      listing: listing,
                      onFavoriteTap: () => _confirmRemove(context, listing),
                      onTap: () {
                        Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) =>
                                ListingDetailScreen(listing: listing),
                          ),
                        );
                      },
                    );
                  }, childCount: listings.length),
                ),
              ),
            // Add extra padding at bottom
            const SliverToBoxAdapter(child: SizedBox(height: 80)),
          ],
        );
      },
    );
  }

  Widget _buildSortChip(String label, String value) {
    return ValoraChip(
      label: label,
      isSelected: _sortOrder == value,
      onSelected: (selected) {
        if (selected) {
          setState(() {
            _sortOrder = value;
          });
        }
      },
    );
  }
}
