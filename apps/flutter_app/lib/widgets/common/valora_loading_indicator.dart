import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_animations.dart';

/// Loading indicator with optional message.
class ValoraLoadingIndicator extends StatelessWidget {
  const ValoraLoadingIndicator({super.key, this.message});

  /// Optional loading message
  final String? message;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          SizedBox(
            width: ValoraSpacing.iconSizeLg,
            height: ValoraSpacing.iconSizeLg,
            child: CircularProgressIndicator(
              strokeWidth: 3,
              valueColor: AlwaysStoppedAnimation(colorScheme.primary),
            ),
          ),
          if (message != null) ...[
            const SizedBox(height: ValoraSpacing.md),
            Text(
              message!,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                color: colorScheme.onSurfaceVariant,
              ),
            ).animate().fade().shimmer(duration: ValoraAnimations.verySlow),
          ],
        ],
      ),
    );
  }
}
