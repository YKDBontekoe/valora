import 'package:flutter/material.dart';
import '../valora_widgets.dart';
import '../valora_error_state.dart';

class HomeLoadingSliver extends StatelessWidget {
  const HomeLoadingSliver({super.key});

  @override
  Widget build(BuildContext context) {
    return const SliverFillRemaining(
      hasScrollBody: false,
      child: Center(child: ValoraLoadingIndicator(message: 'Loading listings...')),
    );
  }
}

class HomeDisconnectedSliver extends StatelessWidget {
  final VoidCallback onRetry;

  const HomeDisconnectedSliver({super.key, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return SliverFillRemaining(
      hasScrollBody: false,
      child: Center(
        child: ValoraEmptyState(
          icon: Icons.cloud_off_outlined,
          title: 'Backend not connected',
          subtitle: 'Unable to connect to Valora Server.',
          action: ValoraButton(
            label: 'Retry',
            variant: ValoraButtonVariant.primary,
            icon: Icons.refresh,
            onPressed: onRetry,
          ),
        ),
      ),
    );
  }
}

class HomeErrorSliver extends StatelessWidget {
  final Object error;
  final VoidCallback onRetry;

  const HomeErrorSliver({super.key, required this.error, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return SliverFillRemaining(
      hasScrollBody: false,
      child: Center(
        child: ValoraErrorState(
          error: error,
          onRetry: onRetry,
        ),
      ),
    );
  }
}

class HomeEmptySliver extends StatelessWidget {
  final bool hasFilters;
  final VoidCallback onScrape;
  final VoidCallback onClearFilters;

  const HomeEmptySliver({
    super.key,
    required this.hasFilters,
    required this.onScrape,
    required this.onClearFilters,
  });

  @override
  Widget build(BuildContext context) {
    return SliverFillRemaining(
      hasScrollBody: false,
      child: Center(
        child: !hasFilters
            ? ValoraEmptyState(
                icon: Icons.home_work_outlined,
                title: 'No listings yet',
                subtitle: 'Get started by scraping some listings.',
                action: ValoraButton(
                  label: 'Scrape 10 Items',
                  variant: ValoraButtonVariant.primary,
                  icon: Icons.cloud_download,
                  onPressed: onScrape,
                ),
              )
            : ValoraEmptyState(
                icon: Icons.search_off,
                title: 'No listings found',
                subtitle: 'Try adjusting your filters or search term.',
                action: ValoraButton(
                  label: 'Clear Filters',
                  variant: ValoraButtonVariant.outline,
                  onPressed: onClearFilters,
                ),
              ),
      ),
    );
  }
}
