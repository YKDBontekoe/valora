import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';

/// Premium shimmer loading placeholder.
class ValoraShimmer extends StatelessWidget {
  const ValoraShimmer({
    super.key,
    this.width,
    this.height,
    this.borderRadius,
  });

  final double? width;
  final double? height;
  final double? borderRadius;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final baseColor =
        isDark ? ValoraColors.neutral800 : ValoraColors.neutral100;
    final highlightColor =
        isDark ? ValoraColors.neutral700 : ValoraColors.neutral200;

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
        .animate(onPlay: (c) => c.repeat(reverse: true))
        .tint(
          color: highlightColor,
          duration: 1200.ms,
          curve: Curves.easeInOut,
        );
  }
}
