import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import '../screens/property_details_screen.dart';
import 'valora_widgets.dart';

/// Property listing card with image, price, and details.
///
/// Designed for use in list views and grids.
class ValoraListingCard extends StatelessWidget {
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
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return ValoraCard(
      padding: EdgeInsets.zero,
      onTap: onTap ??
          () {
            Navigator.of(context).push(
              MaterialPageRoute(
                builder: (context) => PropertyDetailsScreen(listing: listing),
              ),
            );
          },
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Image Section
          Stack(
            children: [
              _buildImage(isDark),
              if (listing.status != null)
                Positioned(
                  top: ValoraSpacing.sm,
                  left: ValoraSpacing.sm,
                  child: ValoraBadge(
                    label: listing.status!.toUpperCase(),
                    color: _getStatusColor(listing.status!),
                  ),
                ),
              Positioned(
                top: ValoraSpacing.sm,
                right: ValoraSpacing.sm,
                child: _buildFavoriteButton(colorScheme),
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
                if (listing.price != null)
                  ValoraPrice(price: listing.price!),
                const SizedBox(height: ValoraSpacing.sm),

                // Address
                Text(
                  listing.address,
                  style: ValoraTypography.addressDisplay.copyWith(
                    color: colorScheme.onSurface,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: ValoraSpacing.xs),

                // Location
                Text(
                  '${listing.city ?? ''} ${listing.postalCode ?? ''}'.trim(),
                  style: textTheme.bodyMedium?.copyWith(
                    color: colorScheme.onSurfaceVariant,
                  ),
                ),
                const SizedBox(height: ValoraSpacing.md),

                // Specs Row
                _buildSpecsRow(colorScheme),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildImage(bool isDark) {
    return Hero(
      tag: listing.id,
      child: AspectRatio(
        aspectRatio: 16 / 10,
        child: listing.imageUrl != null
            ? CachedNetworkImage(
                imageUrl: listing.imageUrl!,
                fit: BoxFit.cover,
                placeholder: (context, url) =>
                    _buildPlaceholder(isDark, isLoading: true),
                errorWidget: (context, url, error) =>
                    _buildPlaceholder(isDark),
                fadeInDuration: const Duration(milliseconds: 500),
                fadeInCurve: Curves.easeOut,
              )
            : _buildPlaceholder(isDark),
      ),
    );
  }

  Widget _buildPlaceholder(bool isDark, {bool isLoading = false}) {
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

  Widget _buildFavoriteButton(ColorScheme colorScheme) {
    return Material(
      color: Colors.white.withValues(alpha: 0.9),
      shape: const CircleBorder(),
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
            child: Icon(
              isFavorite ? Icons.favorite : Icons.favorite_border,
              key: ValueKey(isFavorite),
              size: ValoraSpacing.iconSizeMd,
              color: isFavorite ? ValoraColors.error : ValoraColors.neutral600,
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildSpecsRow(ColorScheme colorScheme) {
    final specs = <Widget>[];

    if (listing.bedrooms != null) {
      specs.add(_buildSpec(
        Icons.bed_outlined,
        '${listing.bedrooms}',
        colorScheme,
      ));
    }

    if (listing.bathrooms != null) {
      specs.add(_buildSpec(
        Icons.bathtub_outlined,
        '${listing.bathrooms}',
        colorScheme,
      ));
    }

    if (listing.livingAreaM2 != null) {
      specs.add(_buildSpec(
        Icons.square_foot_outlined,
        '${listing.livingAreaM2} m²',
        colorScheme,
      ));
    }

    if (listing.plotAreaM2 != null) {
      specs.add(_buildSpec(
        Icons.landscape_outlined,
        '${listing.plotAreaM2} m²',
        colorScheme,
      ));
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

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'new':
        return ValoraColors.newBadge;
      case 'sold':
      case 'under offer':
        return ValoraColors.soldBadge;
      default:
        return ValoraColors.primary;
    }
  }
}
