import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/discovery_provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../widgets/common/valora_card.dart';
import '../../widgets/common/valora_empty_state.dart';
import '../../widgets/common/valora_shimmer.dart';
import 'discovery_filters.dart';
import 'discovery_map.dart';

class DiscoveryScreen extends StatefulWidget {
  const DiscoveryScreen({super.key});

  @override
  State<DiscoveryScreen> createState() => _DiscoveryScreenState();
}

class _DiscoveryScreenState extends State<DiscoveryScreen> {
  bool _showMap = false;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Discover Homes'),
        actions: [
          IconButton(
            icon: Icon(_showMap ? Icons.list : Icons.map),
            onPressed: () {
              setState(() {
                _showMap = !_showMap;
              });
            },
          ),
          Builder(
            builder: (context) => IconButton(
              icon: const Icon(Icons.filter_list),
              onPressed: () {
                Scaffold.of(context).openEndDrawer();
              },
            ),
          ),
        ],
      ),
      endDrawer: const Drawer(
        child: SafeArea(
          child: DiscoveryFilters(),
        ),
      ),
      body: Consumer<DiscoveryProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return const _LoadingList();
          }

          if (provider.error != null) {
            return ValoraEmptyState(
              icon: Icons.error_outline,
              title: 'Error loading listings',
              subtitle: provider.error.toString(),
            );
          }

          if (provider.listings.isEmpty) {
            return const ValoraEmptyState(
              icon: Icons.search_off,
              title: 'No homes found',
              subtitle: 'Try adjusting your filters',
            );
          }

          if (_showMap) {
            return DiscoveryMap(listings: provider.listings);
          }

          return ListView.builder(
            padding: const EdgeInsets.all(ValoraSpacing.md),
            itemCount: provider.listings.length,
            itemBuilder: (context, index) {
              final listing = provider.listings[index];
              return Padding(
                padding: const EdgeInsets.only(bottom: ValoraSpacing.md),
                child: ValoraCard(
                  child: Padding(
                    padding: const EdgeInsets.all(ValoraSpacing.md),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        if (listing.imageUrl != null)
                          Container(
                            height: 150,
                            width: double.infinity,
                            decoration: BoxDecoration(
                              borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
                              image: DecorationImage(
                                image: NetworkImage(listing.imageUrl!),
                                fit: BoxFit.cover,
                              ),
                            ),
                          ),
                        const SizedBox(height: ValoraSpacing.sm),
                        Text(
                          listing.address,
                          style: ValoraTypography.titleMedium,
                        ),
                        Text(
                          '${listing.postalCode ?? ''} ${listing.city ?? ''}',
                          style: ValoraTypography.bodyMedium,
                        ),
                        const SizedBox(height: ValoraSpacing.sm),
                        Text(
                          '€${listing.price?.toStringAsFixed(0) ?? 'N/A'}',
                          style: ValoraTypography.titleLarge.copyWith(
                            color: ValoraColors.primary,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              );
            },
          );
        },
      ),
    );
  }
}

class _LoadingList extends StatelessWidget {
  const _LoadingList();

  @override
  Widget build(BuildContext context) {
    return ListView.builder(
      padding: const EdgeInsets.all(ValoraSpacing.md),
      itemCount: 5,
      itemBuilder: (context, index) {
        return const Padding(
          padding: EdgeInsets.only(bottom: ValoraSpacing.md),
          child: ValoraCard(
            child: Padding(
              padding: EdgeInsets.all(ValoraSpacing.md),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  ValoraShimmer(width: double.infinity, height: 150),
                  SizedBox(height: ValoraSpacing.sm),
                  ValoraShimmer(width: 200, height: 20),
                  SizedBox(height: ValoraSpacing.xs),
                  ValoraShimmer(width: 150, height: 16),
                  SizedBox(height: ValoraSpacing.sm),
                  ValoraShimmer(width: 100, height: 24),
                ],
              ),
            ),
          ),
        );
      },
    );
  }
}
