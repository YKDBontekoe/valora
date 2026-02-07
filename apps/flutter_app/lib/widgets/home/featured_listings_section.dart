import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../models/listing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_colors.dart';
import 'featured_listing_card.dart';

class FeaturedListingsSection extends StatelessWidget {
  final List<Listing> listings;
  final Function(Listing) onTap;
  final VoidCallback? onSeeAllTap;

  const FeaturedListingsSection({
    super.key,
    required this.listings,
    required this.onTap,
    this.onSeeAllTap,
  });

  @override
  Widget build(BuildContext context) {
    if (listings.isEmpty) return const SliverToBoxAdapter(child: SizedBox.shrink());

    return SliverToBoxAdapter(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(24, 24, 24, 16),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Featured for You',
                        style: ValoraTypography.titleLarge.copyWith(
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        'Curated by Valora AI based on your taste',
                        style: ValoraTypography.bodySmall.copyWith(
                          color: Theme.of(context).colorScheme.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ),
                ),
                GestureDetector(
                  onTap: onSeeAllTap,
                  child: Text(
                    'See All',
                    style: ValoraTypography.labelSmall.copyWith(
                      color: ValoraColors.primary,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ).animate().fade().slideX(begin: -0.2, end: 0, duration: 400.ms),
          ),
          SizedBox(
            height: 320,
            child: ListView.builder(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
              scrollDirection: Axis.horizontal,
              itemCount: listings.length,
              itemBuilder: (context, index) {
                final listing = listings[index];
                return FeaturedListingCard(
                  key: ValueKey(listing.id),
                  listing: listing,
                  onTap: () => onTap(listing),
                ).animate().fade(duration: 400.ms).slideX(begin: 0.1, end: 0, delay: (50 * index).ms);
              },
            ),
          ),
        ],
      ),
    );
  }
}
