import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';

import '../../core/formatters/currency_formatter.dart';
import '../../core/theme/valora_animations.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/listing.dart';
import '../../providers/favorites_provider.dart';
import '../common/valora_card.dart';
import '../common/valora_badge.dart';
import '../common/valora_shimmer.dart';

class NearbyListingCard extends StatelessWidget {
  const NearbyListingCard({
    super.key,
    required this.listing,
    this.onTap,
    this.onFavoriteTap,
  });

  final Listing listing;
  final VoidCallback? onTap;
  final VoidCallback? onFavoriteTap;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return ValoraCard(
      onTap: onTap,
      padding: const EdgeInsets.all(ValoraSpacing.sm),
      borderRadius: ValoraSpacing.radiusLg,
      elevation: ValoraSpacing.elevationSm,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Image Section
          Stack(
            children: [
              Container(
                width: 96,
                height: 96,
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
                  color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200,
                ),
                clipBehavior: Clip.antiAlias,
                child: listing.imageUrl != null
                    ? Hero(
                        tag: 'listing_img_${listing.id}',
                        child: CachedNetworkImage(
                          imageUrl: listing.imageUrl!,
                          memCacheWidth: 300,
                          fit: BoxFit.cover,
                          placeholder: (context, url) => const ValoraShimmer(
                            width: 96,
                            height: 96,
                          ),
                          errorWidget: (context, url, error) => Center(
                            child: Icon(
                              Icons.image_not_supported_rounded,
                              color: ValoraColors.neutral400,
                            ),
                          ),
                        ),
                      )
                    : Center(
                        child: Icon(
                          Icons.home_rounded,
                          color: ValoraColors.neutral400,
                        ),
                      ),
              ),

              // Favorite Button
              Positioned(
                top: 4,
                right: 4,
                child: Consumer<FavoritesProvider>(
                  builder: (context, favoritesProvider, child) {
                    final isFavorite = favoritesProvider.isFavorite(listing.id);
                    return GestureDetector(
                      onTap: onFavoriteTap ?? () => favoritesProvider.toggleFavorite(listing),
                      child: Container(
                        padding: const EdgeInsets.all(4),
                        decoration: BoxDecoration(
                          color: (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight)
                              .withValues(alpha: 0.9),
                          shape: BoxShape.circle,
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withValues(alpha: 0.1),
                              blurRadius: 4,
                              offset: const Offset(0, 2),
                            ),
                          ],
                        ),
                        child: Icon(
                          isFavorite ? Icons.favorite_rounded : Icons.favorite_border_rounded,
                          size: 16,
                          color: isFavorite ? ValoraColors.error : ValoraColors.neutral400,
                        ).animate(target: isFavorite ? 1 : 0)
                        .scale(
                          begin: const Offset(1, 1),
                          end: const Offset(1.2, 1.2),
                          curve: Curves.elasticOut,
                          duration: ValoraAnimations.fast,
                        )
                        .then()
                        .scale(end: const Offset(1, 1), duration: ValoraAnimations.fast),
                      ),
                    );
                  },
                ),
              ),
            ],
          ),

          const SizedBox(width: ValoraSpacing.md),

          // Info Section
          Expanded(
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 2),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Price and Status
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Flexible(
                        child: Text(
                          listing.price != null
                              ? CurrencyFormatter.formatEur(listing.price!)
                              : 'Price on request',
                          style: ValoraTypography.titleMedium.copyWith(
                            color: isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
                            fontWeight: FontWeight.bold,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      const SizedBox(width: ValoraSpacing.xs),
                      ValoraBadge(
                        label: 'Active',
                        color: ValoraColors.success,
                      ).animate().fadeIn(duration: ValoraAnimations.normal),
                    ],
                  ),

                  const SizedBox(height: 2),

                  // Address
                  Text(
                    listing.address,
                    style: ValoraTypography.bodySmall.copyWith(
                      color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),

                  const Spacer(),

                  // Features (Bed/Bath/Sqft)
                  Row(
                    children: [
                      _buildFeature(
                        context,
                        Icons.bed_rounded,
                        '${listing.bedrooms ?? 0}',
                      ),
                      const SizedBox(width: ValoraSpacing.md),
                      _buildFeature(
                        context,
                        Icons.shower_rounded,
                        '${listing.bathrooms ?? 0}',
                      ),
                      const SizedBox(width: ValoraSpacing.md),
                      _buildFeature(
                        context,
                        Icons.square_foot_rounded,
                        '${listing.livingAreaM2 ?? 0}',
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFeature(BuildContext context, IconData icon, String label) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final color = isDark ? ValoraColors.neutral400 : ValoraColors.neutral500;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: color),
        const SizedBox(width: 4),
        Text(
          label,
          style: ValoraTypography.labelSmall.copyWith(
            color: color,
            fontWeight: FontWeight.w500,
          ),
        ),
      ],
    );
  }
}
