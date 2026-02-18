import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/search_listings_provider.dart';
import '../../widgets/valora_widgets.dart';

class SearchLoadingSliver extends StatelessWidget {
  const SearchLoadingSliver({super.key});

  @override
  Widget build(BuildContext context) {
    return Selector<SearchListingsProvider, bool>(
      selector: (_, p) => p.isLoading,
      builder: (context, isLoading, _) {
        if (isLoading) {
          return const SliverFillRemaining(
            child: ValoraLoadingIndicator(message: 'Searching...'),
          );
        }
        return const SliverToBoxAdapter(child: SizedBox.shrink());
      },
    );
  }
}

class SearchErrorSliver extends StatelessWidget {
  const SearchErrorSliver({super.key});

  @override
  Widget build(BuildContext context) {
    return Selector<SearchListingsProvider, String?>(
      selector: (_, p) => p.error,
      builder: (context, error, _) {
        final provider = context.read<SearchListingsProvider>();
        if (error != null && provider.listings.isEmpty) {
          return SliverFillRemaining(
            hasScrollBody: false,
            child: Center(
              child: ValoraEmptyState(
                icon: Icons.error_outline_rounded,
                title: 'Search Failed',
                subtitle: error,
                actionLabel: 'Retry',
                onAction: provider.refresh,
              ),
            ),
          );
        }
        return const SliverToBoxAdapter(child: SizedBox.shrink());
      },
    );
  }
}

class SearchEmptySliver extends StatelessWidget {
  const SearchEmptySliver({super.key});

  @override
  Widget build(BuildContext context) {
    return Selector<
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
    );
  }
}
