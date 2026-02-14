import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_animations.dart';
import '../../core/theme/valora_shadows.dart';

/// Versatile card component with premium surface treatment.
///
/// Features: adaptive elevation, subtle borders, hover glow, press scale,
/// and optional gradient overlay.
class ValoraCard extends StatefulWidget {
  const ValoraCard({
    super.key,
    required this.child,
    this.padding,
    this.margin,
    this.elevation = ValoraSpacing.elevationSm,
    this.borderRadius = ValoraSpacing.radiusLg,
    this.onTap,
    this.gradient,
    this.backgroundColor,
    this.borderColor,
    this.gradientBorder,
    this.borderWidth,
    this.clipBehavior = Clip.antiAlias,
  });

  final Widget child;
  final EdgeInsetsGeometry? padding;
  final EdgeInsetsGeometry? margin;
  final double elevation;
  final double borderRadius;
  final VoidCallback? onTap;
  final Gradient? gradient;
  final Color? backgroundColor;
  final Color? borderColor;
  final Gradient? gradientBorder;
  final double? borderWidth;
  final Clip clipBehavior;

  @override
  State<ValoraCard> createState() => _ValoraCardState();
}

class _ValoraCardState extends State<ValoraCard> {
  bool _isHovered = false;
  bool _isPressed = false;

  List<BoxShadow> _resolveShadow(bool isDark) {
    if (_isPressed) return isDark ? ValoraShadows.smDark : ValoraShadows.sm;
    if (_isHovered) return isDark ? ValoraShadows.lgDark : ValoraShadows.lg;

    if (widget.elevation <= ValoraSpacing.elevationNone) return [];
    if (widget.elevation <= ValoraSpacing.elevationSm) {
      return isDark ? ValoraShadows.smDark : ValoraShadows.sm;
    }
    if (widget.elevation <= ValoraSpacing.elevationMd) {
      return isDark ? ValoraShadows.mdDark : ValoraShadows.md;
    }
    return isDark ? ValoraShadows.lgDark : ValoraShadows.lg;
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    final backgroundColor = widget.backgroundColor ??
        (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight);

    final borderColor = widget.borderColor ??
        (isDark
            ? ValoraColors.neutral700.withValues(alpha: 0.5)
            : ValoraColors.neutral200.withValues(alpha: 0.7));

    final effectiveBorderWidth = widget.borderWidth ?? (_isHovered ? 1.5 : 1.0);

    Widget cardContent = widget.padding != null
        ? Padding(padding: widget.padding!, child: widget.child)
        : widget.child;

    Widget card;
    if (widget.gradientBorder != null) {
      card = AnimatedContainer(
        duration: ValoraAnimations.normal,
        curve: ValoraAnimations.smooth,
        margin: widget.margin,
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(widget.borderRadius),
          gradient: widget.gradientBorder,
          boxShadow: _resolveShadow(isDark),
        ),
        padding: EdgeInsets.all(effectiveBorderWidth),
        child: Container(
          decoration: BoxDecoration(
            color: backgroundColor,
            borderRadius: BorderRadius.circular(
              widget.borderRadius - effectiveBorderWidth,
            ),
            gradient: widget.gradient,
          ),
          clipBehavior: widget.clipBehavior,
          child: cardContent,
        ),
      );
    } else {
      card = AnimatedContainer(
        duration: ValoraAnimations.normal,
        curve: ValoraAnimations.smooth,
        margin: widget.margin,
        decoration: BoxDecoration(
          color: widget.gradient != null ? null : backgroundColor,
          gradient: widget.gradient,
          borderRadius: BorderRadius.circular(widget.borderRadius),
          border: Border.all(
            color: _isHovered
                ? (isDark
                    ? ValoraColors.neutral600.withValues(alpha: 0.7)
                    : ValoraColors.neutral300.withValues(alpha: 0.9))
                : borderColor,
            width: effectiveBorderWidth,
          ),
          boxShadow: _resolveShadow(isDark),
        ),
        clipBehavior: widget.clipBehavior,
        child: cardContent,
      );
    }

    if (widget.onTap != null) {
      card = MouseRegion(
        onEnter: (_) => setState(() => _isHovered = true),
        onExit: (_) => setState(() {
          _isHovered = false;
          _isPressed = false;
        }),
        cursor: SystemMouseCursors.click,
        child: GestureDetector(
          onTapDown: (_) => setState(() => _isPressed = true),
          onTapUp: (_) => setState(() => _isPressed = false),
          onTapCancel: () => setState(() => _isPressed = false),
          onTap: widget.onTap,
          child: card,
        ),
      );

      // Hover and Press animations
      card = card
          .animate(target: _isHovered && !_isPressed ? 1 : 0)
          .scale(
            end: const Offset(1.02, 1.02),
            duration: ValoraAnimations.normal,
            curve: ValoraAnimations.smooth,
          )
          .animate(target: _isPressed ? 1 : 0)
          .scale(
            end: const Offset(0.97, 0.97),
            duration: ValoraAnimations.fast,
            curve: ValoraAnimations.snappy,
          );
    }

    return card;
  }
}
