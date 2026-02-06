import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';
import 'valora_card.dart';

/// A styled dialog container using `ValoraCard` styling.
class ValoraDialog extends StatelessWidget {
  const ValoraDialog({
    super.key,
    required this.title,
    required this.child,
    required this.actions,
  });

  final String title;
  final Widget child;
  final List<Widget> actions;

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: Colors.transparent,
      elevation: 0,
      insetPadding: const EdgeInsets.all(ValoraSpacing.md),
      child: ValoraCard(
        padding: const EdgeInsets.all(ValoraSpacing.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              title,
              style: ValoraTypography.headlineSmall,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: ValoraSpacing.lg),
            Flexible(
              child: SingleChildScrollView(
                child: child,
              ),
            ),
            const SizedBox(height: ValoraSpacing.xl),
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: actions.map((action) {
                return Padding(
                  padding: const EdgeInsets.only(left: ValoraSpacing.sm),
                  child: action,
                );
              }).toList(),
            ),
          ],
        ),
      ),
    ).animate().fade().scale(curve: ValoraAnimations.emphatic);
  }
}
