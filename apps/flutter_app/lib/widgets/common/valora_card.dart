import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_animations.dart';
import '../../core/theme/valora_shadows.dart';

/// A versatile card component with consistent styling and interactions.
///
/// Use for content containers like listings, info panels, etc.
class ValoraCard extends StatefulWidget {
  const ValoraCard({
    super.key,
    required this.child,
    this.padding,
    this.margin,
    this.onTap,
    this.elevation,
    this.borderRadius,
    this.gradient,
    this.backgroundColor,
    this.clipBehavior = Clip.antiAlias,
  });

  /// The content of the card
  final Widget child;

  /// Custom padding (defaults to ValoraSpacing.cardPadding)
  final EdgeInsetsGeometry? padding;

  /// Custom margin
  final EdgeInsetsGeometry? margin;

  /// Tap callback for interactive cards
  final VoidCallback? onTap;

  /// Custom elevation (defaults to ValoraSpacing.elevationSm)
  final double? elevation;

  /// Custom border radius (defaults to ValoraSpacing.radiusLg)
  final double? borderRadius;

  /// Optional gradient background
  final Gradient? gradient;

  /// Optional background color
  final Color? backgroundColor;

  /// Clip behavior
  final Clip clipBehavior;

  @override
  State<ValoraCard> createState() => _ValoraCardState();
}

class _ValoraCardState extends State<ValoraCard> {
  bool _isHovered = false;
  bool _isPressed = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final cardRadius = widget.borderRadius ?? ValoraSpacing.radiusLg;
    final baseElevation = widget.elevation ?? ValoraSpacing.elevationSm;

    // Determine shadow levels
    List<BoxShadow> currentShadows;

    // Helper to get shadows
    List<BoxShadow> getShadows(double elevation, bool isDark) {
      if (elevation <= ValoraSpacing.elevationNone) return [];
      if (elevation <= ValoraSpacing.elevationSm) return isDark ? ValoraShadows.smDark : ValoraShadows.sm;
      if (elevation <= ValoraSpacing.elevationMd) return isDark ? ValoraShadows.mdDark : ValoraShadows.md;
      return isDark ? ValoraShadows.lgDark : ValoraShadows.lg;
    }

     // Helper to get hover shadows
    List<BoxShadow> getHoverShadows(double elevation, bool isDark) {
      if (elevation <= ValoraSpacing.elevationNone) return [];
      if (elevation <= ValoraSpacing.elevationSm) return isDark ? ValoraShadows.mdDark : ValoraShadows.md;
      if (elevation <= ValoraSpacing.elevationMd) return isDark ? ValoraShadows.lgDark : ValoraShadows.lg;
      return isDark ? ValoraShadows.xlDark : ValoraShadows.xl;
    }

    if (_isPressed) {
       currentShadows = getShadows(baseElevation, isDark);
    } else if (_isHovered) {
       currentShadows = getHoverShadows(baseElevation, isDark);
    } else {
       currentShadows = getShadows(baseElevation, isDark);
    }

    final bgColor = widget.backgroundColor ??
        (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight);

    return AnimatedContainer(
      duration: ValoraAnimations.normal,
      curve: ValoraAnimations.standard,
      margin: widget.margin,
      decoration: BoxDecoration(
        color: widget.gradient == null ? bgColor : null,
        gradient: widget.gradient,
        borderRadius: BorderRadius.circular(cardRadius),
        boxShadow: currentShadows,
      ),
      child: Material(
        color: Colors.transparent,
        borderRadius: BorderRadius.circular(cardRadius),
        clipBehavior: widget.clipBehavior,
        child: InkWell(
          onTap: widget.onTap != null
              ? () {
                  HapticFeedback.lightImpact();
                  widget.onTap!();
                }
              : null,
          onHover: (hovered) {
             if (widget.onTap != null) setState(() => _isHovered = hovered);
          },
          onTapDown: (_) {
             if (widget.onTap != null) setState(() => _isPressed = true);
          },
          onTapUp: (_) {
             if (widget.onTap != null) setState(() => _isPressed = false);
          },
          onTapCancel: () {
             if (widget.onTap != null) setState(() => _isPressed = false);
          },
          child: Padding(
            padding: widget.padding ?? const EdgeInsets.all(ValoraSpacing.cardPadding),
            child: widget.child,
          ),
        ),
      ),
    )
    .animate(target: _isPressed ? 1 : 0)
    .scale(
      end: const Offset(0.98, 0.98),
      duration: ValoraAnimations.fast,
      curve: ValoraAnimations.standard,
    )
    .animate(target: _isHovered && !_isPressed ? 1 : 0)
    .scale(
      end: const Offset(1.01, 1.01),
      duration: ValoraAnimations.normal,
      curve: ValoraAnimations.standard,
    );
  }
}
