import 'package:flutter/material.dart';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
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

    return MouseRegion(
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      child: Container(
        margin: const EdgeInsets.only(bottom: 16),
        child: ValoraCard(
          onTap: widget.onTap,
          padding: const EdgeInsets.all(ValoraSpacing.sm),
          borderRadius: ValoraSpacing.radiusLg,
          // Let ValoraCard handle interactive elevation
          elevation: ValoraSpacing.elevationSm,
          child: Row(
            children: [
              // Image
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
                    child: widget.listing.imageUrl != null
                        ? Hero(
                            tag: widget.listing.id,
                            child: CachedNetworkImage(
                              imageUrl: widget.listing.imageUrl!,
                              memCacheWidth: 300,
                              fit: BoxFit.cover,
                              placeholder: (context, url) => const ValoraShimmer(width: 96, height: 96),
                              errorWidget: (context, url, error) => Center(
                                child: Icon(Icons.image_not_supported, color: ValoraColors.neutral400),
                              ),
                            ),
                          )
                        : Center(
                            child: Icon(Icons.home, color: ValoraColors.neutral400),
                          ),
                  )
                  .animate(target: _isHovered ? 1 : 0)
                  .scale(end: const Offset(1.05, 1.05), duration: ValoraAnimations.slow, curve: ValoraAnimations.deceleration),

                  Positioned(
                    top: 4,
                    right: 4,
                    child: Consumer<FavoritesProvider>(
                      builder: (context, favoritesProvider, child) {
                        final isFavorite = favoritesProvider.isFavorite(widget.listing.id);
                        return GestureDetector(
                          onTap: widget.onFavoriteTap ?? () => favoritesProvider.toggleFavorite(widget.listing),
                          child: Container(
                            padding: const EdgeInsets.all(4),
                            decoration: BoxDecoration(
                              color: (isDark ? Colors.black : Colors.white).withValues(alpha: 0.9),
                              shape: BoxShape.circle,
                            ),
                            child: Icon(
                              isFavorite ? Icons.favorite_rounded : Icons.favorite_border_rounded,
                              size: 14,
                              color: isFavorite ? ValoraColors.error : ValoraColors.neutral400,
                            )
                            .animate(target: isFavorite ? 1 : 0)
                            .scale(begin: const Offset(1, 1), end: const Offset(1.2, 1.2), curve: Curves.elasticOut)
                            .then()
                            .scale(end: const Offset(1, 1)),
                          ),
                        );
                      },
                    ),
                  ),
                ],
              ),
              const SizedBox(width: 16),
              // Info
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.symmetric(vertical: 4),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Expanded(
                            child: Text(
                              widget.listing.price != null ? '\$${widget.listing.price!.toStringAsFixed(0)}' : 'Price on request',
                              style: ValoraTypography.titleMedium.copyWith(
                                color: isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
                                fontWeight: FontWeight.bold,
                              ),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                            decoration: BoxDecoration(
                              color: ValoraColors.success.withValues(alpha: 0.1),
                              borderRadius: BorderRadius.circular(4),
                            ),
                            child: const Text(
                              'Active',
                              style: TextStyle(
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
                          color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          _buildFeature(context, Icons.bed_rounded, '${widget.listing.bedrooms ?? 0}'),
                          const SizedBox(width: 8),
                          _buildFeature(context, Icons.shower_rounded, '${widget.listing.bathrooms ?? 0}'),
                          const SizedBox(width: 8),
                          _buildFeature(context, Icons.square_foot_rounded, '${widget.listing.livingAreaM2 ?? 0}'),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
    ));
  }

  Widget _buildFeature(BuildContext context, IconData icon, String label) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Row(
      children: [
        Icon(icon, size: 14, color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500),
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
