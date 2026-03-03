import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';
import '../../core/theme/valora_shadows.dart';
import '../../services/notification_service.dart';
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
    _NavItem(icon: Icons.search_rounded, activeIcon: Icons.search_rounded, label: 'Search', index: 0),
    _NavItem(icon: Icons.map_outlined, activeIcon: Icons.map_rounded, label: 'Insights', index: 1),
    _NavItem(icon: Icons.notifications_none_rounded, activeIcon: Icons.notifications_rounded, label: 'Alerts', index: 2),
    _NavItem(icon: Icons.settings_outlined, activeIcon: Icons.settings_rounded, label: 'Settings', index: 3),
  ];

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final mediaQuery = MediaQuery.of(context);
    final isCompactNav =
        mediaQuery.size.width < 390 || mediaQuery.textScaler.scale(1) > 1.1;

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
        constraints: const BoxConstraints(
          minHeight: ValoraSpacing.navBarHeight,
        ),
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
                flex: isSelected && !isCompactNav ? 2 : 1,
                child: _GlassNavItem(
                  icon: isSelected ? item.activeIcon : item.icon,
                  label: item.label,
                  isSelected: isSelected,
                  showSelectedLabel: !isCompactNav,
                  showBadge: item.index == 2, // Notifications tab
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
  final IconData activeIcon;
  final String label;
  final int index;

  const _NavItem({
    required this.icon,
    required this.activeIcon,
    required this.label,
    required this.index,
  });
}

class _GlassNavItem extends StatefulWidget {
  final IconData icon;
  final String label;
  final bool isSelected;
  final bool showSelectedLabel;
  final bool showBadge;
  final VoidCallback onTap;

  const _GlassNavItem({
    required this.icon,
    required this.label,
    required this.isSelected,
    required this.showSelectedLabel,
    this.showBadge = false,
    required this.onTap,
  });

  @override
  State<_GlassNavItem> createState() => _GlassNavItemState();
}

