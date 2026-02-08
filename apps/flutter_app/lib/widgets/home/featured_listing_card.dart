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
import '../valora_glass_container.dart';

class FeaturedListingCard extends StatefulWidget {
  final Listing listing;
  final VoidCallback onTap;
  final VoidCallback? onFavoriteTap;
  final int matchPercentage;
  final int priceChangePercent;

  const FeaturedListingCard({
    super.key,
    required this.listing,
    required this.onTap,
    this.onFavoriteTap,
    this.matchPercentage = 98,
    this.priceChangePercent = -2,
  });

  @override
  State<FeaturedListingCard> createState() => _FeaturedListingCardState();
}

class _FeaturedListingCardState extends State<FeaturedListingCard> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      width: 280,
      margin: const EdgeInsets.only(right: ValoraSpacing.lg),
      child: MouseRegion(
        onEnter: (_) => setState(() => _isHovered = true),
        onExit: (_) => setState(() => _isHovered = false),
        child: ValoraCard(
          padding: EdgeInsets.zero,
          onTap: widget.onTap,
          borderRadius: ValoraSpacing.radiusXl,
          elevation: ValoraSpacing.elevationMd,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Image Section
              Stack(
                children: [
                  Container(
                        height: 180,
                        width: double.infinity,
                        color: isDark
                            ? ValoraColors.neutral700
                            : ValoraColors.neutral200,
                        child: widget.listing.imageUrl != null
                            ? Hero(
                                tag: widget.listing.id,
                                child: CachedNetworkImage(
                                  imageUrl: widget.listing.imageUrl!,
                                  memCacheWidth: 800,
                                  width: double.infinity,
                                  height: 180,
                                  fit: BoxFit.cover,
                                  placeholder: (context, url) =>
                                      const ValoraShimmer(
                                        width: double.infinity,
                                        height: 180,
                                      ),
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
                                  size: 48,
                                  color: ValoraColors.neutral400,
                                ),
                              ),
                      )
                      .animate(target: _isHovered ? 1 : 0)
                      .scale(
                        end: const Offset(1.05, 1.05),
                        duration: ValoraAnimations.slow,
                        curve: ValoraAnimations.deceleration,
                      ),
                  // Gradient Overlay
                  Positioned.fill(
                    child: DecoratedBox(
                      decoration: BoxDecoration(
                        gradient: LinearGradient(
                          begin: Alignment.topCenter,
                          end: Alignment.bottomCenter,
                          colors: [
                            ValoraColors.neutral900.withValues(alpha: 0.4),
                            Colors.transparent,
                            Colors.transparent,
                          ],
                          stops: const [0.0, 0.5, 1.0],
                        ),
                      ),
                    ),
                  ),

                  // Match Badge
                  Positioned(
                    top: 12,
                    left: 12,
                    child: ValoraGlassContainer(
                       borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
                       color: ValoraColors.glassBlack.withValues(alpha: 0.4),
                       padding: const EdgeInsets.symmetric(
                            horizontal: 10,
                            vertical: 6,
                          ),
                       child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Container(
                                width: 6,
                                height: 6,
                                decoration: const BoxDecoration(
                                  color: ValoraColors.success,
                                  shape: BoxShape.circle,
                                  boxShadow: [
                                    BoxShadow(
                                      color: ValoraColors.success,
                                      blurRadius: 4,
                                      spreadRadius: 1,
                                    ),
                                  ],
                                ),
                              ),
                              const SizedBox(width: 6),
                              Text(
                                '${widget.matchPercentage}% Match',
                                style: ValoraTypography.labelSmall.copyWith(
                                  color: Colors.white,
                                  fontSize: 10,
                                  letterSpacing: 0.5,
                                ),
                              ),
                            ],
                          ),
                    ),
                  ),

                  // Favorite Button
                  Positioned(
                    top: 12,
                    right: 12,
                    child: Consumer<FavoritesProvider>(
                      builder: (context, favoritesProvider, child) {
                        final isFavorite = favoritesProvider.isFavorite(
                          widget.listing.id,
                        );
                        return Material(
                          color: Colors.transparent,
                          child: InkWell(
                            onTap: widget.onFavoriteTap ??
                                    () => favoritesProvider.toggleFavorite(
                                      widget.listing,
                                    ),
                            customBorder: const CircleBorder(),
                            child: Container(
                              width: 32,
                              height: 32,
                              decoration: BoxDecoration(
                                color:
                                    (isDark
                                            ? ValoraColors.surfaceDark
                                            : ValoraColors.surfaceLight)
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
                                    isFavorite
                                        ? Icons.favorite_rounded
                                        : Icons.favorite_border_rounded,
                                    size: 18,
                                    color: isFavorite
                                        ? ValoraColors.error
                                        : ValoraColors.neutral400,
                                  )
                                  .animate(target: isFavorite ? 1 : 0)
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

              // Info Section
              Padding(
                padding: const EdgeInsets.all(ValoraSpacing.md),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                widget.listing.price != null
                                    ? CurrencyFormatter.formatEur(
                                        widget.listing.price!,
                                      )
                                    : 'Price on request',
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: ValoraTypography.titleLarge.copyWith(
                                  color: isDark
                                      ? ValoraColors.neutral50
                                      : ValoraColors.neutral900,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                widget.listing.city ?? widget.listing.address,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: ValoraTypography.bodySmall.copyWith(
                                  color: isDark
                                      ? ValoraColors.neutral400
                                      : ValoraColors.neutral500,
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(width: 8),
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 8,
                            vertical: 4,
                          ),
                          decoration: BoxDecoration(
                            color: ValoraColors.primary.withValues(alpha: 0.1),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Row(
                            children: [
                              Icon(
                                widget.priceChangePercent < 0
                                    ? Icons.trending_down_rounded
                                    : Icons.trending_up_rounded,
                                size: 14,
                                color: ValoraColors.primary,
                              ),
                              const SizedBox(width: 4),
                              Text(
                                '${widget.priceChangePercent > 0 ? '+' : ''}${widget.priceChangePercent}%',
                                style: ValoraTypography.labelSmall.copyWith(
                                  color: ValoraColors.primary,
                                  fontSize: 12,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: ValoraSpacing.sm),
                    Divider(
                      height: 1,
                      color: isDark
                          ? ValoraColors.neutral800
                          : ValoraColors.neutral100,
                    ),
                    const SizedBox(height: ValoraSpacing.sm),
                    Row(
                      children: [
                        _buildFeature(
                          context,
                          Icons.bed_rounded,
                          '${widget.listing.bedrooms ?? 0} Bd',
                        ),
                        const SizedBox(width: ValoraSpacing.sm),
                        _buildFeature(
                          context,
                          Icons.shower_rounded,
                          '${widget.listing.bathrooms ?? 0} Ba',
                        ),
                        const SizedBox(width: ValoraSpacing.sm),
                        _buildFeature(
                          context,
                          Icons.square_foot_rounded,
                          '${widget.listing.livingAreaM2 ?? 0} m²',
                        ),
                      ],
                    ),
                  ],
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
          size: 16,
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
