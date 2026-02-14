import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';
import 'valora_button.dart';

/// Empty state display with icon, message, and optional action.
class ValoraEmptyState extends StatelessWidget {
  const ValoraEmptyState({
    super.key,
    required this.icon,
    required this.title,
    this.subtitle,
    this.actionLabel,
    this.onAction,
  });

  final IconData icon;
  final String title;
  final String? subtitle;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(ValoraSpacing.xl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Icon with subtle background circle
            Container(
              width: 80,
              height: 80,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: (isDark
                        ? ValoraColors.primaryLight
                        : ValoraColors.primary)
                    .withValues(alpha: 0.08),
              ),
              child: Icon(
                icon,
                size: 36,
                color: isDark
                    ? ValoraColors.neutral400
                    : ValoraColors.neutral400,
              ),
            )
                .animate()
                .fadeIn(duration: ValoraAnimations.medium)
                .scale(
                  begin: const Offset(0.8, 0.8),
                  end: const Offset(1, 1),
                  duration: ValoraAnimations.medium,
                  curve: ValoraAnimations.emphatic,
                ),
            const SizedBox(height: ValoraSpacing.lg),

            Text(
              title,
              style: ValoraTypography.titleMedium.copyWith(
                color:
                    isDark ? ValoraColors.neutral100 : ValoraColors.neutral800,
              ),
              textAlign: TextAlign.center,
            )
                .animate()
                .fadeIn(
                  delay: 100.ms,
                  duration: ValoraAnimations.medium,
                ),

            if (subtitle != null) ...[
              const SizedBox(height: ValoraSpacing.sm),
              Text(
                subtitle!,
                style: ValoraTypography.bodySmall.copyWith(
                  color: isDark
                      ? ValoraColors.neutral400
                      : ValoraColors.neutral500,
                ),
                textAlign: TextAlign.center,
              )
                  .animate()
                  .fadeIn(
                    delay: 200.ms,
                    duration: ValoraAnimations.medium,
                  ),
            ],

            if (actionLabel != null && onAction != null) ...[
              const SizedBox(height: ValoraSpacing.lg),
              ValoraButton(
                label: actionLabel!,
                onPressed: onAction!,
                variant: ValoraButtonVariant.secondary,
                size: ValoraButtonSize.small,
              )
                  .animate()
                  .fadeIn(
                    delay: 300.ms,
                    duration: ValoraAnimations.medium,
                  )
                  .slideY(
                    begin: 0.2,
                    end: 0,
                    duration: ValoraAnimations.medium,
                    curve: ValoraAnimations.deceleration,
                  ),
            ],
          ],
        ),
      ),
    );
  }
}
