import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_animations.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_shadows.dart';

/// Button component with multiple premium variants.
enum ValoraButtonVariant { primary, secondary, outline, ghost }

class ValoraButton extends StatefulWidget {
  const ValoraButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.variant = ValoraButtonVariant.primary,
    this.icon,
    this.isLoading = false,
    this.isFullWidth = false,
    this.size = ValoraButtonSize.medium,
  });

  /// Button label
  final String label;

  /// Press callback
  final VoidCallback? onPressed;

  /// Visual variant
  final ValoraButtonVariant variant;

  /// Optional leading icon
  final IconData? icon;

  /// Loading state
  final bool isLoading;

  /// Whether button takes full width
  final bool isFullWidth;

  /// Size variant
  final ValoraButtonSize size;

  @override
  State<ValoraButton> createState() => _ValoraButtonState();
}

enum ValoraButtonSize { small, medium, large }

class _ValoraButtonState extends State<ValoraButton> {
  bool _isHovered = false;
  bool _isPressed = false;

  EdgeInsets _getPadding() {
    switch (widget.size) {
      case ValoraButtonSize.small:
        return const EdgeInsets.symmetric(
          horizontal: ValoraSpacing.md,
          vertical: ValoraSpacing.sm,
        );
      case ValoraButtonSize.medium:
        return const EdgeInsets.symmetric(
          horizontal: ValoraSpacing.lg,
          vertical: ValoraSpacing.md - 2,
        );
      case ValoraButtonSize.large:
        return const EdgeInsets.symmetric(
          horizontal: ValoraSpacing.xl,
          vertical: ValoraSpacing.md,
        );
    }
  }

  double _getMinHeight() {
    switch (widget.size) {
      case ValoraButtonSize.small:
        return ValoraSpacing.buttonHeightSm;
      case ValoraButtonSize.medium:
        return ValoraSpacing.buttonHeightMd;
      case ValoraButtonSize.large:
        return ValoraSpacing.buttonHeightLg;
    }
  }

  double _getIconSize() {
    switch (widget.size) {
      case ValoraButtonSize.small:
        return ValoraSpacing.iconSizeSm;
      case ValoraButtonSize.medium:
        return ValoraSpacing.iconSizeSm + 2;
      case ValoraButtonSize.large:
        return ValoraSpacing.iconSizeMd;
    }
  }

  TextStyle _getTextStyle() {
    switch (widget.size) {
      case ValoraButtonSize.small:
        return ValoraTypography.labelMedium.copyWith(fontWeight: FontWeight.w600);
      case ValoraButtonSize.medium:
        return ValoraTypography.labelLarge.copyWith(fontWeight: FontWeight.w600);
      case ValoraButtonSize.large:
        return ValoraTypography.bodyLarge.copyWith(fontWeight: FontWeight.w600);
    }
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final padding = _getPadding();
    final minHeight = _getMinHeight();
    final iconSize = _getIconSize();
    final textStyle = _getTextStyle();

    // Build the child content
    final Widget childContent = AnimatedSwitcher(
      duration: ValoraAnimations.normal,
      switchInCurve: ValoraAnimations.emphatic,
      switchOutCurve: ValoraAnimations.acceleration,
      transitionBuilder: (child, animation) =>
          ScaleTransition(scale: animation, child: child),
      child: widget.isLoading
          ? SizedBox(
              key: const ValueKey('loading'),
              width: ValoraSpacing.iconSizeSm,
              height: ValoraSpacing.iconSizeSm,
              child: CircularProgressIndicator(
                strokeWidth: 2,
                strokeCap: StrokeCap.round,
                valueColor: AlwaysStoppedAnimation(
                  widget.variant == ValoraButtonVariant.primary
                      ? colorScheme.onPrimary
                      : colorScheme.primary,
                ),
              ),
            )
          : Row(
              key: const ValueKey('content'),
              mainAxisSize: widget.isFullWidth
                  ? MainAxisSize.max
                  : MainAxisSize.min,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                if (widget.icon != null) ...[
                  Icon(widget.icon, size: iconSize),
                  const SizedBox(width: ValoraSpacing.sm),
                ],
                Flexible(
                  child: Text(
                    widget.label,
                    style: textStyle,
                    overflow: TextOverflow.ellipsis,
                    maxLines: 1,
                  ),
                ),
              ],
            ),
    );

    final effectiveOnPressed = widget.isLoading ? null : widget.onPressed;
    Widget button;
    final borderRadius = BorderRadius.circular(ValoraSpacing.radiusXl);

