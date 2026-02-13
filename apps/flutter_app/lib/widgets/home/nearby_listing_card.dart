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
  static const double _thumbnailSize = 96.0;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return MouseRegion(
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      child: ValoraCard(
        margin: const EdgeInsets.only(bottom: ValoraSpacing.md),
        onTap: widget.onTap,
        padding: const EdgeInsets.all(ValoraSpacing.sm),
        borderRadius: ValoraSpacing.radiusLg,
        elevation: ValoraSpacing.elevationSm,
        child: Row(
          children: [
              // Image
              Stack(
                children: [
                  Container(
                        width: _thumbnailSize,
                        height: _thumbnailSize,
                        decoration: BoxDecoration(
                          borderRadius: BorderRadius.circular(
                            ValoraSpacing.radiusMd,
                          ),
                          color: isDark
                              ? ValoraColors.neutral700
                              : ValoraColors.neutral200,
                        ),
                        clipBehavior: Clip.antiAlias,
                        child: widget.listing.imageUrl != null
                            ? Hero(
                                tag: widget.listing.id,
                                child: CachedNetworkImage(
                                  imageUrl: widget.listing.imageUrl!,
                                  memCacheWidth: 300,
                                  fit: BoxFit.cover,
                                  placeholder: (context, url) =>
                                      const ValoraShimmer(
                                        width: _thumbnailSize,
                                        height: _thumbnailSize,
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

                  Positioned(
                    top: ValoraSpacing.xs,
                    right: ValoraSpacing.xs,
                    child: Consumer<FavoritesProvider>(
                      builder: (context, favoritesProvider, child) {
                        final isFavorite = favoritesProvider.isFavorite(
                          widget.listing.id,
                        );
                        return Semantics(
                          button: true,
                          toggled: isFavorite,
                          label: isFavorite
                              ? 'Remove from saved listings'
                              : 'Save listing',
                          child: Container(
                            decoration: BoxDecoration(
                              color:
                                  (isDark
                                          ? ValoraColors.surfaceDark
                                          : ValoraColors.surfaceLight)
                                      .withValues(alpha: 0.9),
                              shape: BoxShape.circle,
                            ),
                            child: IconButton(
                              constraints: const BoxConstraints(),
                              padding: const EdgeInsets.all(ValoraSpacing.xs),
                              tooltip: isFavorite
                                  ? 'Remove from saved'
                                  : 'Save listing',
                              onPressed:
                                  widget.onFavoriteTap ??
                                  () => favoritesProvider.toggleFavorite(
                                    widget.listing,
                                  ),
                              icon:
                                  Icon(
                                        isFavorite
                                            ? Icons.favorite_rounded
                                            : Icons.favorite_border_rounded,
                                        size: 14,
                                        color: isFavorite
                                            ? ValoraColors.error
                                            : ValoraColors.neutral400,
                                      )
                                      .animate(target: isFavorite ? 1 : 0)
                                      .scale(
                                        begin: const Offset(1, 1),
                                        end: const Offset(1.2, 1.2),
                                        curve: Curves.elasticOut,
                                      )
                                      .then()
                                      .scale(end: const Offset(1, 1)),
                            ),
                          ),
                        );
                      },
                    ),
                  ),
                ],
              ),
              const SizedBox(width: ValoraSpacing.md),
              // Info
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.symmetric(vertical: ValoraSpacing.xs),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Expanded(
                            child: Text(
                              widget.listing.price != null
                                  ? CurrencyFormatter.formatEur(
                                      widget.listing.price!,
                                    )
                                  : 'Price on request',
                              style: ValoraTypography.titleMedium.copyWith(
                                color: isDark
                                    ? ValoraColors.neutral50
                                    : ValoraColors.neutral900,
                                fontWeight: FontWeight.bold,
                              ),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: ValoraSpacing.sm,
                              vertical: 2,
                            ),
                            decoration: BoxDecoration(
                              color: ValoraColors.success.withValues(
                                alpha: 0.1,
                              ),
                              borderRadius: BorderRadius.circular(
                                ValoraSpacing.radiusSm,
                              ),
                            ),
                            child: Text(
                              'Active',
                              style: ValoraTypography.labelSmall.copyWith(
                                color: ValoraColors.success,
                                fontSize: 10,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 2),
                      Text(
                        widget.listing.address,
                        style: ValoraTypography.bodySmall.copyWith(
                          color: isDark
                              ? ValoraColors.neutral400
                              : ValoraColors.neutral500,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      const SizedBox(height: ValoraSpacing.radiusLg), // 12
                      Row(
                        children: [
                          _buildFeature(
                            context,
                            Icons.bed_rounded,
                            '${widget.listing.bedrooms ?? 0}',
                          ),
                          const SizedBox(width: ValoraSpacing.sm),
                          _buildFeature(
                            context,
                            Icons.shower_rounded,
                            '${widget.listing.bathrooms ?? 0}',
                          ),
                          const SizedBox(width: ValoraSpacing.sm),
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
        const SizedBox(width: ValoraSpacing.xs),
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
