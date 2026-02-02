import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../providers/favorites_provider.dart';
import '../widgets/home_components.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/valora_error_state.dart';
import 'listing_detail_screen.dart';

class SavedListingsScreen extends StatelessWidget {
  const SavedListingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<FavoritesProvider>(
      builder: (context, favoritesProvider, child) {
        final listings = favoritesProvider.favorites;
        final isDark = Theme.of(context).brightness == Brightness.dark;

        return CustomScrollView(
          slivers: [
            SliverAppBar(
              pinned: true,
              backgroundColor: isDark
                  ? ValoraColors.backgroundDark.withValues(alpha: 0.95)
                  : ValoraColors.backgroundLight.withValues(alpha: 0.95),
              surfaceTintColor: Colors.transparent,
              title: Text(
                'Saved Listings',
                style: ValoraTypography.headlineMedium.copyWith(
                  color: isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
                  fontWeight: FontWeight.bold,
                ),
              ),
              centerTitle: false,
            ),
            if (favoritesProvider.isLoading)
              const SliverFillRemaining(
                child: Center(
                  child: CircularProgressIndicator(),
                ),
              )
            else if (listings.isEmpty)
              SliverFillRemaining(
                hasScrollBody: false,
                child: Center(
                  child: ValoraEmptyState(
                    icon: Icons.favorite_border_rounded,
                    title: 'No saved listings',
                    subtitle: 'Listings you save will appear here.',
                    action: const SizedBox.shrink(), // No action needed
                  ),
                ),
              )
            else
              SliverPadding(
                padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
                sliver: SliverList(
                  delegate: SliverChildBuilderDelegate(
                    (context, index) {
                      final listing = listings[index];
                      return NearbyListingCard(
                        listing: listing,
                        onTap: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => ListingDetailScreen(listing: listing),
                            ),
                          );
                        },
                      );
                    },
                    childCount: listings.length,
                  ),
                ),
              ),
            // Add extra padding at bottom
             const SliverToBoxAdapter(child: SizedBox(height: 80)),
          ],
        );
      },
    );
  }
}
