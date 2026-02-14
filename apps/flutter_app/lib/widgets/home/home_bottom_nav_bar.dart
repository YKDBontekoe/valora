import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_animate/flutter_animate.dart';
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
    _NavItem(icon: Icons.analytics_rounded, label: 'Report', index: 2),
    _NavItem(icon: Icons.favorite_rounded, label: 'Saved', index: 3),
    _NavItem(icon: Icons.settings_rounded, label: 'Settings', index: 4),
  ];

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    // Using semantic glass tokens from design system
    final glassColor = isDark
        ? ValoraColors.glassBlackStrong
        : ValoraColors.glassWhiteStrong;

    final borderColor = isDark
        ? ValoraColors.glassBorderDark
        : ValoraColors.glassBorderLight.withValues(alpha: 0.5);

    return SafeArea(
      top: false,
      left: false,
      right: false,
      child: Container(
        constraints: const BoxConstraints(minHeight: ValoraSpacing.navBarHeight),
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
              return Expanded(
                child: _GlassNavItem(
                  icon: item.icon,
                  label: item.label,
                  isSelected: currentIndex == item.index,
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
  final VoidCallback onTap;

  const _GlassNavItem({
    required this.icon,
    required this.label,
    required this.isSelected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final unselectedColor = isDark
        ? ValoraColors.neutral400
        : ValoraColors.neutral500;

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
            child: AnimatedContainer(
              duration: ValoraAnimations.medium,
              curve: ValoraAnimations.emphatic,
              alignment: Alignment.center,
              padding: EdgeInsets.symmetric(
                horizontal: isSelected ? ValoraSpacing.sm : ValoraSpacing.xs,
                vertical: ValoraSpacing.radiusMd,
              ),
              decoration: BoxDecoration(
                gradient: isSelected
                    ? LinearGradient(
                        colors: [
                          ValoraColors.primary.withValues(alpha: 0.1),
                          ValoraColors.primary.withValues(alpha: 0.2),
                        ],
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                      )
                    : null,
                color: isSelected ? null : Colors.transparent,
                borderRadius: BorderRadius.circular(ValoraSpacing.lg),
                border: isSelected
                    ? Border.all(
                        color: ValoraColors.primary.withValues(alpha: 0.2),
                        width: 1,
                      )
                    : null,
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(
                            icon,
                            size: ValoraSpacing.iconSizeMd,
                            semanticLabel: null,
                            color: isSelected
                                ? ValoraColors.primary
                                : unselectedColor,
                          )
                          .animate(target: isSelected ? 1 : 0)
                          .scale(
                            begin: const Offset(1, 1),
                            end: const Offset(1.15, 1.15),
                            duration: ValoraAnimations.fast,
                            curve: ValoraAnimations.emphatic,
                          )
                          .tint(
                            color: ValoraColors.primary,
                            end: 1,
                          ),
                      Flexible(
                        child: AnimatedSize(
                          duration: ValoraAnimations.medium,
                          curve: ValoraAnimations.emphatic,
                          child: SizedBox(
                            width: isSelected ? null : 0,
                            child: ExcludeSemantics(
                              child: Padding(
                                padding: isSelected
                                    ? const EdgeInsets.only(left: ValoraSpacing.xs)
                                    : EdgeInsets.zero,
                                child: Text(
                                  label,
                                  style: ValoraTypography.labelMedium.copyWith(
                                    color: ValoraColors.primary,
                                    fontWeight: FontWeight.bold,
                                    letterSpacing: 0.2,
                                  ),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  softWrap: false,
                                ),
                              ),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: ValoraSpacing.xs),
                  AnimatedOpacity(
                    opacity: isSelected ? 1 : 0,
                    duration: ValoraAnimations.normal,
                    curve: Curves.easeOut,
                    child: Container(
                      width: 4,
                      height: 4,
                      decoration: const BoxDecoration(
                        color: ValoraColors.primary,
                        shape: BoxShape.circle,
                        boxShadow: [
                          BoxShadow(
                            color: ValoraColors.primary,
                            blurRadius: 4,
                            spreadRadius: 0,
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
