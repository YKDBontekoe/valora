import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:share_plus/share_plus.dart';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/listing.dart';
import '../../providers/favorites_provider.dart';
import '../valora_widgets.dart';
import '../valora_glass_container.dart';

class ListingSliverAppBar extends StatefulWidget {
  const ListingSliverAppBar({
    super.key,
    required this.listing,
    this.onImageTap,
  });

  final Listing listing;
  final ValueChanged<int>? onImageTap;

  @override
  State<ListingSliverAppBar> createState() => _ListingSliverAppBarState();
}

class _ListingSliverAppBarState extends State<ListingSliverAppBar> {
  int _currentImageIndex = 0;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final screenHeight = MediaQuery.of(context).size.height;
    final expandedHeight = screenHeight * 0.45;

    // Use imageUrls if available, otherwise fallback to single imageUrl, otherwise empty list
    final images = widget.listing.imageUrls.isNotEmpty
        ? widget.listing.imageUrls
        : (widget.listing.imageUrl != null ? [widget.listing.imageUrl!] : <String>[]);

    return SliverAppBar(
      expandedHeight: expandedHeight,
      pinned: true,
      stretch: true,
      backgroundColor: Colors.transparent,
      iconTheme: const IconThemeData(color: Colors.white),
      leading: Padding(
        padding: const EdgeInsets.all(8.0),
        child: ValoraGlassContainer(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
          padding: EdgeInsets.zero,
          child: IconButton(
            icon: const Icon(Icons.arrow_back_rounded, color: Colors.white, size: 20),
            onPressed: () => Navigator.of(context).pop(),
          ),
        ),
      ),
      actions: [
        if (widget.listing.url != null)
          Padding(
            padding: const EdgeInsets.only(right: ValoraSpacing.sm),
            child: ValoraGlassContainer(
              borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
              padding: EdgeInsets.zero,
              width: 40,
              height: 40,
              child: IconButton(
                // ignore: deprecated_member_use
                onPressed: () => Share.share(widget.listing.url!),
                icon: const Icon(Icons.share_rounded, color: Colors.white, size: 20),
              ),
            ),
          ),
        Consumer<FavoritesProvider>(
          builder: (context, favorites, _) {
            final isFav = favorites.isFavorite(widget.listing.id);
            return Padding(
              padding: const EdgeInsets.only(right: ValoraSpacing.md),
              child: ValoraGlassContainer(
                borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
                padding: EdgeInsets.zero,
                width: 40,
                height: 40,
                child: IconButton(
                  onPressed: () => favorites.toggleFavorite(widget.listing),
                  icon: Icon(
                    isFav ? Icons.favorite_rounded : Icons.favorite_border_rounded,
                    color: isFav ? ValoraColors.error : Colors.white,
                    size: 20,
                  )
                  .animate(target: isFav ? 1 : 0)
                  .scale(
                    duration: 400.ms,
                    curve: Curves.elasticOut,
                    begin: const Offset(1, 1),
                    end: const Offset(1.2, 1.2),
                  )
                  .then()
                  .scale(end: const Offset(1, 1)),
                ),
              ),
            );
          },
        ),
      ],
      flexibleSpace: FlexibleSpaceBar(
        background: Stack(
          fit: StackFit.expand,
          children: [
            if (images.isNotEmpty)
              Hero(
                tag: widget.listing.id,
                child: PageView.builder(
                  itemCount: images.length,
                  onPageChanged: (index) => setState(() => _currentImageIndex = index),
                  itemBuilder: (context, index) {
                    return GestureDetector(
                      onTap: () => widget.onImageTap?.call(index),
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
                ),
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
                    Colors.black.withValues(alpha: 0.4),
                    Colors.transparent,
                    Colors.transparent,
                    Colors.black.withValues(alpha: 0.6),
                  ],
                  stops: const [0.0, 0.2, 0.8, 1.0],
                ),
              ),
            ),

            // Photo Counter
            if (images.length > 1)
              Positioned(
                bottom: ValoraSpacing.xl,
                right: ValoraSpacing.md,
                child: ValoraGlassContainer(
                  padding: const EdgeInsets.symmetric(
                    horizontal: ValoraSpacing.md,
                    vertical: ValoraSpacing.xs,
                  ),
                  borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(
                        Icons.photo_camera_rounded,
                        size: 14,
                        color: Colors.white,
                      ),
                      const SizedBox(width: ValoraSpacing.xs),
                      Text(
                        '${_currentImageIndex + 1} / ${images.length}',
                        style: ValoraTypography.labelMedium.copyWith(
                          color: Colors.white,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
              ).animate().fadeIn(delay: 300.ms),
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
