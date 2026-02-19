import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';

/// A premium glassmorphic badge for status indicators.
class ValoraBadge extends StatelessWidget {
  const ValoraBadge({
    super.key,
    required this.label,
    this.icon,
    this.color,
    this.textColor,
    this.size = ValoraBadgeSize.medium,
    this.enableBlur = true,
    this.enableAnimation = true,
  });

  final String label;
  final IconData? icon;
  final Color? color;
  final Color? textColor;
  final ValoraBadgeSize size;
  final bool enableBlur;
  final bool enableAnimation;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final effectiveColor = color ?? ValoraColors.primary;
    final effectiveTextColor = textColor ?? effectiveColor;

    final isSmall = size == ValoraBadgeSize.small;
    final fontSize = isSmall ? 10.0 : 11.0;
    final iconSize = isSmall ? 10.0 : 12.0;
    final hPad = isSmall ? ValoraSpacing.xs + 2 : ValoraSpacing.sm;
    final vPad = isSmall ? 2.0 : 4.0;

    Widget badge = Container(
      padding: EdgeInsets.symmetric(horizontal: hPad, vertical: vPad),
      decoration: BoxDecoration(
        color: effectiveColor.withValues(alpha: isDark ? 0.15 : 0.1),
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
        border: Border.all(
          color: effectiveColor.withValues(alpha: 0.2),
          width: 0.5,
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: iconSize, color: effectiveTextColor),
            SizedBox(width: isSmall ? 3 : ValoraSpacing.xs),
          ],
          Text(
            label,
            style: ValoraTypography.labelSmall.copyWith(
              color: effectiveTextColor,
              fontSize: fontSize,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );

    if (enableBlur) {
      badge = ClipRRect(
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
        child: BackdropFilter(
          filter: ImageFilter.blur(sigmaX: 8, sigmaY: 8),
          child: badge,
        ),
      );
    }

    if (enableAnimation) {
      badge = badge
          .animate()
          .fadeIn(duration: ValoraAnimations.normal)
          .scale(
            begin: const Offset(0.92, 0.92),
            end: const Offset(1, 1),
            duration: ValoraAnimations.normal,
            curve: ValoraAnimations.deceleration,
          );
    }

    return badge;
  }
}

enum ValoraBadgeSize { small, medium }
