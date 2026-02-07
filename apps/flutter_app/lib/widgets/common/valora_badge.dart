import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';

/// Badge component for status indicators.
class ValoraBadge extends StatelessWidget {
  const ValoraBadge({super.key, required this.label, this.color, this.icon});

  /// Badge text
  final String label;

  /// Background color
  final Color? color;

  /// Optional icon
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final bgColor = color ?? ValoraColors.primary;

    return ClipRRect(
      borderRadius: BorderRadius.circular(ValoraSpacing.radiusSm),
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 5, sigmaY: 5),
        child: Container(
          padding: const EdgeInsets.symmetric(
            horizontal: ValoraSpacing.sm,
            vertical: ValoraSpacing.xs,
          ),
          decoration: BoxDecoration(
            color: bgColor.withValues(alpha: 0.8), // Glass opacity
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusSm),
            border: Border.all(
              color: Colors.white.withValues(alpha: 0.2),
              width: 1,
            ),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (icon != null) ...[
                Icon(icon, size: ValoraSpacing.iconSizeSm, color: Colors.white),
                const SizedBox(width: ValoraSpacing.xs),
              ],
              Text(
                label,
                style: ValoraTypography.labelSmall.copyWith(
                  color: Colors.white,
                  fontWeight: FontWeight.w600,
                  shadows: [
                    Shadow(
                      color: Colors.black.withValues(alpha: 0.3),
                      blurRadius: 2,
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    ).animate().fadeIn().scale(curve: ValoraAnimations.emphatic);
  }
}
