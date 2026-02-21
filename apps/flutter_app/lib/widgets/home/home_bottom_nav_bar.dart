import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';
import '../../core/theme/valora_shadows.dart';
import '../valora_glass_container.dart';

class HomeBottomNavBar extends StatelessWidget {
  final int currentIndex;
  final ValueChanged<int> onTap;

  const HomeBottomNavBar({
    super.key,
    this.currentIndex = 0,
    required this.onTap,
  });

  static const List<_NavItem> _navItems = [
    _NavItem(icon: Icons.search_rounded, label: 'Search', index: 0),
    _NavItem(icon: Icons.map_rounded, label: 'Insights', index: 1),
    _NavItem(icon: Icons.settings_rounded, label: 'Settings', index: 2),
  ];
  static const double _barHeight = 76;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final mediaQuery = MediaQuery.of(context);
    final isCompactNav =
        mediaQuery.size.width < 390 || mediaQuery.textScaler.scale(1) > 1.1;

    // Using semantic glass tokens from design system
    final glassColor =
        isDark ? ValoraColors.glassBlackStrong : ValoraColors.glassWhiteStrong;

    final borderColor = isDark
        ? ValoraColors.glassBorderDark
        : ValoraColors.glassBorderLight.withValues(alpha: 0.5);

    return SafeArea(
      top: false,
      left: false,
      right: false,
      child: Container(
        height: _barHeight,
        margin: const EdgeInsets.fromLTRB(
          ValoraSpacing.md,
          0,
          ValoraSpacing.md,
          ValoraSpacing.sm,
        ),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(ValoraSpacing.xl),
          boxShadow: isDark ? ValoraShadows.xlDark : ValoraShadows.xl,
        ),
        child: ValoraGlassContainer(
          padding: const EdgeInsets.symmetric(
            horizontal: ValoraSpacing.sm,
            vertical: ValoraSpacing.sm,
          ),
          blur: ValoraSpacing.lg,
          borderRadius: BorderRadius.circular(ValoraSpacing.xl),
          color: glassColor,
          borderColor: borderColor,
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceEvenly,
            crossAxisAlignment: CrossAxisAlignment.center,
            children: _navItems.map((item) {
              final isSelected = currentIndex == item.index;
              return Expanded(
                child: _GlassNavItem(
                  icon: item.icon,
                  label: item.label,
                  isSelected: isSelected,
                  showSelectedLabel: !isCompactNav,
                  onTap: () => onTap(item.index),
                ),
              );
            }).toList(),
          ),
        ),
      ),
    );
  }
}

class _NavItem {
  final IconData icon;
  final String label;
  final int index;

  const _NavItem({
    required this.icon,
    required this.label,
    required this.index,
  });
}

class _GlassNavItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool isSelected;
  final bool showSelectedLabel;
  final VoidCallback onTap;

  const _GlassNavItem({
    required this.icon,
    required this.label,
    required this.isSelected,
    required this.showSelectedLabel,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final unselectedColor =
        isDark ? ValoraColors.neutral400 : ValoraColors.neutral500;

    return Semantics(
      button: true,
      selected: isSelected,
      label: '$label tab',
      child: Tooltip(
        message: label,
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: () {
              HapticFeedback.lightImpact();
              onTap();
            },
            borderRadius: BorderRadius.circular(ValoraSpacing.lg),
            child: AnimatedScale(
              scale: isSelected ? 1.02 : 1.0,
              duration: ValoraAnimations.fast,
              curve: Curves.easeOut,
              child: AnimatedContainer(
                duration: ValoraAnimations.normal,
                curve: Curves.easeOut,
                constraints: const BoxConstraints(
                  minHeight: ValoraSpacing.touchTargetMin,
                ),
                padding: const EdgeInsets.symmetric(
                  horizontal: ValoraSpacing.sm,
                  vertical: ValoraSpacing.xs,
                ),
                decoration: BoxDecoration(
                  color: isSelected
                      ? ValoraColors.primary
                          .withValues(alpha: isDark ? 0.18 : 0.1)
                      : Colors.transparent,
                  borderRadius: BorderRadius.circular(ValoraSpacing.lg),
                  border: Border.all(
                    color: isSelected
                        ? ValoraColors.primary.withValues(alpha: 0.24)
                        : Colors.transparent,
                    width: 1,
                  ),
                ),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(
                      icon,
                      size: ValoraSpacing.iconSizeMd,
                      semanticLabel: null,
                      color:
                          isSelected ? ValoraColors.primary : unselectedColor,
                    ),
                    if (showSelectedLabel) ...[
                      const SizedBox(height: 4),
                      Text(
                        label,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: ValoraTypography.labelSmall.copyWith(
                          color: isSelected
                              ? ValoraColors.primary
                              : unselectedColor,
                          fontWeight:
                              isSelected ? FontWeight.w700 : FontWeight.w500,
                        ),
                      ),
                    ],
                    const SizedBox(height: 2),
                    AnimatedContainer(
                      duration: ValoraAnimations.normal,
                      curve: Curves.easeOut,
                      width: isSelected ? 16 : 0,
                      height: 2,
                      decoration: BoxDecoration(
                        color: ValoraColors.primary.withValues(alpha: 0.85),
                        borderRadius:
                            BorderRadius.circular(ValoraSpacing.radiusSm),
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
