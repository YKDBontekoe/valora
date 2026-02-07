import 'dart:ui';
import 'package:flutter/material.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';

class ValoraGlassContainer extends StatelessWidget {
  final Widget child;
  final double? width;
  final double? height;
  final EdgeInsetsGeometry? padding;
  final EdgeInsetsGeometry? margin;
  final BorderRadius? borderRadius;
  final Color? color;
  final Color? borderColor;
  final double blur;

  const ValoraGlassContainer({
    super.key,
    required this.child,
    this.width,
    this.height,
    this.padding,
    this.margin,
    this.borderRadius,
    this.color,
    this.borderColor,
    this.blur = 10.0,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    // Default values based on theme
    final effectiveColor =
        color ?? (isDark ? ValoraColors.glassBlack : ValoraColors.glassWhite);
    final effectiveBorderColor =
        borderColor ??
        (isDark ? ValoraColors.glassBorderDark : ValoraColors.glassBorderLight);
    final effectiveBorderRadius =
        borderRadius ?? BorderRadius.circular(ValoraSpacing.radiusLg);

    return Container(
      width: width,
      height: height,
      margin: margin,
      child: ClipRRect(
        borderRadius: effectiveBorderRadius,
        child: BackdropFilter(
          filter: ImageFilter.blur(sigmaX: blur, sigmaY: blur),
          child: Container(
            padding: padding,
            decoration: BoxDecoration(
              color: effectiveColor,
              borderRadius: effectiveBorderRadius,
              border: Border.all(color: effectiveBorderColor, width: 1.0),
            ),
            child: child,
          ),
        ),
      ),
    );
  }
}