class _GlassNavItemState extends State<_GlassNavItem> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final unselectedColor = isDark
        ? ValoraColors.neutral400
        : ValoraColors.neutral500;

    return Semantics(
      button: true,
      selected: widget.isSelected,
      label: '${widget.label} tab',
      child: Tooltip(
        message: widget.label,
        child: MouseRegion(
          onEnter: (_) => setState(() => _isHovered = true),
          onExit: (_) => setState(() => _isHovered = false),
          child: Material(
            color: Colors.transparent,
            child: InkWell(
              onTap: () {
                HapticFeedback.lightImpact();
                widget.onTap();
              },
              borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
              highlightColor: ValoraColors.primary.withValues(alpha: 0.1),
              splashColor: ValoraColors.primary.withValues(alpha: 0.15),
              child: AnimatedContainer(
                duration: ValoraAnimations.medium,
                curve: ValoraAnimations.emphatic,
                padding: EdgeInsets.symmetric(
                  horizontal: widget.isSelected ? ValoraSpacing.md : ValoraSpacing.sm,
                  vertical: ValoraSpacing.md,
                ),
                decoration: BoxDecoration(
                  gradient: widget.isSelected
                      ? LinearGradient(
                          colors: [
                            ValoraColors.primary.withValues(alpha: 0.15),
                            ValoraColors.primary.withValues(alpha: 0.25),
                          ],
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                        )
                      : _isHovered
                          ? LinearGradient(
                              colors: [
                                (isDark ? ValoraColors.neutral800 : ValoraColors.neutral200).withValues(alpha: 0.5),
                                (isDark ? ValoraColors.neutral800 : ValoraColors.neutral200).withValues(alpha: 0.8),
                              ],
                              begin: Alignment.topCenter,
                              end: Alignment.bottomCenter,
                            )
                          : null,
                  color: widget.isSelected || _isHovered ? null : Colors.transparent,
                  borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
                  border: widget.isSelected
                      ? Border.all(
                          color: ValoraColors.primary.withValues(alpha: 0.3),
                          width: 1.5,
                        )
                      : _isHovered
                          ? Border.all(
                              color: (isDark ? ValoraColors.neutral700 : ValoraColors.neutral300).withValues(alpha: 0.5),
                              width: 1,
                            )
                          : Border.all(color: Colors.transparent, width: 1),
                  boxShadow: widget.isSelected
                      ? [
                          BoxShadow(
                            color: ValoraColors.primary.withValues(alpha: 0.2),
                            blurRadius: ValoraSpacing.sm,
                            offset: const Offset(0, 2),
                          )
                        ]
                      : _isHovered
                          ? [
                              BoxShadow(
                                color: (isDark ? Colors.black : Colors.black).withValues(alpha: 0.05),
                                blurRadius: ValoraSpacing.xs,
                                offset: const Offset(0, 1),
                              )
                            ]
                          : null,
                ),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Stack(
                          clipBehavior: Clip.none,
                          children: [
                            Icon(
                                  widget.icon,
                                  size: ValoraSpacing.iconSizeMd,
                                  semanticLabel: null,
                                  color: widget.isSelected
                                      ? ValoraColors.primary
                                      : unselectedColor,
                                )
                                .animate(target: widget.isSelected ? 1 : 0)
                                .scale(
                                  begin: const Offset(1, 1),
                                  end: const Offset(1.15, 1.15),
                                  duration: ValoraAnimations.fast,
                                  curve: ValoraAnimations.emphatic,
                                )
                                .tint(color: ValoraColors.primary, end: 1),
                            if (widget.showBadge)
                              Selector<NotificationService, int>(
                                selector: (_, s) => s.unreadCount,
                                builder: (context, unreadCount, _) {
                                  if (unreadCount == 0) return const SizedBox.shrink();
                                  return Positioned(
                                    top: -ValoraSpacing.xs,
                                    right: -6,
                                    child: Container(
                                      padding: const EdgeInsets.all(3),
                                      decoration: const BoxDecoration(
                                        color: ValoraColors.error,
                                        shape: BoxShape.circle,
                                      ),
                                      constraints: const BoxConstraints(
                                        minWidth: ValoraSpacing.md,
                                        minHeight: ValoraSpacing.md,
                                      ),
                                      child: Center(
                                        child: Text(
                                          unreadCount > 9 ? '9+' : '$unreadCount',
                                          style: const TextStyle(
                                            color: Colors.white,
                                            fontSize: 9,
                                            fontWeight: FontWeight.bold,
                                          ),
                                        ),
                                      ),
                                    ),
                                  );
                                },
                              ),
                          ],
                        ),
                        if (widget.showSelectedLabel) ...[
                          Flexible(
                            child: AnimatedSize(
                              duration: ValoraAnimations.medium,
                              curve: ValoraAnimations.emphatic,
                              child: widget.isSelected
                                  ? ExcludeSemantics(
                                      child: Padding(
                                        padding: const EdgeInsets.only(
                                          left: ValoraSpacing.xs,
                                        ),
                                        child: Text(
                                          widget.label,
                                          style: ValoraTypography.labelMedium
                                              .copyWith(
                                                color: ValoraColors.primary,
                                                fontWeight: FontWeight.bold,
                                                letterSpacing: 0.2,
                                              ),
                                          maxLines: 1,
                                          overflow: TextOverflow.ellipsis,
                                          softWrap: false,
                                        ),
                                      ),
                                    )
                                  : const SizedBox.shrink(),
                            ),
                          ),
                        ],
                      ],
                    ),
                    const SizedBox(height: ValoraSpacing.xs),
                    AnimatedOpacity(
                      opacity: widget.isSelected ? 1 : 0,
                      duration: ValoraAnimations.normal,
                      curve: Curves.easeOut,
                      child: Container(
                        width: ValoraSpacing.xs,
                        height: ValoraSpacing.xs,
                        decoration: const BoxDecoration(
                          color: ValoraColors.primary,
                          shape: BoxShape.circle,
                          boxShadow: [
                            BoxShadow(
                              color: ValoraColors.primary,
                              blurRadius: ValoraSpacing.xs,
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
      ),
    );
  }
}
