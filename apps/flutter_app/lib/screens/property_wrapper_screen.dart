import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/listing_provider.dart';
import '../repositories/listing_repository.dart';
import '../services/auth_service.dart';
import 'property_detail_screen.dart';

class PropertyWrapperScreen extends StatelessWidget {
  final String listingId;

  const PropertyWrapperScreen({super.key, required this.listingId});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider(
      create: (context) {
        final authService = context.read<AuthService>();
        final repo = ListingRepository(authService);
        final provider = ListingProvider(repo);
        provider.loadListing(listingId);
        return provider;
      },
      child: Consumer<ListingProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return const Scaffold(
              body: Center(child: CircularProgressIndicator()),
            );
          }

          if (provider.error != null) {
            return Scaffold(
              appBar: AppBar(title: const Text('Error')),
              body: Center(child: Text(provider.error!)),
            );
          }

          if (provider.listing == null) {
            return Scaffold(
              appBar: AppBar(title: const Text('Not Found')),
              body: const Center(child: Text('Listing not found')),
            );
          }

          return PropertyDetailScreen(listing: provider.listing!);
        },
      ),
    );
  }
}
