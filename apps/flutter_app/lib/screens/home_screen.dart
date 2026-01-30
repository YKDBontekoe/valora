import 'package:flutter/material.dart';
import '../services/api_service.dart';
import '../models/listing.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  final ApiService _apiService = ApiService();
  bool _isConnected = false;
  List<Listing> _listings = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _checkConnection();
  }

  Future<void> _checkConnection() async {
    final connected = await _apiService.healthCheck();
    setState(() {
      _isConnected = connected;
      _isLoading = false;
    });
    if (connected) {
      _loadListings();
    }
  }

  Future<void> _loadListings() async {
    try {
      final listings = await _apiService.getListings();
      setState(() => _listings = listings);
    } catch (e) {
      debugPrint('Failed to load listings: $e');
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Valora'),
        backgroundColor: Theme.of(context).colorScheme.inversePrimary,
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _buildContent(),
      floatingActionButton: FloatingActionButton(
        onPressed: _checkConnection,
        child: const Icon(Icons.refresh),
      ),
    );
  }

  Widget _buildContent() {
    if (!_isConnected) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.cloud_off, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            const Text('Backend not connected'),
            const SizedBox(height: 8),
            Text(
              'Start the API: dotnet run --project Valora.Api',
              style: TextStyle(color: Colors.grey[600], fontSize: 12),
            ),
          ],
        ),
      );
    }

    if (_listings.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.home_outlined, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            const Text('No listings yet'),
            const SizedBox(height: 8),
            Text(
              'Scraper functionality coming soon',
              style: TextStyle(color: Colors.grey[600]),
            ),
          ],
        ),
      );
    }

    return ListView.builder(
      itemCount: _listings.length,
      itemBuilder: (context, index) {
        final listing = _listings[index];
        return ListTile(
          title: Text(listing.address),
          subtitle: Text('${listing.city ?? ''} ${listing.postalCode ?? ''}'),
          trailing: listing.price != null
              ? Text('€${listing.price!.toStringAsFixed(0)}')
              : null,
        );
      },
    );
  }
}
