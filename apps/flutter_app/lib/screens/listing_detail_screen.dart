import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:share_plus/share_plus.dart';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:url_launcher/url_launcher.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import '../providers/favorites_provider.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/valora_glass_container.dart';

class ListingDetailScreen extends StatelessWidget {
  const ListingDetailScreen({
    super.key,
    required this.listing,
  });

  final Listing listing;

  Future<void> _openExternalLink(BuildContext context) async {
    final url = listing.url;
    if (url != null) {
      final uri = Uri.parse(url);
      try {
        if (!await launchUrl(uri, mode: LaunchMode.externalApplication)) {
          if (context.mounted) {
            _showErrorSnackBar(context, 'Could not open $url');
          }
        }
      } catch (e) {
        if (context.mounted) {
          _showErrorSnackBar(context, 'Error launching URL: $e');
        }
      }
    }
  }

  void _showErrorSnackBar(BuildContext context, String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: ValoraColors.error,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      extendBodyBehindAppBar: true,
      body: CustomScrollView(
        physics: const BouncingScrollPhysics(),
        slivers: [
          _buildSliverAppBar(context, isDark),
          SliverToBoxAdapter(
            child: ValoraGlassContainer(
              margin: const EdgeInsets.all(ValoraSpacing.md),
              padding: const EdgeInsets.all(ValoraSpacing.lg),
              borderRadius: BorderRadius.circular(ValoraSpacing.radiusXl),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _buildHeader(colorScheme),
                  const SizedBox(height: ValoraSpacing.lg),
                  _buildAddressSection(colorScheme),
                  const SizedBox(height: ValoraSpacing.lg),
                  Divider(color: colorScheme.outlineVariant),
                  const SizedBox(height: ValoraSpacing.lg),
                  _buildSpecsGrid(context, colorScheme),
                  const SizedBox(height: ValoraSpacing.xl),
                  if (listing.url != null)
                    SafeArea(
                      top: false,
                      child: Padding(
                        padding:
                            const EdgeInsets.only(bottom: ValoraSpacing.md),
                        child: ValoraButton(
                          label: 'View on Funda',
                          icon: Icons.open_in_new,
                          isFullWidth: true,
                          onPressed: () => _openExternalLink(context),
                        ),
                      ),
                    ),
                ],
              ),
            ),
          ),
          // Add extra padding at bottom to ensure scrollability
          const SliverToBoxAdapter(child: SizedBox(height: ValoraSpacing.xxl)),
        ],
      ),
    );
  }

  Widget _buildSliverAppBar(BuildContext context, bool isDark) {
    return SliverAppBar(
      expandedHeight: 400,
      pinned: true,
      stretch: true,
      backgroundColor: Colors.transparent,
      iconTheme: const IconThemeData(color: Colors.white),
      actions: [
        if (listing.url != null)
          IconButton(
            // ignore: deprecated_member_use
            onPressed: () => Share.share(listing.url!),
            icon: const Icon(Icons.share_rounded, color: Colors.white),
          ),
        Consumer<FavoritesProvider>(
          builder: (context, favorites, _) {
            final isFav = favorites.isFavorite(listing.id);
            return IconButton(
              onPressed: () => favorites.toggleFavorite(listing),
              icon: Icon(
                isFav ? Icons.favorite_rounded : Icons.favorite_border_rounded,
                color: isFav ? ValoraColors.error : Colors.white,
              ),
            );
          },
        ),
        const SizedBox(width: 8),
      ],
      flexibleSpace: FlexibleSpaceBar(
        background: Hero(
          tag: listing.id,
          child: Stack(
            fit: StackFit.expand,
            children: [
              listing.imageUrl != null
                  ? CachedNetworkImage(
                      imageUrl: listing.imageUrl!,
                      fit: BoxFit.cover,
                      placeholder: (context, url) =>
                          _buildPlaceholder(isDark, isLoading: true),
                      errorWidget: (context, url, error) =>
                          _buildPlaceholder(isDark),
                    )
                  : _buildPlaceholder(isDark),
              // Gradient overlay for text readability
              DecoratedBox(
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topCenter,
                    end: Alignment.bottomCenter,
                    colors: [
                      Colors.black.withValues(alpha: 0.6),
                      Colors.transparent,
                      Colors.transparent,
                      Colors.black.withValues(alpha: 0.7),
                    ],
                    stops: const [0.0, 0.3, 0.7, 1.0],
                  ),
                ),
              ),
            ],
          ),
        ),
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
          size: ValoraSpacing.iconSizeXl * 1.5,
          color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
        ),
      ),
    );
  }

  Widget _buildHeader(ColorScheme colorScheme) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (listing.price != null)
          ValoraPrice(
            price: listing.price!,
            size: ValoraPriceSize.large,
          ),
        if (listing.status != null)
          ValoraBadge(
            label: listing.status!.toUpperCase(),
            color: _getStatusColor(listing.status!),
          ),
      ],
    );
  }

  Widget _buildAddressSection(ColorScheme colorScheme) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          listing.address,
          style: ValoraTypography.headlineSmall.copyWith(
            color: colorScheme.onSurface,
          ),
        ),
        const SizedBox(height: ValoraSpacing.xs),
        Text(
          '${listing.city ?? ''} ${listing.postalCode ?? ''}'.trim(),
          style: ValoraTypography.bodyLarge.copyWith(
            color: colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }

  Widget _buildSpecsGrid(BuildContext context, ColorScheme colorScheme) {
    final specs = <Widget>[];

    if (listing.bedrooms != null) {
      specs.add(_buildSpecItem(
        Icons.bed_rounded,
        'Bedrooms',
        '${listing.bedrooms}',
        colorScheme,
      ));
    }

    if (listing.bathrooms != null) {
      specs.add(_buildSpecItem(
        Icons.shower_rounded,
        'Bathrooms',
        '${listing.bathrooms}',
        colorScheme,
      ));
    }

    if (listing.livingAreaM2 != null) {
      specs.add(_buildSpecItem(
        Icons.square_foot_rounded,
        'Living Area',
        '${listing.livingAreaM2} m²',
        colorScheme,
      ));
    }

    if (listing.plotAreaM2 != null) {
      specs.add(_buildSpecItem(
        Icons.landscape_rounded,
        'Plot Size',
        '${listing.plotAreaM2} m²',
        colorScheme,
      ));
    }

    if (specs.isEmpty) return const SizedBox.shrink();

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      clipBehavior: Clip.none,
      child: Row(
        children: specs.map((widget) => Padding(
          padding: const EdgeInsets.only(right: ValoraSpacing.lg),
          child: widget,
        )).toList(),
      ),
    );
  }

  Widget _buildSpecItem(
    IconData icon,
    String label,
    String value,
    ColorScheme colorScheme,
  ) {
    return Container(
      padding: const EdgeInsets.all(ValoraSpacing.md),
      decoration: BoxDecoration(
        color: colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
        border: Border.all(
          color: colorScheme.outlineVariant.withValues(alpha: 0.5),
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: colorScheme.surface,
              shape: BoxShape.circle,
            ),
            child: Icon(
              icon,
              size: 20,
              color: colorScheme.primary,
            ),
          ),
          const SizedBox(width: ValoraSpacing.md),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                label,
                style: ValoraTypography.labelSmall.copyWith(
                  color: colorScheme.onSurfaceVariant,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                value,
                style: ValoraTypography.titleMedium.copyWith(
                  color: colorScheme.onSurface,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
          const SizedBox(width: ValoraSpacing.sm),
        ],
      ),
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
