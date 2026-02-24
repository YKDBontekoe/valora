import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';
import 'valora_card.dart';

/// A styled dialog using ValoraCard for consistent theming.
class ValoraDialog extends StatelessWidget {
  const ValoraDialog({
    super.key,
    required this.title,
    required this.child,
    this.actions = const [],
  });

  final String title;
  final Widget child;
  final List<Widget> actions;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Dialog(
      backgroundColor: Colors.transparent,
      elevation: 0,
      insetPadding: const EdgeInsets.all(ValoraSpacing.lg),
      child: ValoraCard(
        borderRadius: ValoraSpacing.radiusXxl,
        elevation: ValoraSpacing.elevationLg,
        padding: const EdgeInsets.all(ValoraSpacing.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: ValoraTypography.titleLarge.copyWith(
                color: colorScheme.onSurface,
              ),
            ),
            const SizedBox(height: ValoraSpacing.md),
            Flexible(
              child: SingleChildScrollView(
                child: DefaultTextStyle(
                  style: ValoraTypography.bodyMedium.copyWith(
                    color: colorScheme.onSurfaceVariant,
                  ),
                  child: child,
                ),
              ),
            ),
            if (actions.isNotEmpty) ...[
              const SizedBox(height: ValoraSpacing.lg),
              SizedBox(
                width: double.infinity,
                child: Wrap(
                  alignment: WrapAlignment.end,
                  spacing: ValoraSpacing.sm,
                  runSpacing: ValoraSpacing.sm,
                  children: actions,
                ),
              ),
            ],
          ],
        ),
      )
          .animate()
          .fadeIn(duration: ValoraAnimations.normal)
          .scale(
            begin: const Offset(0.92, 0.92),
            end: const Offset(1, 1),
            duration: ValoraAnimations.medium,
            curve: ValoraAnimations.deceleration,
          ),
    );
  }
}
