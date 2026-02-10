import re

file_path = 'apps/flutter_app/lib/screens/listing_detail_screen.dart'

with open(file_path, 'r') as f:
    content = f.read()

def replace_function(content, func_signature, new_body):
    start_index = content.find(func_signature)
    if start_index == -1:
        print(f"Function {func_signature} not found")
        return content

    # Find the opening brace after signature
    brace_start = content.find('{', start_index)
    if brace_start == -1:
        return content

    # Find matching closing brace
    brace_count = 1
    current_index = brace_start + 1
    while brace_count > 0 and current_index < len(content):
        if content[current_index] == '{':
            brace_count += 1
        elif content[current_index] == '}':
            brace_count -= 1
        current_index += 1

    if brace_count > 0:
        print("Could not find closing brace")
        return content

    end_index = current_index

    # Replace
    return content[:start_index] + new_body + content[end_index:]

# New _buildFeatureChip
new_feature_chip = """Widget _buildFeatureChip(
    IconData icon,
    String label,
    ColorScheme colorScheme,
  ) {
    return Padding(
      padding: const EdgeInsets.only(right: ValoraSpacing.xs, bottom: ValoraSpacing.xs),
      child: ValoraChip(
        label: label,
        icon: icon,
        isSelected: false,
        backgroundColor: colorScheme.secondaryContainer.withValues(alpha: 0.5),
        textColor: colorScheme.onSecondaryContainer,
      ),
    );
  }"""

# New _buildSliverAppBar
new_sliver_app_bar = """Widget _buildSliverAppBar(BuildContext context, bool isDark) {
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
                  Widget imageWidget = CachedNetworkImage(
                    imageUrl: images[index],
                    fit: BoxFit.cover,
                    placeholder: (context, url) =>
                        _buildPlaceholder(isDark, isLoading: true),
                    errorWidget: (context, url, error) =>
                        _buildPlaceholder(isDark),
                  );

                  // Hero transition for the first image
                  if (index == 0) {
                    imageWidget = Hero(
                      tag: 'listing_img_${listing.id}',
                      child: imageWidget,
                    );
                  }

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
                    child: imageWidget,
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
                bottom: ValoraSpacing.lg + 20,
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
  }"""

content = replace_function(content, "Widget _buildFeatureChip(", new_feature_chip)
content = replace_function(content, "Widget _buildSliverAppBar(BuildContext context, bool isDark)", new_sliver_app_bar)

with open(file_path, 'w') as f:
    f.write(content)

print("Successfully updated ListingDetailScreen")
