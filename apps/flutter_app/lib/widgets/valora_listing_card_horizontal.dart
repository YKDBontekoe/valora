import 'package:flutter/material.dart';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../core/formatters/currency_formatter.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_animations.dart';
import '../core/theme/valora_shadows.dart';
import '../models/listing.dart';
import '../providers/favorites_provider.dart';
import 'valora_widgets.dart';

class ValoraListingCardHorizontal extends StatefulWidget {
  final Listing listing;
  final VoidCallback onTap;
  final VoidCallback? onFavoriteTap;

  const ValoraListingCardHorizontal({
    super.key,
    required this.listing,
    required this.onTap,
    this.onFavoriteTap,
  });

  @override
  State<ValoraListingCardHorizontal> createState() =>
      _ValoraListingCardHorizontalState();
}

class _ValoraListingCardHorizontalState
    extends State<ValoraListingCardHorizontal> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colorScheme = Theme.of(context).colorScheme;

    final validImageUrl = widget.listing.imageUrl?.trim();
    final hasValidImage = validImageUrl != null && validImageUrl.isNotEmpty;

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
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Image Section
            Stack(
              children: [
                ClipRRect(
                  borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
                  child: AnimatedScale(
                    scale: _isHovered ? 1.05 : 1.0,
                    duration: ValoraAnimations.slow,
                    curve: ValoraAnimations.deceleration,
                    child: Container(
                      width: ValoraSpacing.thumbnailSizeLg,
                      height: ValoraSpacing.thumbnailSizeLg,
                      color: isDark
                          ? ValoraColors.neutral700
                          : ValoraColors.neutral200,
                      child: hasValidImage
                          ? Hero(
                              tag: widget.listing.id,
                              child: CachedNetworkImage(
                                // ignore: unnecessary_non_null_assertion
                                imageUrl: validImageUrl!,
                                memCacheWidth: 300,
                                fit: BoxFit.cover,
                                placeholder: (context, url) =>
                                    const ValoraShimmer(
                                      width: double.infinity,
                                      height: double.infinity,
                                    ),
                                errorWidget: (context, url, error) => Center(
                                  child: Icon(
                                    Icons.image_not_supported_outlined,
                                    color: ValoraColors.neutral400,
                                  ),
                                ),
                              ),
                            )
                          : Center(
                              child: Icon(
                                Icons.home_outlined,
                                color: ValoraColors.neutral400,
                              ),
                            ),
                    ),
                  ),
                ),
                Positioned(
                  top: ValoraSpacing.xs,
                  right: ValoraSpacing.xs,
                  child: Consumer<FavoritesProvider>(
                    builder: (context, favoritesProvider, child) {
                      final isFavorite = favoritesProvider.isFavorite(
                        widget.listing.id,
                      );
                      return GestureDetector(
                        onTap: widget.onFavoriteTap ??
                            () => favoritesProvider.toggleFavorite(
                                  widget.listing,
                                ),
                        child: Container(
                          padding: const EdgeInsets.all(4),
                          decoration: BoxDecoration(
                            color: (isDark
                                    ? ValoraColors.surfaceDark
                                    : ValoraColors.surfaceLight)
                                .withValues(alpha: 0.8),
                            shape: BoxShape.circle,
                            boxShadow: ValoraShadows.sm,
                          ),
                          child: Icon(
                            isFavorite
                                ? Icons.favorite_rounded
                                : Icons.favorite_border_rounded,
                            size: 16,
                            color: isFavorite
                                ? ValoraColors.error
                                : ValoraColors.neutral500,
                          )
                              .animate(target: isFavorite ? 1 : 0)
                              .scale(
                                duration: 400.ms,
                                curve: Curves.elasticOut,
                                begin: const Offset(1, 1),
                                end: const Offset(1.2, 1.2),
                              )
                              .then()
                              .scale(end: const Offset(1, 1)),
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
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const SizedBox(height: ValoraSpacing.xs),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Flexible(
                        child: Text(
                          widget.listing.price != null
                              ? CurrencyFormatter.formatEur(
                                  widget.listing.price!,
                                )
                              : 'Price on request',
                          style: ValoraTypography.titleMedium.copyWith(
                            color: colorScheme.primary,
                            fontWeight: FontWeight.bold,
                          ),
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      if (widget.listing.status != null &&
                          widget.listing.status!.toLowerCase() != 'active')
                        ValoraBadge(
                          label: widget.listing.status!,
                          size: ValoraBadgeSize.small,
                          color: ValoraColors.neutral500,
                        ),
                    ],
                  ),
                  const SizedBox(height: ValoraSpacing.xs),
                  Text(
                    widget.listing.address,
                    style: ValoraTypography.bodyMedium.copyWith(
                      color: colorScheme.onSurface,
                      fontWeight: FontWeight.w500,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  Text(
                    '${widget.listing.city ?? ''} ${widget.listing.postalCode ?? ''}'
                        .trim(),
                    style: ValoraTypography.bodySmall.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: ValoraSpacing.md),
                  ListingSpecsRow(listing: widget.listing),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class ListingSpecsRow extends StatelessWidget {
  const ListingSpecsRow({super.key, required this.listing});

  final Listing listing;

  @override
  Widget build(BuildContext context) {
    final specs = <Widget>[];

    if (listing.bedrooms != null) {
      specs.add(ListingSpecItem(
        icon: Icons.bed_outlined,
        label: '${listing.bedrooms}',
      ));
    }
    if (listing.bathrooms != null) {
      specs.add(ListingSpecItem(
        icon: Icons.bathtub_outlined,
        label: '${listing.bathrooms}',
      ));
    }
    if (listing.livingAreaM2 != null) {
      specs.add(ListingSpecItem(
        icon: Icons.square_foot_outlined,
        label: '${listing.livingAreaM2} m²',
      ));
    }

    if (specs.isEmpty) return const SizedBox.shrink();

    return Row(
      children: [
        for (int i = 0; i < specs.length; i++) ...[
          specs[i],
          if (i < specs.length - 1) const SizedBox(width: ValoraSpacing.md),
        ],
      ],
    );
  }
}

class ListingSpecItem extends StatelessWidget {
  const ListingSpecItem({super.key, required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(
          icon,
          size: ValoraSpacing.iconSizeSm,
          color: colorScheme.onSurfaceVariant,
        ),
        const SizedBox(width: ValoraSpacing.xs),
        Text(
          label,
          style: ValoraTypography.metadata.copyWith(
            color: colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }
}
