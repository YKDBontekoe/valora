import 'dart:ui';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../core/utils/listing_utils.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_animations.dart';
import '../models/listing.dart';
import 'valora_widgets.dart';

/// Property listing card with image, price, and details.
///
/// Designed for use in list views and grids.
class ValoraListingCard extends StatefulWidget {
  const ValoraListingCard({
    super.key,
    required this.listing,
    this.onTap,
    this.onFavorite,
    this.isFavorite = false,
  });

  /// The listing data to display
  final Listing listing;

  /// Tap callback for viewing details
  final VoidCallback? onTap;

  /// Favorite toggle callback
  final VoidCallback? onFavorite;

  /// Whether listing is favorited
  final bool isFavorite;

  @override
  State<ValoraListingCard> createState() => _ValoraListingCardState();
}

class _ValoraListingCardState extends State<ValoraListingCard> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return MouseRegion(
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      child: ValoraCard(
        padding: EdgeInsets.zero,
        onTap: widget.onTap,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Image Section
            Stack(
              children: [
                _ListingImage(
                  imageUrl: widget.listing.imageUrl,
                  listingId: widget.listing.id,
                  isDark: isDark,
                  isHovered: _isHovered,
                ),
                if (widget.listing.status != null)
                  Positioned(
                    top: ValoraSpacing.sm,
                    left: ValoraSpacing.sm,
                    child: ValoraBadge(
                      label: widget.listing.status!.toUpperCase(),
                      color: ListingUtils.getStatusColor(widget.listing.status!),
                    ),
                  ),
                Positioned(
                  top: ValoraSpacing.sm,
                  right: ValoraSpacing.sm,
                  child: _FavoriteButton(
                    isFavorite: widget.isFavorite,
                    onFavorite: widget.onFavorite,
                  ),
                ),
                if (widget.listing.contextCompositeScore != null)
                  Positioned(
                    bottom: ValoraSpacing.sm,
                    right: ValoraSpacing.sm,
                    child: ValoraBadge(
                      label: widget.listing.contextCompositeScore!.toStringAsFixed(1),
                      color: ListingUtils.getScoreColor(
                          widget.listing.contextCompositeScore!),
                      icon: Icons.insights,
                    ),
                  ),
              ],
            ),

            // Content Section
            Padding(
              padding: const EdgeInsets.all(ValoraSpacing.cardPadding),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Price
                  if (widget.listing.price != null)
                    ValoraPrice(price: widget.listing.price!),
                  const SizedBox(height: ValoraSpacing.sm),

                  // Address
                  Text(
                    widget.listing.address,
                    style: ValoraTypography.addressDisplay.copyWith(
                      color: colorScheme.onSurface,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: ValoraSpacing.xs),

                  // Location
                  Text(
                    '${widget.listing.city ?? ''} ${widget.listing.postalCode ?? ''}'
                        .trim(),
                    style: textTheme.bodyMedium?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: ValoraSpacing.md),

                  // Specs Row
                  _ListingSpecs(
                    bedrooms: widget.listing.bedrooms,
                    bathrooms: widget.listing.bathrooms,
                    livingAreaM2: widget.listing.livingAreaM2,
                    plotAreaM2: widget.listing.plotAreaM2,
                    colorScheme: colorScheme,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ListingImage extends StatelessWidget {
  const _ListingImage({
    required this.imageUrl,
    required this.listingId,
    required this.isDark,
    required this.isHovered,
  });

  final String? imageUrl;
  final String listingId;
  final bool isDark;
  final bool isHovered;

  @override
  Widget build(BuildContext context) {
    final validImageUrl = imageUrl?.trim();
    final hasValidImage = validImageUrl != null && validImageUrl.isNotEmpty;

    return Hero(
      tag: listingId,
      child: AspectRatio(
        aspectRatio: 16 / 10,
        child: ClipRect(
          child: (hasValidImage
              ? CachedNetworkImage(
                  imageUrl: validImageUrl,
                  memCacheWidth: 800, // Optimize memory usage
                  fit: BoxFit.cover,
                  placeholder: (context, url) =>
                      _Placeholder(isDark: isDark, isLoading: true),
                  errorWidget: (context, url, error) =>
                      _Placeholder(isDark: isDark),
                  fadeInDuration: const Duration(milliseconds: 500),
                  fadeInCurve: Curves.easeOut,
                )
              : _Placeholder(isDark: isDark))
          .animate(target: isHovered ? 1 : 0)
          .scale(
            end: const Offset(1.05, 1.05),
            duration: ValoraAnimations.slow,
            curve: ValoraAnimations.deceleration,
          ),
        ),
      ),
    );
  }
}

class _Placeholder extends StatelessWidget {
  const _Placeholder({required this.isDark, this.isLoading = false});

  final bool isDark;
  final bool isLoading;

  @override
  Widget build(BuildContext context) {
    if (isLoading) {
      return const ValoraShimmer(
        width: double.infinity,
        height: double.infinity,
        borderRadius: 0,
      );
    }

    return Container(
      color: isDark ? ValoraColors.surfaceVariantDark : ValoraColors.neutral100,
      child: Center(
        child: Icon(
          Icons.home_outlined,
          size: ValoraSpacing.iconSizeXl,
          color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
        ),
      ),
    );
  }
}

class _FavoriteButton extends StatelessWidget {
  const _FavoriteButton({required this.isFavorite, this.onFavorite});

  final bool isFavorite;
  final VoidCallback? onFavorite;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return ClipOval(
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 8, sigmaY: 8),
        child: Material(
          color: (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight)
              .withValues(alpha: 0.7),
          child: InkWell(
            onTap: onFavorite,
            customBorder: const CircleBorder(),
            child: Padding(
              padding: const EdgeInsets.all(ValoraSpacing.sm),
              child: AnimatedSwitcher(
                duration: const Duration(milliseconds: 300),
                transitionBuilder: (child, animation) {
                  return ScaleTransition(scale: animation, child: child);
                },
                child: isFavorite
                    ? Icon(
                        Icons.favorite,
                        key: const ValueKey(true),
                        size: ValoraSpacing.iconSizeMd,
                        color: ValoraColors.error,
                      )
                        .animate()
                        .scale(
                          duration: 400.ms,
                          curve: Curves.elasticOut,
                          begin: const Offset(0.5, 0.5),
                          end: const Offset(1, 1),
                        )
                        .shimmer(delay: 200.ms, duration: 600.ms)
                    : Icon(
                        Icons.favorite_border,
                        key: const ValueKey(false),
                        size: ValoraSpacing.iconSizeMd,
                        color: ValoraColors.neutral600,
                      ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _ListingSpecs extends StatelessWidget {
  const _ListingSpecs({
    this.bedrooms,
    this.bathrooms,
    this.livingAreaM2,
    this.plotAreaM2,
    required this.colorScheme,
  });

  final int? bedrooms;
  final int? bathrooms;
  final int? livingAreaM2;
  final int? plotAreaM2;
  final ColorScheme colorScheme;

  @override
  Widget build(BuildContext context) {
    final specs = <Widget>[];

    if (bedrooms != null) {
      specs.add(_buildSpec(Icons.bed_outlined, '$bedrooms', colorScheme));
    }

    if (bathrooms != null) {
      specs.add(_buildSpec(Icons.bathtub_outlined, '$bathrooms', colorScheme));
    }

    if (livingAreaM2 != null) {
      specs.add(
        _buildSpec(Icons.square_foot_outlined, '$livingAreaM2 m²', colorScheme),
      );
    }

    if (plotAreaM2 != null) {
      specs.add(
        _buildSpec(Icons.landscape_outlined, '$plotAreaM2 m²', colorScheme),
      );
    }

    if (specs.isEmpty) {
      return const SizedBox.shrink();
    }

    return Wrap(
      spacing: ValoraSpacing.md,
      runSpacing: ValoraSpacing.sm,
      children: specs,
    );
  }

  Widget _buildSpec(IconData icon, String value, ColorScheme colorScheme) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(
          icon,
          size: ValoraSpacing.iconSizeSm + 2,
          color: colorScheme.onSurfaceVariant,
        ),
        const SizedBox(width: ValoraSpacing.xs),
        Text(
          value,
          style: ValoraTypography.metadata.copyWith(
            color: colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }
}
