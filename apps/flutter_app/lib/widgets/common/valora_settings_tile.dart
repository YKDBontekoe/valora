import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_animations.dart';
import '../../core/theme/valora_typography.dart';

class ValoraSettingsTile extends StatefulWidget {
  const ValoraSettingsTile({
    super.key,
    required this.icon,
    required this.title,
    this.subtitle,
    this.iconColor,
    this.iconBackgroundColor,
    this.onTap,
    this.showDivider = true,
    this.showChevron = true,
    this.trailing,
  });

  final IconData icon;
  final String title;
  final String? subtitle;
  final Color? iconColor;
  final Color? iconBackgroundColor;
  final VoidCallback? onTap;
  final bool showDivider;
  final bool showChevron;
  final Widget? trailing;

  @override
  State<ValoraSettingsTile> createState() => _ValoraSettingsTileState();
}

class _ValoraSettingsTileState extends State<ValoraSettingsTile> {
  bool _isHovered = false;
  bool _isPressed = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final colorScheme = Theme.of(context).colorScheme;
    final isInteractive = widget.onTap != null;

    final effectiveIconColor = widget.iconColor ?? colorScheme.primary;
    final effectiveIconBg = widget.iconBackgroundColor ??
        effectiveIconColor.withValues(alpha: 0.1);

    final textColor = colorScheme.onSurface;
    final subtitleColor = colorScheme.onSurfaceVariant;
    final dividerColor = isDark
        ? ValoraColors.neutral800
        : ValoraColors.neutral100;

    // Hover/Press background color
    final interactiveColor = isDark
        ? ValoraColors.neutral800.withValues(alpha: 0.5)
        : ValoraColors.neutral100.withValues(alpha: 0.5);

    final showHighlight = isInteractive && (_isHovered || _isPressed);

    return MouseRegion(
      onEnter: isInteractive ? (_) => setState(() => _isHovered = true) : null,
      onExit: isInteractive
          ? (_) => setState(() {
              _isHovered = false;
              _isPressed = false;
            })
          : null,
      cursor: isInteractive ? SystemMouseCursors.click : SystemMouseCursors.basic,
      child: GestureDetector(
        onTapDown: isInteractive ? (_) => setState(() => _isPressed = true) : null,
        onTapUp: isInteractive ? (_) => setState(() => _isPressed = false) : null,
        onTapCancel: isInteractive ? () => setState(() => _isPressed = false) : null,
        onTap: isInteractive
            ? () {
                HapticFeedback.lightImpact();
                widget.onTap!();
              }
            : null,
        child: AnimatedContainer(
          duration: ValoraAnimations.fast,
          color: showHighlight ? interactiveColor : Colors.transparent,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: ValoraSpacing.lg,
                  vertical: ValoraSpacing.md,
                ),
                child: Row(
                  children: [
                    // Icon Container
                    Container(
                      width: 40,
                      height: 40,
                      decoration: BoxDecoration(
                        color: effectiveIconBg,
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        widget.icon,
                        color: effectiveIconColor,
                        size: ValoraSpacing.iconSizeSm,
                      ),
                    )
                    .animate(target: showHighlight ? 1 : 0)
                    .scale(
                      begin: const Offset(1, 1),
                      end: const Offset(1.1, 1.1),
                      duration: ValoraAnimations.fast,
                      curve: ValoraAnimations.snappy,
                    ),

                    const SizedBox(width: ValoraSpacing.md),

                    // Text Content
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            widget.title,
                            style: ValoraTypography.bodyLarge.copyWith(
                              color: textColor,
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                          if (widget.subtitle != null) ...[
                            const SizedBox(height: 2),
                            Text(
                              widget.subtitle!,
                              style: ValoraTypography.bodySmall.copyWith(
                                color: subtitleColor,
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),

                    // Trailing
                    if (widget.trailing != null)
                      widget.trailing!
                    else if (widget.showChevron && isInteractive)
                      Icon(
                        Icons.chevron_right_rounded,
                        color: isDark
                            ? ValoraColors.neutral500
                            : ValoraColors.neutral300,
                        size: ValoraSpacing.iconSizeMd,
                      )
                      .animate(target: showHighlight ? 1 : 0)
                      .moveX(
                        begin: 0,
                        end: 4,
                        duration: ValoraAnimations.fast,
                        curve: ValoraAnimations.snappy,
                      ),
                  ],
                ),
              ),
              if (widget.showDivider)
                Divider(
                  height: 1,
                  thickness: 1,
                  color: dividerColor,
                  indent: 72, // Aligned with text start
                ),
            ],
          ),
        ),
      ),
    );
  }
}
