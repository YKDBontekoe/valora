import 'package:flutter/material.dart';
import '../core/theme/valora_spacing.dart';
import '../services/api_service.dart';
import '../models/listing.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/valora_listing_card.dart';
import 'listing_detail_screen.dart';

class HomeScreen extends StatefulWidget {
  final ApiService? apiService;

  const HomeScreen({super.key, this.apiService});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  late final ApiService _apiService;
  bool _isConnected = false;
  List<Listing> _listings = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _apiService = widget.apiService ?? ApiService();
    _checkConnection();
  }

  Future<void> _checkConnection() async {
    setState(() => _isLoading = true);
    final connected = await _apiService.healthCheck();
    if (mounted) {
      setState(() {
        _isConnected = connected;
      });
      if (connected) {
        _loadListings();
      } else {
        setState(() => _isLoading = false);
      }
    }
  }

  Future<void> _loadListings() async {
    if (!mounted) return;
    setState(() => _isLoading = true);
    try {
      final listings = await _apiService.getListings();
      if (mounted) {
        setState(() => _listings = listings);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e.toString().replaceAll('Exception: ', '')),
            backgroundColor: Theme.of(context).colorScheme.error,
            action: SnackBarAction(
              label: 'Retry',
              textColor: Colors.white,
              onPressed: _loadListings,
            ),
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(
          'Valora',
          style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                fontWeight: FontWeight.bold,
              ),
        ),
      ),
      body: _isLoading
          ? const ValoraLoadingIndicator(message: 'Connecting...')
          : _buildContent(),
      floatingActionButton: FloatingActionButton(
        onPressed: _checkConnection,
        child: const Icon(Icons.refresh),
      ),
    );
  }

  Widget _buildContent() {
    if (!_isConnected) {
      return ValoraEmptyState(
        icon: Icons.cloud_off_outlined,
        title: 'Backend not connected',
        subtitle: 'Start the API: dotnet run --project Valora.Api',
        action: ValoraButton(
          label: 'Retry',
          variant: ValoraButtonVariant.primary,
          icon: Icons.refresh,
          onPressed: _checkConnection,
        ),
      );
    }

    if (_listings.isEmpty) {
      return const ValoraEmptyState(
        icon: Icons.home_outlined,
        title: 'No listings yet',
        subtitle: 'Properties will appear here once scraped',
      );
    }

    return RefreshIndicator(
      onRefresh: _loadListings,
      child: ListView.separated(
        padding: const EdgeInsets.all(ValoraSpacing.screenPadding),
        itemCount: _listings.length,
        separatorBuilder: (context, index) => const SizedBox(
          height: ValoraSpacing.listItemGap,
        ),
        itemBuilder: (context, index) {
          final listing = _listings[index];
          return ValoraListingCard(
            listing: listing,
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
      ),
    );
  }
}
