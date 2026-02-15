import 'package:flutter/material.dart';
import 'package:share_plus/share_plus.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import '../providers/favorites_provider.dart';
import '../widgets/home_components.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/saved_listings_filter_dialog.dart';
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
  final Set<String> _selectedListingIds = {};
  bool get _isSelectionMode => _selectedListingIds.isNotEmpty;
  Map<String, dynamic> _activeFilters = {};

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

  void _toggleSelection(String id) {
    setState(() {
      if (_selectedListingIds.contains(id)) {
        _selectedListingIds.remove(id);
      } else {
        _selectedListingIds.add(id);
      }
    });
  }

  void _clearSelection() {
    setState(() {
      _selectedListingIds.clear();
    });
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

  Future<void> _openFilterDialog() async {
    final result = await showDialog<Map<String, dynamic>>(
      context: context,
      builder: (context) => SavedListingsFilterDialog(
        initialMinPrice: _activeFilters['minPrice'],
        initialMaxPrice: _activeFilters['maxPrice'],
        initialCity: _activeFilters['city'],
        initialMinBedrooms: _activeFilters['minBedrooms'],
        initialMinLivingArea: _activeFilters['minLivingArea'],
        initialMaxLivingArea: _activeFilters['maxLivingArea'],
        initialMinSafetyScore: _activeFilters['minSafetyScore'],
        initialMinCompositeScore: _activeFilters['minCompositeScore'],
      ),
    );

    if (result != null) {
      setState(() {
        _activeFilters = result;
      });
    }
  }

  void _showSortOptions() {
    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.transparent,
      isScrollControlled: true,
      builder: (context) => Container(
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.surface,
          borderRadius: const BorderRadius.vertical(top: Radius.circular(20)),
        ),
        padding: const EdgeInsets.symmetric(vertical: 24),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 24),
                child: Text(
                  'Sort By',
                  style: ValoraTypography.titleLarge.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
              const SizedBox(height: 16),
              _buildSortOption('Newest Added', 'date_added'),
              _buildSortOption('Price: Low to High', 'price_asc'),
              _buildSortOption('Price: High to Low', 'price_desc'),
              _buildSortOption('City: A-Z', 'city_asc'),
              _buildSortOption('Context Score', 'score_desc'),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSortOption(String label, String value) {
    final isSelected = _sortOrder == value;
    final colorScheme = Theme.of(context).colorScheme;

    return InkWell(
      onTap: () {
        setState(() => _sortOrder = value);
        Navigator.pop(context);
      },
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
        color: isSelected
            ? colorScheme.primary.withValues(alpha: 0.1)
            : Colors.transparent,
        child: Row(
          children: [
            Expanded(
              child: Text(
                label,
                style: ValoraTypography.bodyLarge.copyWith(
                  color: isSelected
                      ? colorScheme.primary
                      : colorScheme.onSurface,
                  fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                ),
              ),
            ),
            if (isSelected)
              Icon(Icons.check_rounded, color: colorScheme.primary),
          ],
        ),
      ),
    );
  }

  Future<void> _deleteSelected(List<Listing> allListings) async {
    final favoritesProvider = Provider.of<FavoritesProvider>(
      context,
      listen: false,
    );
    final count = _selectedListingIds.length;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => ValoraDialog(
        title: 'Delete $count listings?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(context, false),
          ),
          ValoraButton(
            label: 'Delete',
            variant: ValoraButtonVariant.primary,
            onPressed: () => Navigator.pop(context, true),
          ),
        ],
        child: Text(
          'Are you sure you want to remove these $count listings from your saved items?',
        ),
      ),
    );

    if (confirmed == true) {
      final toRemove = allListings
          .where((l) => _selectedListingIds.contains(l.id))
          .toList();
      await favoritesProvider.removeFavorites(toRemove);
      _clearSelection();
    }
  }

  Future<void> _shareListings(List<Listing> listings) async {
    if (listings.isEmpty) return;

    final StringBuffer sb = StringBuffer();
    sb.writeln('Check out these homes I found on Valora:\n');

    for (final listing in listings) {
      sb.writeln('📍 ${listing.address}, ${listing.city}');
      if (listing.price != null) {
        sb.writeln('💰 €${listing.price!.toStringAsFixed(0)}');
      }
      if (listing.url != null) {
        sb.writeln('🔗 ${listing.url}');
      }
      sb.writeln('');
    }

    // ignore: deprecated_member_use
    await Share.share(sb.toString());
  }

  Future<void> _clearAll() async {
    final favoritesProvider = Provider.of<FavoritesProvider>(
      context,
      listen: false,
    );

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => ValoraDialog(
        title: 'Clear all saved listings?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(context, false),
          ),
          ValoraButton(
            label: 'Clear All',
            variant: ValoraButtonVariant.primary,
            onPressed: () => Navigator.pop(context, true),
          ),
        ],
        child: const Text(
          'This will remove all listings from your favorites. This action cannot be undone.',
        ),
      ),
    );

    if (confirmed == true) {
      final allFavorites = List<Listing>.from(favoritesProvider.favorites);
      await favoritesProvider.removeFavorites(allFavorites);
      _clearSelection();
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final theme = Theme.of(context);

    return Consumer<FavoritesProvider>(
      builder: (context, favoritesProvider, child) {
        var listings = favoritesProvider.favorites;

        // Apply Search
        if (_searchQuery.isNotEmpty) {
          listings = listings.where((l) {
            return l.address.toLowerCase().contains(_searchQuery) ||
                (l.city?.toLowerCase().contains(_searchQuery) ?? false) ||
                (l.postalCode?.toLowerCase().contains(_searchQuery) ?? false);
          }).toList();
        }

        // Apply Filters
        if (_activeFilters.isNotEmpty) {
          listings = listings.where((l) {
            final minPrice = _activeFilters['minPrice'] as double?;
            final maxPrice = _activeFilters['maxPrice'] as double?;
            final city = _activeFilters['city'] as String?;
            final minBedrooms = _activeFilters['minBedrooms'] as int?;
            final minLivingArea = _activeFilters['minLivingArea'] as int?;
            final maxLivingArea = _activeFilters['maxLivingArea'] as int?;
            final minSafety = _activeFilters['minSafetyScore'] as double?;
            final minComposite = _activeFilters['minCompositeScore'] as double?;

            if (minPrice != null && (l.price ?? 0) < minPrice) {
              return false;
            }
            if (maxPrice != null && (l.price ?? 0) > maxPrice) {
              return false;
            }
            if (city != null &&
                !(l.city?.toLowerCase().contains(city.toLowerCase()) ??
                    false)) {
              return false;
            }
            if (minBedrooms != null && (l.bedrooms ?? 0) < minBedrooms) {
              return false;
            }
            if (minLivingArea != null &&
                (l.livingAreaM2 ?? 0) < minLivingArea) {
              return false;
            }
            if (maxLivingArea != null &&
                (l.livingAreaM2 ?? 0) > maxLivingArea) {
              return false;
            }
            if (minSafety != null && (l.contextSafetyScore ?? 0) < minSafety) {
              return false;
            }
            if (minComposite != null &&
                (l.contextCompositeScore ?? 0) < minComposite) {
              return false;
            }

            return true;
          }).toList();
        }

        // Apply Sort
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
          case 'score_desc':
            listings.sort((a, b) => (b.contextCompositeScore ?? 0)
                .compareTo(a.contextCompositeScore ?? 0));
            break;
          case 'date_added':
          default:
            listings.sort((a, b) {
              final DateTime aDate = favoritesProvider.savedAtFor(a.id) ??
                  DateTime.fromMillisecondsSinceEpoch(0);
              final DateTime bDate = favoritesProvider.savedAtFor(b.id) ??
                  DateTime.fromMillisecondsSinceEpoch(0);
              return bDate.compareTo(aDate);
            });
            break;
        }

        return PopScope(
          canPop: !_isSelectionMode,
          onPopInvoked: (didPop) {
            if (didPop) {
              return;
            }
            if (_isSelectionMode) {
              _clearSelection();
            }
          },
          child: CustomScrollView(
            slivers: [
              SliverAppBar(
                pinned: true,
                backgroundColor: isDark
                    ? ValoraColors.backgroundDark.withValues(alpha: 0.95)
                    : ValoraColors.backgroundLight.withValues(alpha: 0.95),
                surfaceTintColor: Colors.transparent,
                leading: _isSelectionMode
                    ? IconButton(
                        icon: const Icon(Icons.close_rounded),
                        onPressed: _clearSelection,
                      )
                    : null,
                title: Text(
                  _isSelectionMode
                      ? '${_selectedListingIds.length} Selected'
                      : 'Saved Listings',
                  style: ValoraTypography.headlineMedium.copyWith(
                    color: isDark
                        ? ValoraColors.neutral50
                        : ValoraColors.neutral900,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                centerTitle: false,
                actions: [
                  if (_isSelectionMode) ...[
                    IconButton(
                      icon: const Icon(Icons.share_rounded),
                      tooltip: 'Share Selected',
                      onPressed: () {
                        final selected = listings
                            .where((l) => _selectedListingIds.contains(l.id))
                            .toList();
                        _shareListings(selected);
                      },
                    ),
                    IconButton(
                      icon: const Icon(Icons.delete_outline_rounded),
                      tooltip: 'Remove Selected',
                      onPressed: () {
                        _deleteSelected(listings);
                      },
                    ),
                  ] else ...[
                    IconButton(
                      icon: const Icon(Icons.sort_rounded),
                      tooltip: 'Sort',
                      onPressed: _showSortOptions,
                    ),
                    Stack(
                      children: [
                        IconButton(
                          icon: const Icon(Icons.tune_rounded),
                          tooltip: 'Filter',
                          onPressed: _openFilterDialog,
                        ),
                        if (_activeFilters.values.any((v) => v != null))
                          Positioned(
                            right: 8,
                            top: 8,
                            child: Container(
                              width: 8,
                              height: 8,
                              decoration: const BoxDecoration(
                                color: ValoraColors.primary,
                                shape: BoxShape.circle,
                              ),
                            ),
                          ),
                      ],
                    ),
                    PopupMenuButton<String>(
                      onSelected: (value) {
                        if (value == 'share') {
                          _shareListings(listings);
                        } else if (value == 'clear') {
                          _clearAll();
                        }
                      },
                      itemBuilder: (context) => [
                        const PopupMenuItem(
                          value: 'share',
                          child: Row(
                            children: [
                              Icon(Icons.share_rounded, size: 20),
                              SizedBox(width: 8),
                              Text('Share List'),
                            ],
                          ),
                        ),
                        const PopupMenuItem(
                          value: 'clear',
                          child: Row(
                            children: [
                              Icon(Icons.delete_sweep_rounded, size: 20),
                              SizedBox(width: 8),
                              Text('Clear All'),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
              if (!_isSelectionMode)
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(24, 8, 24, 16),
                    child: ValoraTextField(
                      controller: _searchController,
                      label: '',
                      hint: 'Search saved listings...',
                      prefixIcon: const Icon(Icons.search_rounded),
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
                    icon: _searchQuery.isNotEmpty || _activeFilters.isNotEmpty
                        ? Icons.search_off_rounded
                        : Icons.favorite_border_rounded,
                    title: _searchQuery.isNotEmpty || _activeFilters.isNotEmpty
                        ? 'No matches found'
                        : 'No saved listings',
                    subtitle:
                        _searchQuery.isNotEmpty || _activeFilters.isNotEmpty
                            ? 'Try adjusting your search terms or filters.'
                            : 'Listings you save will appear here.',
                    actionLabel:
                        _searchQuery.isNotEmpty || _activeFilters.isNotEmpty
                            ? 'Clear Filters'
                            : null,
                    onAction:
                        _searchQuery.isNotEmpty || _activeFilters.isNotEmpty
                            ? () {
                                _searchController.clear();
                                setState(() {
                                  _activeFilters.clear();
                                });
                              }
                            : null,
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
                    final isSelected = _selectedListingIds.contains(listing.id);

                    return Padding(
                      padding: const EdgeInsets.only(bottom: 16),
                      child: GestureDetector(
                        onLongPress: () {
                          if (!_isSelectionMode) {
                            _toggleSelection(listing.id);
                          }
                        },
                        onTap: () {
                          if (_isSelectionMode) {
                            _toggleSelection(listing.id);
                          } else {
                            Navigator.push(
                              context,
                              MaterialPageRoute(
                                builder: (context) =>
                                    ListingDetailScreen(listing: listing),
                              ),
                            );
                          }
                        },
                        child: Stack(
                          children: [
                            AbsorbPointer(
                              absorbing: _isSelectionMode,
                              child: NearbyListingCard(
                                listing: listing,
                                onFavoriteTap: () =>
                                    _confirmRemove(context, listing),
                                onTap: () {}, // Handled by GestureDetector
                              ),
                            ),
                            if (_isSelectionMode)
                              Positioned.fill(
                                child: Container(
                                  decoration: BoxDecoration(
                                    color: isSelected
                                        ? theme.colorScheme.primary
                                            .withValues(alpha: 0.1)
                                        : Colors.transparent,
                                    borderRadius: BorderRadius.circular(16),
                                    border: isSelected
                                        ? Border.all(
                                            color: theme.colorScheme.primary,
                                            width: 2,
                                          )
                                        : null,
                                  ),
                                  child: Align(
                                    alignment: Alignment.topRight,
                                    child: Padding(
                                      padding: const EdgeInsets.all(12),
                                      child: Container(
                                        decoration: BoxDecoration(
                                          color: isSelected
                                              ? theme.colorScheme.primary
                                              : theme.colorScheme.surface
                                                  .withValues(alpha: 0.8),
                                          shape: BoxShape.circle,
                                          border: Border.all(
                                            color: isSelected
                                                ? theme.colorScheme.primary
                                                : theme.colorScheme.outline,
                                            width: 2,
                                          ),
                                        ),
                                        child: Padding(
                                          padding: const EdgeInsets.all(4),
                                          child: Icon(
                                            Icons.check_rounded,
                                            size: 16,
                                            color: isSelected
                                                ? theme.colorScheme.onPrimary
                                                : Colors.transparent,
                                          ),
                                        ),
                                      ),
                                    ),
                                  ),
                                ),
                              ),
                          ],
                        ),
                      ),
                    );
                  }, childCount: listings.length),
                ),
              ),
            // Add extra padding at bottom
            const SliverToBoxAdapter(child: SizedBox(height: 80)),
          ],
        ),
      );
    });
  }
}
