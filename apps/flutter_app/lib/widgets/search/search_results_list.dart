import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/theme/valora_spacing.dart';
import '../../models/listing.dart';
import '../../providers/search_listings_provider.dart';
import '../../widgets/valora_listing_card_horizontal.dart';
import '../../widgets/valora_widgets.dart';

class SearchResultsList extends StatelessWidget {
  final Function(Listing) onListingTap;

  const SearchResultsList({super.key, required this.onListingTap});

  @override
  Widget build(BuildContext context) {
    return Selector<SearchListingsProvider, List<Listing>>(
      selector: (_, p) => p.listings,
      shouldRebuild: (prev, next) => prev != next,
      builder: (context, listings, _) {
        if (listings.isEmpty) {
          return const SliverToBoxAdapter(child: SizedBox.shrink());
        }
        return SliverPadding(
          padding: const EdgeInsets.symmetric(
            horizontal: ValoraSpacing.lg,
            vertical: ValoraSpacing.md,
          ),
          sliver: SliverList(
            delegate: SliverChildBuilderDelegate((context, index) {
              if (index == listings.length) {
                return Selector<SearchListingsProvider, bool>(
                  selector: (_, p) => p.isLoadingMore,
                  builder: (context, isLoadingMore, _) {
                    if (isLoadingMore) {
                      return const Padding(
                        padding: EdgeInsets.symmetric(
                          vertical: ValoraSpacing.lg,
                        ),
                        child: ValoraLoadingIndicator(),
                      );
                    }
                    return const SizedBox(height: 80);
                  },
                );
              }

              final listing = listings[index];
              return RepaintBoundary(
                child: ValoraListingCardHorizontal(
                  listing: listing,
                  onTap: () => onListingTap(listing),
                ),
              );
            }, childCount: listings.length + 1),
          ),
        );
      },
    );
  }
}