    switch (widget.variant) {
      case ValoraButtonVariant.primary:
        button = AnimatedContainer(
          duration: ValoraAnimations.normal,
          decoration: BoxDecoration(
            borderRadius: borderRadius,
            boxShadow: _isHovered && !_isPressed
                ? (isDark ? ValoraShadows.primaryDark : ValoraShadows.primary)
                : [],
          ),
          child: ElevatedButton(
            onPressed: effectiveOnPressed != null
                ? () {
                    HapticFeedback.lightImpact();
                    effectiveOnPressed();
                  }
                : null,
            style: ElevatedButton.styleFrom(
              backgroundColor: colorScheme.primary,
              foregroundColor: colorScheme.onPrimary,
              disabledBackgroundColor: isDark
                  ? ValoraColors.neutral700
                  : ValoraColors.neutral200,
              disabledForegroundColor: ValoraColors.neutral400,
              elevation: 0,
              padding: padding,
              minimumSize: Size(0, minHeight),
              shape: RoundedRectangleBorder(borderRadius: borderRadius),
            ),
            child: childContent,
          ),
        );
        break;
      case ValoraButtonVariant.secondary:
        button = ElevatedButton(
          onPressed: effectiveOnPressed != null
              ? () {
                  HapticFeedback.lightImpact();
                  effectiveOnPressed();
                }
              : null,
          style: ElevatedButton.styleFrom(
            backgroundColor: colorScheme.primary.withValues(alpha: 0.08),
            foregroundColor: colorScheme.primary,
            side: BorderSide(
              color: colorScheme.primary.withValues(alpha: 0.15),
            ),
            elevation: 0,
            padding: padding,
            minimumSize: Size(0, minHeight),
            disabledBackgroundColor: isDark
                ? ValoraColors.neutral800
                : ValoraColors.neutral100,
            disabledForegroundColor: ValoraColors.neutral300,
            shape: RoundedRectangleBorder(borderRadius: borderRadius),
          ),
          child: childContent,
        );
        break;
      case ValoraButtonVariant.outline:
        button = OutlinedButton(
          onPressed: effectiveOnPressed != null
              ? () {
                  HapticFeedback.lightImpact();
                  effectiveOnPressed();
                }
              : null,
          style: OutlinedButton.styleFrom(
            foregroundColor: colorScheme.primary,
            side: BorderSide(
              color: _isHovered
                  ? colorScheme.primary
                  : colorScheme.outline.withValues(alpha: 0.5),
              width: 1.5,
            ),
            padding: padding,
            minimumSize: Size(0, minHeight),
            disabledForegroundColor: ValoraColors.neutral400,
            shape: RoundedRectangleBorder(borderRadius: borderRadius),
          ),
          child: childContent,
        );
        break;
      case ValoraButtonVariant.ghost:
        button = TextButton(
          onPressed: effectiveOnPressed != null
              ? () {
                  HapticFeedback.lightImpact();
                  effectiveOnPressed();
                }
              : null,
          style: TextButton.styleFrom(
            foregroundColor: isDark
                ? ValoraColors.neutral300
                : ValoraColors.neutral600,
            backgroundColor: _isHovered
                ? colorScheme.primary.withValues(alpha: 0.06)
                : Colors.transparent,
            padding: padding,
            minimumSize: Size(0, minHeight),
            shape: RoundedRectangleBorder(borderRadius: borderRadius),
          ),
          child: childContent,
        );
        break;
    }

    // Wrap with interactivity
    if (widget.onPressed != null && !widget.isLoading) {
      button = MouseRegion(
        onEnter: (_) => setState(() => _isHovered = true),
        onExit: (_) => setState(() {
          _isHovered = false;
          _isPressed = false;
        }),
        child: Listener(
          onPointerDown: (_) => setState(() => _isPressed = true),
          onPointerUp: (_) => setState(() => _isPressed = false),
          onPointerCancel: (_) => setState(() => _isPressed = false),
          child: button,
        ),
      );
    }

    // Press/hover animations
    return button
        .animate(target: _isPressed ? 1 : 0)
        .scale(
          end: const Offset(0.96, 0.96),
          duration: ValoraAnimations.fast,
          curve: ValoraAnimations.snappy,
        )
        .animate(target: _isHovered && !_isPressed ? 1 : 0)
        .scale(
          end: const Offset(1.02, 1.02),
          duration: ValoraAnimations.normal,
          curve: ValoraAnimations.smooth,
        );
  }
}
