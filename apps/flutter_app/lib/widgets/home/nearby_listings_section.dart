import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../models/listing.dart';
import '../../core/theme/valora_typography.dart';
import 'nearby_listing_card.dart';

class NearbyListingsHeader extends StatelessWidget {
  const NearbyListingsHeader({super.key});

  @override
  Widget build(BuildContext context) {
    return SliverToBoxAdapter(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 32, 24, 16),
        child: Text(
          'Nearby Listings',
          style: ValoraTypography.titleLarge.copyWith(
            fontWeight: FontWeight.bold,
          ),
        ).animate().fade().slideY(begin: 0.2, end: 0, delay: 200.ms),
      ),
    );
  }
}

class NearbyListingsList extends StatelessWidget {
  final List<Listing> listings;
  final bool hasNextPage;
  final double bottomPadding;
  final Function(Listing) onTap;

  const NearbyListingsList({
    super.key,
    required this.listings,
    required this.hasNextPage,
    required this.bottomPadding,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    if (listings.isEmpty && !hasNextPage) return const SliverToBoxAdapter(child: SizedBox.shrink());

    return SliverPadding(
      padding: const EdgeInsets.symmetric(horizontal: 24),
      sliver: SliverList(
        delegate: SliverChildBuilderDelegate(
          (context, index) {
            if (index == listings.length) {
              if (hasNextPage) {
                return Padding(
                  padding: const EdgeInsets.symmetric(vertical: 24),
                  child: Padding(
                    padding: EdgeInsets.only(bottom: bottomPadding),
                    child: const Center(child: CircularProgressIndicator()),
                  ),
                );
              }
              return SizedBox(height: bottomPadding);
            }

            final listing = listings[index];
            return NearbyListingCard(
              key: ValueKey(listing.id),
              listing: listing,
              onTap: () => onTap(listing),
            ).animate().fade(duration: 400.ms).slideY(begin: 0.1, end: 0, delay: (50 * (index % 10)).ms);
          },
          childCount: listings.length + 1,
        ),
      ),
    );
  }
}
