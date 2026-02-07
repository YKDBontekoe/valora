import 'package:flutter/material.dart';
import '../valora_widgets.dart';
import '../valora_error_state.dart';
import '../../core/theme/valora_spacing.dart';

class HomeLoadingSliver extends StatelessWidget {
  const HomeLoadingSliver({super.key});

  @override
  Widget build(BuildContext context) {
    return SliverToBoxAdapter(
      child: Padding(
        padding: const EdgeInsets.all(ValoraSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Featured Header
            const ValoraShimmer(width: 150, height: 24),
            const SizedBox(height: ValoraSpacing.xs),
            const ValoraShimmer(width: 200, height: 14),
            const SizedBox(height: ValoraSpacing.lg),

            // Featured Cards Row
            SizedBox(
              height: 300,
              child: ListView.builder(
                scrollDirection: Axis.horizontal,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: 2,
                itemBuilder: (context, index) {
                  return Padding(
                    padding: const EdgeInsets.only(right: 20),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const ValoraShimmer(
                          width: 260,
                          height: 180,
                          borderRadius: ValoraSpacing.radiusXl,
                        ),
                        const SizedBox(height: ValoraSpacing.md),
                        const ValoraShimmer(width: 120, height: 20),
                        const SizedBox(height: ValoraSpacing.xs),
                        const ValoraShimmer(width: 180, height: 16),
                        const SizedBox(height: ValoraSpacing.sm),
                        Row(
                          children: const [
                            ValoraShimmer(width: 40, height: 16),
                            SizedBox(width: 8),
                            ValoraShimmer(width: 40, height: 16),
                            SizedBox(width: 8),
                            ValoraShimmer(width: 60, height: 16),
                          ],
                        ),
                      ],
                    ),
                  );
                },
              ),
            ),
            const SizedBox(height: ValoraSpacing.xl),

            // Nearby Header
            const ValoraShimmer(width: 180, height: 24),
            const SizedBox(height: ValoraSpacing.lg),

            // Nearby List
            ListView.builder(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              itemCount: 3,
              itemBuilder: (context, index) {
                return Padding(
                  padding: const EdgeInsets.only(bottom: ValoraSpacing.md),
                  child: Row(
                    children: [
                      const ValoraShimmer(
                        width: 100,
                        height: 100,
                        borderRadius: ValoraSpacing.radiusMd,
                      ),
                      const SizedBox(width: ValoraSpacing.md),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const ValoraShimmer(width: 100, height: 20),
                            const SizedBox(height: ValoraSpacing.xs),
                            const ValoraShimmer(width: 200, height: 14),
                            const SizedBox(height: ValoraSpacing.md),
                            Row(
                              children: const [
                                ValoraShimmer(width: 30, height: 12),
                                SizedBox(width: 8),
                                ValoraShimmer(width: 30, height: 12),
                                SizedBox(width: 8),
                                ValoraShimmer(width: 50, height: 12),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),
          ],
        ),
      ),
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

  const HomeErrorSliver({
    super.key,
    required this.error,
    required this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    return SliverFillRemaining(
      hasScrollBody: false,
      child: Center(
        child: ValoraErrorState(error: error, onRetry: onRetry),
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
