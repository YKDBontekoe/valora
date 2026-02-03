import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../models/listing.dart';
import 'valora_widgets.dart';
import 'valora_glass_container.dart';

class HomeHeader extends StatefulWidget {
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
  State<HomeHeader> createState() => _HomeHeaderState();
}

class _HomeHeaderState extends State<HomeHeader> {
  final FocusNode _searchFocusNode = FocusNode();
  bool _isSearchFocused = false;

  @override
  void initState() {
    super.initState();
    _searchFocusNode.addListener(() {
      if (mounted) {
        setState(() {
          _isSearchFocused = _searchFocusNode.hasFocus;
        });
      }
    });
  }

  @override
  void dispose() {
    _searchFocusNode.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return ValoraGlassContainer(
      borderRadius: const BorderRadius.vertical(bottom: Radius.circular(ValoraSpacing.radiusXl)),
      padding: const EdgeInsets.fromLTRB(24, 12, 24, 16),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          // Search Bar
          Row(
            children: [
              Expanded(
                child: AnimatedContainer(
                  duration: const Duration(milliseconds: 200),
                  height: 52,
                  decoration: BoxDecoration(
                    color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
                    borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withValues(alpha: _isSearchFocused ? 0.12 : 0.08),
                        blurRadius: _isSearchFocused ? 24 : 20,
                        offset: Offset(0, _isSearchFocused ? 10 : 8),
                      ),
                    ],
                    border: Border.all(
                      color: _isSearchFocused
                          ? ValoraColors.primary
                          : (isDark ? ValoraColors.neutral700 : ValoraColors.neutral200),
                      width: _isSearchFocused ? 1.5 : 1,
                    ),
                  ),
                  child: TextField(
                    controller: widget.searchController,
                    focusNode: _searchFocusNode,
                    onChanged: widget.onSearchChanged,
                    style: ValoraTypography.bodyMedium.copyWith(
                      color: isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
                    ),
                    decoration: InputDecoration(
                      hintText: 'Search city, zip, or address...',
                      hintStyle: ValoraTypography.bodyMedium.copyWith(
                        color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
                      ),
                      prefixIcon: Icon(
                        Icons.search_rounded,
                        color: _isSearchFocused
                            ? ValoraColors.primary
                            : (isDark ? ValoraColors.neutral500 : ValoraColors.neutral400),
                      ),
                      border: InputBorder.none,
                      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Material(
                color: Colors.transparent,
                child: InkWell(
                  onTap: () {
                    HapticFeedback.lightImpact();
                    widget.onFilterPressed();
                  },
                  borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
                  child: Container(
                    width: 52,
                    height: 52,
                    decoration: BoxDecoration(
                      color: ValoraColors.primary.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
                      border: Border.all(
                        color: ValoraColors.primary.withValues(alpha: 0.2),
                      ),
                    ),
                    child: Stack(
                      alignment: Alignment.center,
                      children: [
                        const Icon(
                          Icons.tune_rounded,
                          color: ValoraColors.primary,
                        ),
                        if (widget.activeFilterCount > 0)
                          Positioned(
                            top: 12,
                            right: 12,
                            child: Container(
                              width: 8,
                              height: 8,
                              decoration: BoxDecoration(
                                color: ValoraColors.error,
                                shape: BoxShape.circle,
                                border: Border.all(
                                  color: isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight,
                                  width: 1.5,
                                ),
                              ),
                            ),
                          ),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: ValoraSpacing.md),
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
    return _FilterChip(
      icon: icon,
      label: label,
      isActive: isActive,
      backgroundColor: backgroundColor,
      textColor: textColor,
    );
  }
}

class _FilterChip extends StatefulWidget {
  final IconData? icon;
  final String label;
  final bool isActive;
  final Color? backgroundColor;
  final Color? textColor;

  const _FilterChip({
    this.icon,
    required this.label,
    this.isActive = false,
    this.backgroundColor,
    this.textColor,
  });

  @override
  State<_FilterChip> createState() => _FilterChipState();
}

class _FilterChipState extends State<_FilterChip> with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _scaleAnimation;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 100),
    );
    _scaleAnimation = Tween<double>(begin: 1.0, end: 0.95).animate(
      CurvedAnimation(parent: _controller, curve: Curves.easeInOut),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final bg = widget.backgroundColor ?? (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight);
    final text = widget.textColor ?? (isDark ? ValoraColors.neutral400 : ValoraColors.neutral500);
    final border = widget.isActive ? Colors.transparent : (isDark ? ValoraColors.neutral700 : ValoraColors.neutral200);

    return Listener(
      onPointerDown: (_) => _controller.forward(),
      onPointerUp: (_) => _controller.reverse(),
      onPointerCancel: (_) => _controller.reverse(),
      child: ScaleTransition(
        scale: _scaleAnimation,
        child: Container(
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(100),
            boxShadow: widget.isActive
                ? [
                    BoxShadow(
                      color: ValoraColors.primary.withValues(alpha: 0.25),
                      blurRadius: 12,
                      offset: const Offset(0, 6),
                    )
                  ]
                : [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.03),
                      blurRadius: 4,
                      offset: const Offset(0, 2),
                    )
                  ],
          ),
          child: Material(
            color: bg,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(100),
              side: BorderSide(color: border),
            ),
            child: InkWell(
              onTap: () => HapticFeedback.selectionClick(),
              borderRadius: BorderRadius.circular(100),
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    if (widget.icon != null) ...[
                      Icon(widget.icon, size: 16, color: text),
                      const SizedBox(width: 6),
                    ],
                    Text(
                      widget.label,
                      style: ValoraTypography.labelSmall.copyWith(
                        color: text,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class FeaturedListingCard extends StatefulWidget {
  final Listing listing;
  final VoidCallback onTap;
  final bool isFavorite;
  final VoidCallback? onFavoriteToggle;

  const FeaturedListingCard({
    super.key,
    required this.listing,
    required this.onTap,
    this.isFavorite = false,
    this.onFavoriteToggle,
  });

  @override
  State<FeaturedListingCard> createState() => _FeaturedListingCardState();
}

class _FeaturedListingCardState extends State<FeaturedListingCard> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return MouseRegion(
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      child: Container(
        width: 280,
        margin: const EdgeInsets.only(right: 20),
        child: ValoraCard(
          padding: EdgeInsets.zero,
          onTap: widget.onTap,
          borderRadius: ValoraSpacing.radiusXl,
          elevation: _isHovered ? ValoraSpacing.elevationLg : ValoraSpacing.elevationMd,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Image Section
            Stack(
              children: [
                Container(
                  height: 180,
                  color: ValoraColors.neutral200,
                  child: widget.listing.imageUrl != null
                      ? Hero(
                          tag: widget.listing.id,
                          child: CachedNetworkImage(
                            imageUrl: widget.listing.imageUrl!,
                            width: double.infinity,
                            height: 180,
                            fit: BoxFit.cover,
                            placeholder: (context, url) => Center(
                              child: Icon(Icons.home, size: 48, color: ValoraColors.neutral400),
                            ),
                            errorWidget: (context, url, error) => Center(
                              child: Icon(Icons.image_not_supported, color: ValoraColors.neutral400),
                            ),
                          ),
                        )
                      : Center(
                          child: Icon(Icons.home, size: 48, color: ValoraColors.neutral400),
                        ),
                ),
                // Gradient Overlay
                Positioned.fill(
                  child: DecoratedBox(
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        begin: Alignment.topCenter,
                        end: Alignment.bottomCenter,
                        colors: [
                          Colors.black.withValues(alpha: 0.4),
                          Colors.transparent,
                          Colors.transparent,
                        ],
                        stops: const [0.0, 0.5, 1.0],
                      ),
                    ),
                  ),
                ),
                Positioned(
                  top: 12,
                  left: 12,
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
                    child: BackdropFilter(
                      filter: ImageFilter.blur(sigmaX: 10, sigmaY: 10),
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                        decoration: BoxDecoration(
                          color: ValoraColors.glassBlack.withValues(alpha: 0.4),
                          borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
                          border: Border.all(
                            color: Colors.white.withValues(alpha: 0.1),
                            width: 1,
                          ),
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
                                boxShadow: [
                                  BoxShadow(
                                    color: ValoraColors.success,
                                    blurRadius: 4,
                                    spreadRadius: 1,
                                  ),
                                ],
                              ),
                            ),
                            const SizedBox(width: 6),
                            const Text(
                              '98% Match',
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 10,
                                fontWeight: FontWeight.bold,
                                letterSpacing: 0.5,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
                Positioned(
                  top: 12,
                  right: 12,
                  child: GestureDetector(
                    onTap: widget.onFavoriteToggle,
                    child: Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: (isDark ? Colors.black : Colors.white).withValues(alpha: 0.9),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        widget.isFavorite ? Icons.favorite_rounded : Icons.favorite_border_rounded,
                        size: 18,
                        color: widget.isFavorite ? ValoraColors.error : ValoraColors.neutral400,
                      ),
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
                            widget.listing.price != null ? '\$${widget.listing.price!.toStringAsFixed(0)}' : 'Price on request',
                            style: ValoraTypography.titleLarge.copyWith(
                              color: isDark ? ValoraColors.neutral50 : ValoraColors.neutral900,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            widget.listing.city ?? widget.listing.address,
                            style: ValoraTypography.bodySmall.copyWith(
                              color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
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
                            const SizedBox(width: 4),
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
                      _buildFeature(context, Icons.bed_rounded, '${widget.listing.bedrooms ?? 0} Bd'),
                      const SizedBox(width: 8),
                      _buildFeature(context, Icons.shower_rounded, '${widget.listing.bathrooms ?? 0} Ba'),
                      const SizedBox(width: 8),
                      _buildFeature(context, Icons.square_foot_rounded, '${widget.listing.livingAreaM2 ?? 0} m²'),
                    ],
                  ),
                ],
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
        Icon(icon, size: 16, color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500),
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

class NearbyListingCard extends StatefulWidget {
  final Listing listing;
  final VoidCallback onTap;
  final bool isFavorite;
  final VoidCallback? onFavoriteToggle;

  const NearbyListingCard({
    super.key,
    required this.listing,
    required this.onTap,
    this.isFavorite = false,
    this.onFavoriteToggle,
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
          elevation: _isHovered ? ValoraSpacing.elevationLg : null,
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
                      color: ValoraColors.neutral200,
                    ),
                    clipBehavior: Clip.antiAlias,
                    child: widget.listing.imageUrl != null
                        ? Hero(
                            tag: widget.listing.id,
                            child: CachedNetworkImage(
                              imageUrl: widget.listing.imageUrl!,
                              fit: BoxFit.cover,
                              placeholder: (context, url) => Center(
                                child: Icon(Icons.home, color: ValoraColors.neutral400),
                              ),
                              errorWidget: (context, url, error) => Center(
                                child: Icon(Icons.image_not_supported, color: ValoraColors.neutral400),
                              ),
                            ),
                          )
                        : Center(
                            child: Icon(Icons.home, color: ValoraColors.neutral400),
                          ),
                  ),
                  if (widget.onFavoriteToggle != null)
                    Positioned(
                      top: 4,
                      right: 4,
                      child: GestureDetector(
                        onTap: widget.onFavoriteToggle,
                        child: Container(
                          padding: const EdgeInsets.all(4),
                          decoration: BoxDecoration(
                            color: (isDark ? Colors.black : Colors.white).withValues(alpha: 0.9),
                            shape: BoxShape.circle,
                          ),
                          child: Icon(
                            widget.isFavorite ? Icons.favorite_rounded : Icons.favorite_border_rounded,
                            size: 14,
                            color: widget.isFavorite ? ValoraColors.error : ValoraColors.neutral400,
                          ),
                        ),
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
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: const Offset(0, -2),
          ),
        ],
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
    final isDark = Theme.of(context).brightness == Brightness.dark;

    final selectedColor = ValoraColors.primary;
    final unselectedColor = isDark ? ValoraColors.neutral400 : ValoraColors.neutral500;

    return GestureDetector(
      onTap: () {
        HapticFeedback.selectionClick();
        onTap(index);
      },
      behavior: HitTestBehavior.opaque,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          AnimatedContainer(
            duration: const Duration(milliseconds: 400),
            curve: Curves.easeOutBack,
            width: isSelected ? 56 : 32,
            height: 32,
            decoration: BoxDecoration(
              color: isSelected ? ValoraColors.primary.withValues(alpha: 0.15) : Colors.transparent,
              borderRadius: BorderRadius.circular(16),
            ),
            child: Icon(
              icon,
              color: isSelected ? selectedColor : unselectedColor,
              size: 24,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            label,
            style: ValoraTypography.labelSmall.copyWith(
              color: isSelected ? selectedColor : unselectedColor,
              fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
              fontSize: 10,
            ),
          ),
        ],
      ),
    );
  }
}
