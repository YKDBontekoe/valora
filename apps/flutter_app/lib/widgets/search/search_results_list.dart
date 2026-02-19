import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/theme/valora_spacing.dart';
import '../../models/listing.dart';
import '../../providers/search_listings_provider.dart';
import '../valora_widgets.dart';
import '../home_components.dart';

class SearchResultsList extends StatelessWidget {
  final VoidCallback onRefresh;
  final ValueChanged<Listing> onListingTap;

  const SearchResultsList({
    super.key,
    required this.onRefresh,
    required this.onListingTap,
  });

  @override
  Widget build(BuildContext context) {
    return SliverMainAxisGroup(
      slivers: [
        Selector<SearchListingsProvider, bool>(
          selector: (_, p) => p.isLoading && p.listings.isEmpty,
          builder: (context, isInitialLoading, _) {
            if (isInitialLoading) {
              return const SliverFillRemaining(
                hasScrollBody: false,
                child: Center(
                  child: ValoraLoadingIndicator(message: 'Searching...'),
                ),
              );
            }
            return const SliverToBoxAdapter(child: SizedBox.shrink());
          },
        ),
        Selector<SearchListingsProvider, (String?, bool)>(
          selector: (_, p) => (p.error, p.listings.isEmpty),
          builder: (context, state, _) {
            final error = state.$1;
            final isEmpty = state.$2;
            if (error != null && isEmpty) {
              return SliverFillRemaining(
                hasScrollBody: false,
                child: Center(
                  child: ValoraEmptyState(
                    icon: Icons.error_outline_rounded,
                    title: 'Search Failed',
                    subtitle: error,
                    actionLabel: 'Retry',
                    onAction: onRefresh,
                  ),
                ),
              );
            }
            return const SliverToBoxAdapter(child: SizedBox.shrink());
          },
        ),
        Selector<
          SearchListingsProvider,
          (bool isEmpty, String query, bool hasFilters)
        >(
          selector:
              (_, p) => (
                p.listings.isEmpty && !p.isLoading && p.error == null,
                p.query,
                p.hasActiveFilters,
              ),
          builder: (context, state, _) {
            final isEmpty = state.$1;
            final query = state.$2;
            final hasFilters = state.$3;

            if (!isEmpty) {
              return const SliverToBoxAdapter(child: SizedBox.shrink());
            }

            if (query.isNotEmpty || hasFilters) {
              return const SliverFillRemaining(
                hasScrollBody: false,
                child: Center(
                  child: ValoraEmptyState(
                    icon: Icons.search_off_rounded,
                    title: 'No results found',
                    subtitle: 'Try adjusting your filters or search terms.',
                  ),
                ),
              );
            } else {
              return const SliverFillRemaining(
                hasScrollBody: false,
                child: Center(
                  child: ValoraEmptyState(
                    icon: Icons.search_rounded,
                    title: 'Find your home',
                    subtitle:
                        'Enter a location or use filters to start searching.',
                  ),
                ),
              );
            }
          },
        ),
        Selector<SearchListingsProvider, List<Listing>>(
          selector: (_, p) => p.listings,
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
        ),
      ],
    );
  }
}
