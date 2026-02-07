import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_animations.dart';

/// Empty state widget for when no content is available.
class ValoraEmptyState extends StatelessWidget {
  const ValoraEmptyState({
    super.key,
    required this.icon,
    required this.title,
    this.subtitle,
    this.action,
  });

  /// Icon to display
  final IconData icon;

  /// Primary message
  final String title;

  /// Secondary message
  final String? subtitle;

  /// Optional action button
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Center(
      child: Padding(
        padding: const EdgeInsets.all(ValoraSpacing.xl),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(ValoraSpacing.lg),
              decoration: BoxDecoration(
                color: colorScheme.surfaceContainerHighest,
                shape: BoxShape.circle,
              ),
              child: Icon(
                icon,
                size: ValoraSpacing.iconSizeXl,
                color: colorScheme.onSurfaceVariant,
              ),
            ).animate().fade().scale(curve: ValoraAnimations.emphatic),
            const SizedBox(height: ValoraSpacing.lg),
            Text(
                  title,
                  style: textTheme.titleMedium,
                  textAlign: TextAlign.center,
                )
                .animate()
                .fade(delay: 100.ms)
                .slideY(
                  begin: 0.2,
                  end: 0,
                  curve: ValoraAnimations.deceleration,
                ),
            if (subtitle != null) ...[
              const SizedBox(height: ValoraSpacing.sm),
              Text(
                    subtitle!,
                    style: textTheme.bodyMedium?.copyWith(
                      color: colorScheme.onSurfaceVariant,
                    ),
                    textAlign: TextAlign.center,
                  )
                  .animate()
                  .fade(delay: 200.ms)
                  .slideY(
                    begin: 0.2,
                    end: 0,
                    curve: ValoraAnimations.deceleration,
                  ),
            ],
            if (action != null) ...[
              const SizedBox(height: ValoraSpacing.lg),
              action!
                  .animate()
                  .fade(delay: 300.ms)
                  .scale(curve: ValoraAnimations.emphatic),
            ],
          ],
        ),
      ),
    );
  }
}
