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

    // Interactive elevation logic: maps elevation values to shadow tokens
    List<BoxShadow> currentShadows;

    // Determine target shadow based on state and elevation
    if (baseElevation <= ValoraSpacing.elevationNone) {
      currentShadows = [];
    } else {
      // Determine base shadow level
      List<BoxShadow> baseShadows;
      List<BoxShadow> hoverShadows;

      if (baseElevation <= ValoraSpacing.elevationSm) {
        baseShadows = isDark ? ValoraShadows.smDark : ValoraShadows.sm;
        hoverShadows = isDark ? ValoraShadows.mdDark : ValoraShadows.md;
      } else if (baseElevation <= ValoraSpacing.elevationMd) {
        baseShadows = isDark ? ValoraShadows.mdDark : ValoraShadows.md;
        hoverShadows = isDark ? ValoraShadows.lgDark : ValoraShadows.lg;
      } else {
        baseShadows = isDark ? ValoraShadows.lgDark : ValoraShadows.lg;
        hoverShadows = isDark ? ValoraShadows.xlDark : ValoraShadows.xl;
      }

      if (_isPressed) {
        currentShadows = baseShadows; // Pressing flattens back to base
      } else if (_isHovered) {
        currentShadows = hoverShadows;
      } else {
        currentShadows = baseShadows;
      }
    }

    Widget cardContent = Container(
      padding:
          widget.padding ?? const EdgeInsets.all(ValoraSpacing.cardPadding),
      decoration: widget.gradient != null
          ? BoxDecoration(
              gradient: widget.gradient,
              borderRadius: BorderRadius.circular(cardRadius),
            )
          : null,
      child: widget.child,
    );

    if (widget.onTap != null) {
      cardContent = Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: () {
            HapticFeedback.lightImpact();
            widget.onTap?.call();
          },
          onHover: (hovered) => setState(() => _isHovered = hovered),
          onTapDown: (_) => setState(() => _isPressed = true),
          onTapUp: (_) => setState(() => _isPressed = false),
          onTapCancel: () => setState(() => _isPressed = false),
          borderRadius: BorderRadius.circular(cardRadius),
          child: cardContent,
        ),
      );
    } else if (widget.onTap != null) {
      // InkWell with tap interaction
      cardContent = Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: widget.onTap,
          onHover: (isHovering) => setState(() => _isHovered = isHovering),
          onTapDown: (_) => setState(() => _isPressed = true),
          onTapUp: (_) => setState(() => _isPressed = false),
          onTapCancel: () => setState(() => _isPressed = false),
          child: cardContent,
        ),
      );
    }
    // No MouseRegion hover effect when non-interactive

    return AnimatedContainer(
          duration: ValoraAnimations.normal,
          curve: ValoraAnimations.standard,
          margin: widget.margin,
          decoration: BoxDecoration(
            color: widget.gradient == null
                ? (isDark
                      ? ValoraColors.surfaceDark
                      : ValoraColors.surfaceLight)
                : null,
            borderRadius: BorderRadius.circular(cardRadius),
            boxShadow: currentShadows,
          ),
          clipBehavior: Clip.antiAlias,
          child: cardContent,
        )
        .animate(target: _isPressed ? 1 : 0) // Press scale
        .scale(
          end: const Offset(0.98, 0.98),
          duration: ValoraAnimations.fast,
          curve: ValoraAnimations.standard,
        )
        .animate(target: _isHovered && !_isPressed ? 1 : 0) // Hover lift
        .scale(
          end: const Offset(1.02, 1.02),
          duration: ValoraAnimations.normal,
          curve: ValoraAnimations.standard,
        );
  }
}
