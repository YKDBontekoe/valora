import 'package:flutter/material.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../core/theme/valora_colors.dart';
import '../models/listing.dart';

class HomeHeader extends StatelessWidget {
  final TextEditingController searchController;
  final ValueChanged<String> onSearchChanged;
  final VoidCallback onFilterPressed;
  final int activeFilterCount;

  const HomeHeader({
    super.key,
    required this.searchController,
    required this.onSearchChanged,
    required this.onFilterPressed,
    this.activeFilterCount = 0,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      padding: const EdgeInsets.fromLTRB(24, 12, 24, 16),
      color: isDark ? ValoraColors.backgroundDark.withValues(alpha: 0.95) : ValoraColors.backgroundLight.withValues(alpha: 0.95),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          // Title Row is handled by SliverAppBar title usually, but if we put everything in bottom, we need it here.
          // However, standard pattern is Title in flexibleSpace or title, and Search in bottom.
          // Let's assume this widget is the `bottom` of SliverAppBar.

          // Search Bar
          Row(
            children: [
              Expanded(
                child: Container(
                  height: 52,
                  decoration: BoxDecoration(
                    color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
                    borderRadius: BorderRadius.circular(12),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withValues(alpha: 0.05),
                        blurRadius: 12,
                        offset: const Offset(0, 2),
                      ),
                    ],
                  ),
                  child: TextField(
                    controller: searchController,
                    onChanged: onSearchChanged,
                    decoration: InputDecoration(
                      hintText: 'Search city, zip, or address...',
                      hintStyle: TextStyle(
                        color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
                        fontSize: 14,
                        fontWeight: FontWeight.w500,
                      ),
                      prefixIcon: Icon(
                        Icons.search_rounded,
                        color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
                      ),
                      border: InputBorder.none,
                      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              GestureDetector(
                onTap: onFilterPressed,
                child: Container(
                  width: 52,
                  height: 52,
                  decoration: BoxDecoration(
                    color: ValoraColors.primary.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Stack(
                    alignment: Alignment.center,
                    children: [
                      const Icon(
                        Icons.tune_rounded,
                        color: ValoraColors.primary,
                      ),
                      if (activeFilterCount > 0)
                        Positioned(
                          top: 12,
                          right: 12,
                          child: Container(
                            width: 8,
                            height: 8,
                            decoration: const BoxDecoration(
                              color: ValoraColors.error,
                              shape: BoxShape.circle,
                            ),
                          ),
                        ),
                    ],
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          // Filter Chips
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            clipBehavior: Clip.none,
            child: Row(
              children: [
                _buildFilterChip(
                  context,
                  icon: Icons.auto_awesome,
                  label: 'AI Pick',
                  isActive: true,
                  backgroundColor: ValoraColors.primary,
                  textColor: Colors.white,
                ),
                const SizedBox(width: 8),
                _buildFilterChip(context, label: 'Under \$500k'),
                const SizedBox(width: 8),
                _buildFilterChip(context, label: '3+ Beds'),
                const SizedBox(width: 8),
                _buildFilterChip(context, label: 'Near Schools'),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFilterChip(
    BuildContext context, {
    IconData? icon,
    required String label,
    bool isActive = false,
    Color? backgroundColor,
    Color? textColor,
  }) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final bg = backgroundColor ?? (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight);
    final text = textColor ?? (isDark ? ValoraColors.neutral400 : ValoraColors.neutral500);
    final border = isActive ? Colors.transparent : (isDark ? ValoraColors.neutral700 : ValoraColors.neutral200);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(100),
        border: Border.all(color: border),
        boxShadow: isActive
            ? [
                BoxShadow(
                  color: ValoraColors.primary.withValues(alpha: 0.2),
                  blurRadius: 8,
                  offset: const Offset(0, 4),
                )
              ]
            : [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.02),
                  blurRadius: 2,
                  offset: const Offset(0, 1),
                )
              ],
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 16, color: text),
            const SizedBox(width: 6),
          ],
          Text(
            label,
            style: TextStyle(
              color: text,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}

class FeaturedListingCard extends StatelessWidget {
  final Listing listing;
  final VoidCallback onTap;

  const FeaturedListingCard({
    super.key,
    required this.listing,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 280,
        margin: const EdgeInsets.only(right: 20),
        decoration: BoxDecoration(
          color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
          borderRadius: BorderRadius.circular(20),
          boxShadow: [
            BoxShadow(
              color: ValoraColors.primary.withValues(alpha: 0.08),
              blurRadius: 20,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        clipBehavior: Clip.antiAlias,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Image Section
            Stack(
              children: [
                Container(
                  height: 180,
                  color: ValoraColors.neutral200,
                  child: listing.imageUrl != null
                      ? CachedNetworkImage(
                          imageUrl: listing.imageUrl!,
                          width: double.infinity,
                          height: 180,
                          fit: BoxFit.cover,
                          placeholder: (context, url) => Center(
                            child: Icon(Icons.home, size: 48, color: ValoraColors.neutral400),
                          ),
                          errorWidget: (context, url, error) => Center(
                            child: Icon(Icons.image_not_supported, color: ValoraColors.neutral400),
                          ),
                        )
                      : Center(
                          child: Icon(Icons.home, size: 48, color: ValoraColors.neutral400),
                        ),
                ),
                Positioned(
                  top: 12,
                  left: 12,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: Colors.black.withValues(alpha: 0.4),
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: Colors.white.withValues(alpha: 0.1)),
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
                          ),
                        ),
                        const SizedBox(width: 4),
                        const Text(
                          '98% Match',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 10,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                Positioned(
                  top: 12,
                  right: 12,
                  child: Container(
                    padding: const EdgeInsets.all(8),
                    decoration: BoxDecoration(
                      color: (isDark ? Colors.black : Colors.white).withValues(alpha: 0.9),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Icons.favorite_border_rounded,
                      size: 18,
                      color: ValoraColors.neutral400,
                    ),
                  ),
                ),
              ],
            ),
            // Info Section
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            listing.price != null ? '\$${listing.price!.toStringAsFixed(0)}' : 'Price on request',
                            style: TextStyle(
                              color: isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            listing.city ?? listing.address,
                            style: TextStyle(
                              color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                              fontSize: 12,
                            ),
                          ),
                        ],
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(
                          color: ValoraColors.primary.withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: Row(
                          children: const [
                            Icon(Icons.trending_down_rounded, size: 14, color: ValoraColors.primary),
                            SizedBox(width: 4),
                            Text(
                              '-2%',
                              style: TextStyle(
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
                  const SizedBox(height: 12),
                  Divider(height: 1, color: isDark ? ValoraColors.neutral800 : ValoraColors.neutral100),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      _buildFeature(context, Icons.bed_rounded, '${listing.bedrooms ?? 0} Bd'),
                      const SizedBox(width: 8),
                      _buildFeature(context, Icons.shower_rounded, '${listing.bathrooms ?? 0} Ba'),
                      const SizedBox(width: 8),
                      _buildFeature(context, Icons.square_foot_rounded, '${listing.livingAreaM2 ?? 0} m²'),
                    ],
                  ),
                ],
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
        Icon(icon, size: 16, color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500),
        const SizedBox(width: 4),
        Text(
          label,
          style: TextStyle(
            color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
            fontSize: 11,
          ),
        ),
      ],
    );
  }
}

class NearbyListingCard extends StatelessWidget {
  final Listing listing;
  final VoidCallback onTap;

  const NearbyListingCard({
    super.key,
    required this.listing,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return GestureDetector(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.only(bottom: 16),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
          borderRadius: BorderRadius.circular(12),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.05),
              blurRadius: 12,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Row(
          children: [
            // Image
            Container(
              width: 96,
              height: 96,
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(8),
                color: ValoraColors.neutral200,
              ),
              clipBehavior: Clip.antiAlias,
              child: listing.imageUrl != null
                  ? CachedNetworkImage(
                      imageUrl: listing.imageUrl!,
                      fit: BoxFit.cover,
                      placeholder: (context, url) => Center(
                        child: Icon(Icons.home, color: ValoraColors.neutral400),
                      ),
                      errorWidget: (context, url, error) => Center(
                        child: Icon(Icons.image_not_supported, color: ValoraColors.neutral400),
                      ),
                    )
                  : Center(
                      child: Icon(Icons.home, color: ValoraColors.neutral400),
                    ),
            ),
            const SizedBox(width: 16),
            // Info
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Text(
                          listing.price != null ? '\$${listing.price!.toStringAsFixed(0)}' : 'Price on request',
                          style: TextStyle(
                            color: isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
                            fontSize: 16,
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
                    listing.address,
                    style: TextStyle(
                      color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                      fontSize: 12,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      _buildFeature(context, Icons.bed_rounded, '${listing.bedrooms ?? 0}'),
                      const SizedBox(width: 8),
                      _buildFeature(context, Icons.shower_rounded, '${listing.bathrooms ?? 0}'),
                      const SizedBox(width: 8),
                      _buildFeature(context, Icons.square_foot_rounded, '${listing.livingAreaM2 ?? 0}'),
                    ],
                  ),
                ],
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
        Icon(icon, size: 14, color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500),
        const SizedBox(width: 4),
        Text(
          label,
          style: TextStyle(
            color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
            fontSize: 11,
          ),
        ),
      ],
    );
  }
}

class HomeBottomNavBar extends StatelessWidget {
  final int currentIndex;
  final ValueChanged<int> onTap;

  const HomeBottomNavBar({
    super.key,
    this.currentIndex = 0,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      decoration: BoxDecoration(
        color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
        border: Border(
          top: BorderSide(
            color: isDark ? ValoraColors.neutral800 : ValoraColors.neutral100,
            width: 1,
          ),
        ),
      ),
      padding: EdgeInsets.only(
        bottom: MediaQuery.of(context).padding.bottom + 8,
        top: 8,
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceAround,
        children: [
          _buildNavItem(context, 0, Icons.home_rounded, 'Home'),
          _buildNavItem(context, 1, Icons.search_rounded, 'Search'),
          const SizedBox(width: 48), // Space for FAB
          _buildNavItem(context, 2, Icons.favorite_rounded, 'Saved'),
          _buildNavItem(context, 3, Icons.settings_rounded, 'Settings'),
        ],
      ),
    );
  }

  Widget _buildNavItem(BuildContext context, int index, IconData icon, String label) {
    final isSelected = currentIndex == index;
    final color = isSelected
        ? ValoraColors.primary
        : (Theme.of(context).brightness == Brightness.dark ? ValoraColors.neutral400 : ValoraColors.neutral500);

    return GestureDetector(
      onTap: () => onTap(index),
      behavior: HitTestBehavior.opaque,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, color: color, size: 24),
          const SizedBox(height: 4),
          Text(
            label,
            style: TextStyle(
              color: color,
              fontSize: 10,
              fontWeight: FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }
}
