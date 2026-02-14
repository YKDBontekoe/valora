import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';

/// Premium loading indicator with optional animated message.
class ValoraLoadingIndicator extends StatelessWidget {
  const ValoraLoadingIndicator({super.key, this.message, this.size});

  final String? message;
  final double? size;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SizedBox(
            width: size ?? ValoraSpacing.xl + ValoraSpacing.md,
            height: size ?? ValoraSpacing.xl + ValoraSpacing.md,
            child: CircularProgressIndicator(
              strokeWidth: 3,
              strokeCap: StrokeCap.round,
              valueColor: AlwaysStoppedAnimation(
                isDark ? ValoraColors.primaryLight : ValoraColors.primary,
              ),
              backgroundColor: isDark
                  ? ValoraColors.neutral800
                  : ValoraColors.neutral200,
            ),
          ),
          if (message != null) ...[
            const SizedBox(height: ValoraSpacing.md),
            Text(
              message!,
              style: ValoraTypography.bodySmall.copyWith(
                color: isDark
                    ? ValoraColors.neutral400
                    : ValoraColors.neutral500,
              ),
              textAlign: TextAlign.center,
            )
                .animate(onPlay: (c) => c.repeat(reverse: true))
                .fadeIn(duration: ValoraAnimations.verySlow)
                .then()
                .fade(
                  begin: 1,
                  end: 0.4,
                  duration: ValoraAnimations.verySlow,
                ),
          ],
        ],
      ),
    );
  }
}
