import 'package:flutter/material.dart';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../../core/formatters/currency_formatter.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';
import '../../models/listing.dart';
import '../../providers/favorites_provider.dart';
import '../valora_widgets.dart';

class NearbyListingCard extends StatefulWidget {
  final Listing listing;
  final VoidCallback onTap;
  final VoidCallback? onFavoriteTap;

  const NearbyListingCard({
    super.key,
    required this.listing,
    required this.onTap,
    this.onFavoriteTap,
  });

  @override
  State<NearbyListingCard> createState() => _NearbyListingCardState();
}

class _NearbyListingCardState extends State<NearbyListingCard> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      margin: const EdgeInsets.only(bottom: ValoraSpacing.md),
      child: MouseRegion(
        onEnter: (_) => setState(() => _isHovered = true),
        onExit: (_) => setState(() => _isHovered = false),
        child: ValoraCard(
          onTap: widget.onTap,
          padding: const EdgeInsets.all(ValoraSpacing.sm),
          borderRadius: ValoraSpacing.radiusLg,
          elevation: ValoraSpacing.elevationSm,
          child: Row(
            children: [
              // Image Section
              Stack(
                children: [
                  ClipRRect(
                    borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
                    child: Container(
                      width: 96,
                      height: 96,
                      color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200,
                      child: widget.listing.imageUrl != null
                          ? Hero(
                              tag: widget.listing.id,
                              child: CachedNetworkImage(
                                imageUrl: widget.listing.imageUrl!,
                                memCacheWidth: 300,
                                fit: BoxFit.cover,
                                placeholder: (context, url) =>
                                    const ValoraShimmer(width: 96, height: 96),
                                errorWidget: (context, url, error) => Center(
                                  child: Icon(
                                    Icons.image_not_supported,
                                    color: ValoraColors.neutral400,
                                  ),
                                ),
                              ),
                            )
                          : Center(
                              child: Icon(
                                Icons.home,
                                color: ValoraColors.neutral400,
                              ),
                            ),
                    ),
                  )
                  .animate(target: _isHovered ? 1 : 0)
                  .scale(
                      end: const Offset(1.05, 1.05),
                      duration: ValoraAnimations.slow,
                      curve: ValoraAnimations.deceleration,
                  ),

                  // Favorite Button
                  Positioned(
                    top: 4,
                    right: 4,
                    child: Consumer<FavoritesProvider>(
                      builder: (context, favoritesProvider, child) {
                        final isFavorite = favoritesProvider.isFavorite(widget.listing.id);
                        return Material(
                          color: Colors.transparent,
                          child: InkWell(
                            onTap: widget.onFavoriteTap ??
                                    () => favoritesProvider.toggleFavorite(widget.listing),
                            customBorder: const CircleBorder(),
                            child: Container(
                              width: 28,
                              height: 28,
                              decoration: BoxDecoration(
                                color: (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight)
                                    .withValues(alpha: 0.9),
                                shape: BoxShape.circle,
                                boxShadow: isFavorite
                                    ? [
                                        BoxShadow(
                                          color: ValoraColors.error.withValues(alpha: 0.2),
                                          blurRadius: 8,
                                          spreadRadius: 2,
                                        )
                                      ]
                                    : null,
                              ),
                              child: Icon(
                                isFavorite ? Icons.favorite_rounded : Icons.favorite_border_rounded,
                                size: 16,
                                color: isFavorite ? ValoraColors.error : ValoraColors.neutral400,
                              ).animate(target: isFavorite ? 1 : 0)
                               .scale(
                                  begin: const Offset(0.8, 0.8),
                                  end: const Offset(1, 1),
                                  curve: Curves.elasticOut,
                                  duration: ValoraAnimations.normal
                               ),
                            ),
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
                  padding: const EdgeInsets.symmetric(vertical: 4),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Expanded(
                            child: Text(
                              widget.listing.price != null
                                  ? CurrencyFormatter.formatEur(widget.listing.price!)
                                  : 'Price on request',
                              style: ValoraTypography.titleMedium.copyWith(
                                color: isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
                                fontWeight: FontWeight.bold,
                              ),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          const ValoraBadge(
                            label: 'Active',
                            color: ValoraColors.success,
                          ),
                        ],
                      ),
                      const SizedBox(height: ValoraSpacing.xs),
                      Text(
                        widget.listing.address,
                        style: ValoraTypography.bodySmall.copyWith(
                          color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      const SizedBox(height: ValoraSpacing.sm),
                      Row(
                        children: [
                          _buildFeature(
                            context,
                            Icons.bed_rounded,
                            '${widget.listing.bedrooms ?? 0}',
                          ),
                          const SizedBox(width: ValoraSpacing.md),
                          _buildFeature(
                            context,
                            Icons.shower_rounded,
                            '${widget.listing.bathrooms ?? 0}',
                          ),
                          const SizedBox(width: ValoraSpacing.md),
                          _buildFeature(
                            context,
                            Icons.square_foot_rounded,
                            '${widget.listing.livingAreaM2 ?? 0}',
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildFeature(BuildContext context, IconData icon, String label) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Row(
      children: [
        Icon(
          icon,
          size: 14,
          color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
        ),
        const SizedBox(width: 4),
        Text(
          label,
          style: ValoraTypography.labelSmall.copyWith(
            color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
          ),
        ),
      ],
    );
  }
}
