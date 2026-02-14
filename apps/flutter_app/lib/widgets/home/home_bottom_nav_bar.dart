import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';
import '../../core/theme/valora_shadows.dart';

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
    final glassColor = isDark
        ? ValoraColors.glassBlack.withValues(alpha: 0.9)
        : ValoraColors.glassWhite.withValues(alpha: 0.9);
    final borderColor = isDark
        ? ValoraColors.glassBorderDark
        : ValoraColors.glassBorderLight.withValues(alpha: 0.5);

    // SafeArea is used specifically to respect the home indicator area
    // while allowing the background to extend to the screen edges if needed
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
          boxShadow: ValoraShadows.xl,
        ),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(ValoraSpacing.xl),
          child: BackdropFilter(
            filter: ImageFilter.blur(
              sigmaX: ValoraSpacing.lg,
              sigmaY: ValoraSpacing.lg,
            ),
            child: Container(
              padding: const EdgeInsets.symmetric(
                horizontal: ValoraSpacing.sm,
                vertical: ValoraSpacing.sm,
              ),
              decoration: BoxDecoration(
                color: glassColor,
                borderRadius: BorderRadius.circular(ValoraSpacing.xl),
                border: Border.all(color: borderColor, width: 1),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceEvenly, // Ensure even distribution
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  Expanded(
                    child: _GlassNavItem(
                      icon: Icons.search_rounded,
                      label: 'Search',
                      isSelected: currentIndex == 0,
                      onTap: () => onTap(0),
                    ),
                  ),
                  Expanded(
                    child: _GlassNavItem(
                      icon: Icons.map_rounded,
                      label: 'Insights',
                      isSelected: currentIndex == 1,
                      onTap: () => onTap(1),
                    ),
                  ),
                  Expanded(
                    child: _GlassNavItem(
                      icon: Icons.analytics_rounded,
                      label: 'Report',
                      isSelected: currentIndex == 2,
                      onTap: () => onTap(2),
                    ),
                  ),
                  Expanded(
                    child: _GlassNavItem(
                      icon: Icons.favorite_rounded,
                      label: 'Saved',
                      isSelected: currentIndex == 3,
                      onTap: () => onTap(3),
                    ),
                  ),
                  Expanded(
                    child: _GlassNavItem(
                      icon: Icons.settings_rounded,
                      label: 'Settings',
                      isSelected: currentIndex == 4,
                      onTap: () => onTap(4),
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
              HapticFeedback.selectionClick();
              onTap();
            },
            borderRadius: BorderRadius.circular(ValoraSpacing.lg),
            child: AnimatedContainer(
              duration: ValoraAnimations.medium,
              curve: ValoraAnimations.emphatic,
              alignment: Alignment.center,
              padding: EdgeInsets.symmetric(
                horizontal: isSelected ? ValoraSpacing.sm : ValoraSpacing.xs,
                vertical: ValoraSpacing.radiusLg,
              ),
              decoration: BoxDecoration(
                color: isSelected
                    ? ValoraColors.primary.withValues(alpha: 0.15)
                    : Colors.transparent,
                borderRadius: BorderRadius.circular(ValoraSpacing.lg),
              ),
              child: Row(
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
                        end: const Offset(1.1, 1.1),
                        duration: ValoraAnimations.fast,
                        curve: ValoraAnimations.emphatic,
                      )
                      .then()
                      .scale(end: const Offset(1.0, 1.0)),

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
            ),
          ),
        ),
      ),
    );
  }
}
