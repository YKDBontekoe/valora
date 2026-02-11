import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:share_plus/share_plus.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/listing.dart';
import '../../providers/favorites_provider.dart';
import '../valora_widgets.dart';
import '../../screens/gallery/full_screen_gallery.dart';

class ListingSliverAppBar extends StatelessWidget {
  const ListingSliverAppBar({
    super.key,
    required this.listing,
  });

  final Listing listing;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    // Use imageUrls if available, otherwise fallback to single imageUrl, otherwise empty list
    final images = listing.imageUrls.isNotEmpty
        ? listing.imageUrls
        : (listing.imageUrl != null ? [listing.imageUrl!] : <String>[]);

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
        background: Stack(
          fit: StackFit.expand,
          children: [
            if (images.isNotEmpty)
              PageView.builder(
                itemCount: images.length,
                itemBuilder: (context, index) {
                  return GestureDetector(
                    onTap: () {
                      Navigator.of(context).push(
                        MaterialPageRoute(
                          builder: (_) => FullScreenGallery(
                            imageUrls: images,
                            initialIndex: index,
                          ),
                        ),
                      );
                    },
                    child: CachedNetworkImage(
                      imageUrl: images[index],
                      fit: BoxFit.cover,
                      placeholder: (context, url) =>
                          _buildPlaceholder(isDark, isLoading: true),
                      errorWidget: (context, url, error) =>
                          _buildPlaceholder(isDark),
                    ),
                  );
                },
              )
            else
              _buildPlaceholder(isDark),

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

            // Photo Counter
            if (images.length > 1)
              Positioned(
                bottom:
                    ValoraSpacing.lg + 20, // Adjust for rounded corners of body
                right: ValoraSpacing.md,
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 6,
                  ),
                  decoration: BoxDecoration(
                    color: Colors.black.withValues(alpha: 0.6),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(
                        Icons.photo_library_rounded,
                        size: 14,
                        color: Colors.white,
                      ),
                      const SizedBox(width: 6),
                      Text(
                        '${images.length} Photos',
                        style: ValoraTypography.labelMedium.copyWith(
                          color: Colors.white,
                          fontWeight: FontWeight.w600,
                        ),
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

  Widget _buildPlaceholder(bool isDark, {bool isLoading = false}) {
    if (isLoading) {
      return const ValoraShimmer(
        width: double.infinity,
        height: double.infinity,
        borderRadius: 0,
      );
    }
    // If we have lat/long, we could show a static map here, but for now a nice icon
    // showing it's a "Building" record vs a "Sale" record
    return Container(
      color: isDark ? ValoraColors.surfaceVariantDark : ValoraColors.neutral100,
      child: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.location_city_rounded,
              size: ValoraSpacing.iconSizeXl * 1.5,
              color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
            ),
             const SizedBox(height: ValoraSpacing.md),
             Text(
              'Property Details',
              style: ValoraTypography.titleMedium.copyWith(
                color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
              ),
             ),
          ],
        ),
      ),
    );
  }
}
