import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';
import '../../core/theme/valora_shadows.dart';
import '../valora_glass_container.dart';
import '../valora_widgets.dart';

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
      borderRadius: const BorderRadius.vertical(
        bottom: Radius.circular(ValoraSpacing.radiusXl),
      ),
      padding: const EdgeInsets.fromLTRB(24, 12, 24, 16),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          // Search Bar
          Row(
            children: [
              Expanded(
                child: AnimatedContainer(
                  duration: ValoraAnimations.normal,
                  curve: ValoraAnimations.standard,
                  height: 52,
                  decoration: BoxDecoration(
                    color: isDark
                        ? ValoraColors.surfaceDark
                        : ValoraColors.surfaceLight,
                    borderRadius: BorderRadius.circular(
                      ValoraSpacing.radiusFull,
                    ),
                    boxShadow: _isSearchFocused
                        ? ValoraShadows.lg
                        : ValoraShadows.sm,
                    border: Border.all(
                      color: _isSearchFocused
                          ? ValoraColors.primary
                          : (isDark
                                ? ValoraColors.neutral700
                                : ValoraColors.neutral200),
                      width: _isSearchFocused ? 1.5 : 1,
                    ),
                  ),
                  child: TextField(
                    controller: widget.searchController,
                    focusNode: _searchFocusNode,
                    onChanged: widget.onSearchChanged,
                    style: ValoraTypography.bodyMedium.copyWith(
                      color: isDark
                          ? ValoraColors.neutral50
                          : ValoraColors.neutral900,
                    ),
                    decoration: InputDecoration(
                      hintText: 'Search city, zip, or address...',
                      hintStyle: ValoraTypography.bodyMedium.copyWith(
                        color: isDark
                            ? ValoraColors.neutral500
                            : ValoraColors.neutral400,
                      ),
                      prefixIcon: Icon(
                        Icons.search_rounded,
                        color: _isSearchFocused
                            ? ValoraColors.primary
                            : (isDark
                                  ? ValoraColors.neutral500
                                  : ValoraColors.neutral400),
                      ),
                      border: InputBorder.none,
                      contentPadding: const EdgeInsets.symmetric(
                        horizontal: 16,
                        vertical: 14,
                      ),
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
                      borderRadius: BorderRadius.circular(
                        ValoraSpacing.radiusLg,
                      ),
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
                                  color: isDark
                                      ? ValoraColors.surfaceDark
                                      : ValoraColors.surfaceLight,
                                  width: 1.5,
                                ),
                              ),
                            ),
                          ).animate().scale(curve: Curves.elasticOut),
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
              children:
                  [
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
                      ]
                      .animate(interval: 50.ms)
                      .fade()
                      .slideX(
                        begin: 0.2,
                        end: 0,
                        curve: ValoraAnimations.deceleration,
                      ),
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
    return ValoraChip(
      icon: icon,
      label: label,
      isSelected: isActive,
      onSelected: null, // Static for now, disable tap feedback
      backgroundColor: backgroundColor,
      textColor: textColor,
    );
  }
}
