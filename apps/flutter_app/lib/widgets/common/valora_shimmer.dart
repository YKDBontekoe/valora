import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';

/// A shimmer effect widget for loading states.
class ValoraShimmer extends StatelessWidget {
  const ValoraShimmer({
    super.key,
    required this.width,
    required this.height,
    this.borderRadius,
  });

  final double width;
  final double height;
  final double? borderRadius;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final baseColor = isDark
        ? ValoraColors.surfaceVariantDark
        : ValoraColors.neutral100;

    // flutter_animate's shimmer is easier to use
    const bool isTest = bool.fromEnvironment('FLUTTER_TEST');

    return Container(
          width: width,
          height: height,
          decoration: BoxDecoration(
            color: baseColor,
            borderRadius: BorderRadius.circular(
              borderRadius ?? ValoraSpacing.radiusMd,
            ),
          ),
        )
        .animate(onPlay: (controller) {
          if (!isTest) {
            controller.repeat();
          }
        })
        .shimmer(
          duration: 1500.ms,
          color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral50,
        );
  }
}
