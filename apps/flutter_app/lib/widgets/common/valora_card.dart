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
    this.border,
    this.clipBehavior,
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

  /// Optional background color override
  final Color? backgroundColor;

  /// Optional border
  final BoxBorder? border;

  /// Clip behavior
  final Clip? clipBehavior;

  @override
  State<ValoraCard> createState() => _ValoraCardState();
}

class _ValoraCardState extends State<ValoraCard> {
  final _isHovered = ValueNotifier<bool>(false);
  final _isPressed = ValueNotifier<bool>(false);

  @override
  void dispose() {
    _isHovered.dispose();
    _isPressed.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final cardRadius = widget.borderRadius ?? ValoraSpacing.radiusLg;
    final baseElevation = widget.elevation ?? ValoraSpacing.elevationSm;

    // Pre-calculate shadow sets
    List<BoxShadow> baseShadows;
    List<BoxShadow> hoverShadows;

    if (baseElevation <= ValoraSpacing.elevationNone) {
      baseShadows = [];
      hoverShadows = [];
    } else if (baseElevation <= ValoraSpacing.elevationSm) {
      baseShadows = isDark ? ValoraShadows.smDark : ValoraShadows.sm;
      hoverShadows = isDark ? ValoraShadows.mdDark : ValoraShadows.md;
    } else if (baseElevation <= ValoraSpacing.elevationMd) {
      baseShadows = isDark ? ValoraShadows.mdDark : ValoraShadows.md;
      hoverShadows = isDark ? ValoraShadows.lgDark : ValoraShadows.lg;
    } else {
      baseShadows = isDark ? ValoraShadows.lgDark : ValoraShadows.lg;
      hoverShadows = isDark ? ValoraShadows.xlDark : ValoraShadows.xl;
    }

    // Static content preparation
    final cardContent = Container(
      padding: widget.padding ?? const EdgeInsets.all(ValoraSpacing.cardPadding),
      child: widget.child,
    );

    return Padding(
      padding: widget.margin ?? EdgeInsets.zero,
      child: ValueListenableBuilder<bool>(
        valueListenable: _isHovered,
        builder: (context, isHovered, _) {
          return ValueListenableBuilder<bool>(
            valueListenable: _isPressed,
            builder: (context, isPressed, _) {
              List<BoxShadow> currentShadows;
              if (isPressed) {
                currentShadows = baseShadows;
              } else if (isHovered) {
                currentShadows = hoverShadows;
              } else {
                currentShadows = baseShadows;
              }

              Widget content = widget.onTap != null
                  ? Material(
                      color: Colors.transparent,
                      child: InkWell(
                        onTap: () {
                          HapticFeedback.lightImpact();
                          widget.onTap?.call();
                        },
                        onHover: (bool hovered) => _isHovered.value = hovered,
                        onTapDown: (TapDownDetails _) => _isPressed.value = true,
                        onTapUp: (TapUpDetails _) => _isPressed.value = false,
                        onTapCancel: () => _isPressed.value = false,
                        borderRadius: BorderRadius.circular(cardRadius),
                        child: cardContent,
                      ),
                    )
                  : cardContent;

              return AnimatedContainer(
                duration: ValoraAnimations.normal,
                curve: ValoraAnimations.standard,
                decoration: BoxDecoration(
                  color: widget.backgroundColor ??
                      (widget.gradient == null
                          ? (isDark
                              ? ValoraColors.surfaceDark
                              : ValoraColors.surfaceLight)
                          : null),
                  gradient: widget.gradient,
                  borderRadius: BorderRadius.circular(cardRadius),
                  border: widget.border,
                  boxShadow: currentShadows,
                ),
                clipBehavior: widget.clipBehavior ?? Clip.antiAlias,
                child: content,
              )
              .animate(target: isPressed ? 1 : 0)
              .scale(
                end: const Offset(0.98, 0.98),
                duration: ValoraAnimations.fast,
                curve: ValoraAnimations.standard,
              )
              .animate(target: isHovered && !isPressed ? 1 : 0)
              .scale(
                end: const Offset(1.02, 1.02),
                duration: ValoraAnimations.normal,
                curve: ValoraAnimations.standard,
              );
            },
          );
        },
      ),
    );
  }
}
