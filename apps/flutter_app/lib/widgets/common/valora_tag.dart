import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';

/// A styled tag / pill component with hover and tap interactivity.
class ValoraTag extends StatefulWidget {
  const ValoraTag({
    super.key,
    required this.label,
    this.icon,
    this.backgroundColor,
    this.textColor,
    this.borderColor,
    this.onTap,
  });

  final String label;
  final IconData? icon;
  final Color? backgroundColor;
  final Color? textColor;
  final Color? borderColor;
  final VoidCallback? onTap;

  @override
  State<ValoraTag> createState() => _ValoraTagState();
}

class _ValoraTagState extends State<ValoraTag> {
  bool _isHovered = false;
  bool _isPressed = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    final bgColor =
        widget.backgroundColor ??
        (isDark
            ? ValoraColors.neutral800.withValues(alpha: 0.6)
            : ValoraColors.neutral100);
    final textColor =
        widget.textColor ??
        (isDark ? ValoraColors.neutral300 : ValoraColors.neutral600);
    final effectiveBorderColor =
        widget.borderColor ??
        (isDark
            ? ValoraColors.neutral700.withValues(alpha: 0.4)
            : ValoraColors.neutral200.withValues(alpha: 0.8));

    Widget tag = AnimatedContainer(
      duration: ValoraAnimations.fast,
      padding: const EdgeInsets.symmetric(
        horizontal: ValoraSpacing.sm + 2,
        vertical: ValoraSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: _isHovered ? bgColor.withValues(alpha: 0.9) : bgColor,
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
        border: Border.all(color: effectiveBorderColor, width: 1),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (widget.icon != null) ...[
            Icon(widget.icon, size: 12, color: textColor),
            const SizedBox(width: ValoraSpacing.xs),
          ],
          Text(
            widget.label,
            style: ValoraTypography.labelSmall.copyWith(
              color: textColor,
              fontWeight: FontWeight.w500,
            ),
          ),
        ],
      ),
    );

    if (widget.onTap != null) {
      tag = MouseRegion(
        onEnter: (_) => setState(() => _isHovered = true),
        onExit: (_) => setState(() {
          _isHovered = false;
          _isPressed = false;
        }),
        cursor: SystemMouseCursors.click,
        child: GestureDetector(
          onTapDown: (_) => setState(() => _isPressed = true),
          onTapUp: (_) => setState(() => _isPressed = false),
          onTapCancel: () => setState(() => _isPressed = false),
          onTap: widget.onTap,
          child: tag,
        ),
      );

      tag = tag
          .animate(target: _isPressed ? 1 : 0)
          .scale(
            end: const Offset(0.95, 0.95),
            duration: ValoraAnimations.fast,
          );
    }

    return tag;
  }
}
