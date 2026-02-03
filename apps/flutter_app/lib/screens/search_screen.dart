import 'dart:async';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/exceptions/app_exceptions.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import '../models/listing_filter.dart';
import '../providers/favorites_provider.dart';
import '../services/api_service.dart';
import '../widgets/home_components.dart';
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

  ApiService? _apiService;
  List<Listing> _listings = [];
  bool _isLoading = false;
  String? _error;
  String _currentQuery = '';
  String? _activeSearchQuery;

  @override
  void initState() {
    super.initState();
    _searchController.addListener(_onSearchChanged);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _apiService ??= Provider.of<ApiService>(context);
  }

  @override
  void dispose() {
    _searchController.dispose();
    _scrollController.dispose();
    _debounce?.cancel();
    super.dispose();
  }

  void _onSearchChanged() {
    final query = _searchController.text;
    if (query == _currentQuery) return;
    _currentQuery = query;

    if (_debounce?.isActive ?? false) _debounce!.cancel();
    _debounce = Timer(const Duration(milliseconds: 500), () {
      if (query.isNotEmpty) {
        _performSearch(query);
      } else {
        setState(() {
          _listings = [];
          _isLoading = false;
          _error = null;
          _activeSearchQuery = null;
        });
      }
    });
  }

  Future<void> _performSearch(String query) async {
    _activeSearchQuery = query;
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final response = await _apiService!.getListings(
        ListingFilter(
          searchTerm: query,
          pageSize: 20,
        ),
      );

      if (mounted && _activeSearchQuery == query) {
        setState(() {
          _listings = response.items;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted && _activeSearchQuery == query) {
        setState(() {
          _error = e is AppException ? e.message : 'Failed to search listings';
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final favoritesProvider = Provider.of<FavoritesProvider>(context);

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
            bottom: PreferredSize(
              preferredSize: const Size.fromHeight(80),
              child: Padding(
                padding: const EdgeInsets.fromLTRB(24, 0, 24, 16),
                child: ValoraTextField(
                  controller: _searchController,
                  label: '',
                  hint: 'City, address, or zip code...',
                  prefixIcon: Icons.search_rounded,
                  textInputAction: TextInputAction.search,
                ),
              ),
            ),
          ),

          if (_isLoading)
            const SliverFillRemaining(
              child: ValoraLoadingIndicator(message: 'Searching...'),
            )
          else if (_error != null)
             SliverFillRemaining(
              hasScrollBody: false,
              child: Center(
                child: ValoraEmptyState(
                  icon: Icons.error_outline_rounded,
                  title: 'Search Failed',
                  subtitle: _error,
                  action: ValoraButton(
                    label: 'Retry',
                    onPressed: () => _performSearch(_currentQuery),
                  ),
                ),
              ),
            )
          else if (_listings.isEmpty && _currentQuery.isNotEmpty)
            const SliverFillRemaining(
              hasScrollBody: false,
              child: Center(
                child: ValoraEmptyState(
                  icon: Icons.search_off_rounded,
                  title: 'No results found',
                  subtitle: 'Try adjusting your search terms.',
                ),
              ),
            )
          else if (_listings.isEmpty && _currentQuery.isEmpty)
             const SliverFillRemaining(
              hasScrollBody: false,
              child: Center(
                child: ValoraEmptyState(
                  icon: Icons.search_rounded,
                  title: 'Find your home',
                  subtitle: 'Enter a location to start searching.',
                ),
              ),
            )
          else
            SliverPadding(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
              sliver: SliverList(
                delegate: SliverChildBuilderDelegate(
                  (context, index) {
                    final listing = _listings[index];
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
                  childCount: _listings.length,
                ),
              ),
            ),
             const SliverToBoxAdapter(child: SizedBox(height: 80)),
        ],
      ),
    );
  }
}
