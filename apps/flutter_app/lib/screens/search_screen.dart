import 'dart:async';
import 'dart:developer' as developer;

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../models/listing.dart';
import '../providers/search_listings_provider.dart';
import '../services/api_service.dart';
import '../services/pdok_service.dart';
import '../services/property_photo_service.dart';
import '../widgets/search/search_app_bar.dart';
import '../widgets/search/search_results_list.dart';
import '../widgets/search/search_status_slivers.dart';
import '../widgets/search/sort_options_sheet.dart';
import '../widgets/search/valora_filter_dialog.dart';
import '../widgets/valora_widgets.dart';
import 'listing_detail_screen.dart';

class SearchScreen extends StatefulWidget {
  final PdokService? pdokService;

  const SearchScreen({super.key, this.pdokService});

  @override
  State<SearchScreen> createState() => _SearchScreenState();
}

class _SearchScreenState extends State<SearchScreen> {
  final TextEditingController _searchController = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  late final PdokService _pdokService;
  Timer? _debounce;

  SearchListingsProvider? _searchProvider;
  bool _ownsProvider = false;

  @override
  void initState() {
    super.initState();
    _pdokService = widget.pdokService ?? PdokService();
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
        propertyPhotoService: context.read<PropertyPhotoService>(),
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

  Future<void> _openListingDetail(Listing listing) async {
    Listing listingToDisplay = listing;
    try {
      listingToDisplay =
          await _searchProvider!.fetchFullListingDetails(listingToDisplay);
      listingToDisplay =
          await _searchProvider!.enrichListingWithPhotos(listingToDisplay);
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
    return ChangeNotifierProvider<SearchListingsProvider>.value(
      value: _searchProvider!,
      child: Scaffold(
        body: RefreshIndicator(
          onRefresh: () => _searchProvider!.refresh(clearData: false),
          child: CustomScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            controller: _scrollController,
            slivers: [
              SearchAppBar(
                searchController: _searchController,
                pdokService: _pdokService,
                onSuggestionSelected: _onSuggestionSelected,
                onSubmitted: () {
                  _debounce?.cancel();
                  _searchProvider!.refresh();
                },
                onSortTap: _showSortOptions,
                onFilterTap: _openFilterDialog,
              ),
              const SearchLoadingSliver(),
              const SearchErrorSliver(),
              const SearchEmptySliver(),
              SearchResultsList(onListingTap: _openListingDetail),
            ],
          ),
        ),
      ),
    );
  }
}
