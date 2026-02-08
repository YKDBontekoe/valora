import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
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

    return Align(
      alignment: Alignment.bottomCenter,
      child: SafeArea(
        child: Container(
          margin: const EdgeInsets.fromLTRB(24, 0, 24, 12),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(32),
            boxShadow: ValoraShadows.xl,
          ),
          child: ClipRRect(
            borderRadius: BorderRadius.circular(32),
            child: BackdropFilter(
              filter: ImageFilter.blur(sigmaX: 20, sigmaY: 20),
              child: Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
                decoration: BoxDecoration(
                  color: glassColor,
                  borderRadius: BorderRadius.circular(32),
                  border: Border.all(color: borderColor, width: 1),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    _GlassNavItem(
                      icon: Icons.home_rounded,
                      label: 'Home',
                      isSelected: currentIndex == 0,
                      onTap: () => onTap(0),
                    ),
                    _GlassNavItem(
                      icon: Icons.search_rounded,
                      label: 'Search',
                      isSelected: currentIndex == 1,
                      onTap: () => onTap(1),
                    ),
                    _GlassNavItem(
                      icon: Icons.favorite_rounded,
                      label: 'Saved',
                      isSelected: currentIndex == 2,
                      onTap: () => onTap(2),
                    ),
                    _GlassNavItem(
                      icon: Icons.settings_rounded,
                      label: 'Settings',
                      isSelected: currentIndex == 3,
                      onTap: () => onTap(3),
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
            borderRadius: BorderRadius.circular(24),
            child: AnimatedContainer(
              duration: ValoraAnimations.medium,
              curve: ValoraAnimations.emphatic,
              padding: EdgeInsets.symmetric(
                horizontal: isSelected ? 20 : 16,
                vertical: 12,
              ),
              decoration: BoxDecoration(
                color: isSelected
                    ? ValoraColors.primary.withValues(alpha: 0.15)
                    : Colors.transparent,
                borderRadius: BorderRadius.circular(24),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                        icon,
                        size: 24,
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

                  AnimatedSize(
                    duration: ValoraAnimations.medium,
                    curve: ValoraAnimations.emphatic,
                    child: SizedBox(
                      width: isSelected ? null : 0,
                      child: ExcludeSemantics(
                        child: Padding(
                          padding: isSelected
                              ? const EdgeInsets.only(left: 8)
                              : EdgeInsets.zero,
                          child: Text(
                            label,
                            style: const TextStyle(
                              color: ValoraColors.primary,
                              fontWeight: FontWeight.bold,
                              fontSize: 14,
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
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
