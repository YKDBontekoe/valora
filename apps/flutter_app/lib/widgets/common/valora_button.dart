import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_animations.dart';

/// Button component with multiple variants.
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

  @override
  State<ValoraButton> createState() => _ValoraButtonState();
}

class _ValoraButtonState extends State<ValoraButton> {
  bool _isHovered = false;
  bool _isPressed = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    Widget child = AnimatedSwitcher(
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
                valueColor: AlwaysStoppedAnimation(
                  _getLoadingColor(isDark),
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
                  Icon(widget.icon, size: ValoraSpacing.iconSizeSm + 2),
                  const SizedBox(width: ValoraSpacing.sm),
                ],
                Text(
                  widget.label,
                  style: const TextStyle(fontWeight: FontWeight.w600),
                ),
              ],
            ),
    );

    final effectiveOnPressed = widget.isLoading ? null : widget.onPressed;

    ButtonStyle style = _getButtonStyle(isDark);

    Widget button;
    switch (widget.variant) {
      case ValoraButtonVariant.primary:
        button = ElevatedButton(
          onPressed: effectiveOnPressed,
          style: style,
          child: child,
        );
        break;
      case ValoraButtonVariant.secondary:
        button = ElevatedButton(
          onPressed: effectiveOnPressed,
          style: style,
          child: child,
        );
        break;
      case ValoraButtonVariant.outline:
        button = OutlinedButton(
          onPressed: effectiveOnPressed,
          style: style,
          child: child,
        );
        break;
      case ValoraButtonVariant.ghost:
        button = TextButton(
          onPressed: effectiveOnPressed,
          style: style,
          child: child,
        );
        break;
    }

    if (effectiveOnPressed != null) {
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

    return button
        .animate(target: _isPressed ? 1 : 0)
        .scale(
          end: const Offset(0.95, 0.95),
          duration: ValoraAnimations.fast,
          curve: ValoraAnimations.standard,
        )
        .animate(target: _isHovered && !_isPressed ? 1 : 0)
        .scale(
          end: const Offset(1.02, 1.02),
          duration: ValoraAnimations.normal,
          curve: ValoraAnimations.standard,
        );
  }

  Color _getLoadingColor(bool isDark) {
    if (widget.variant == ValoraButtonVariant.primary) {
      return ValoraColors.surfaceLight;
    }
    return ValoraColors.primary;
  }

  ButtonStyle _getButtonStyle(bool isDark) {
    switch (widget.variant) {
      case ValoraButtonVariant.primary:
        return ElevatedButton.styleFrom(
          backgroundColor: ValoraColors.primary,
          foregroundColor: ValoraColors.surfaceLight,
          disabledBackgroundColor: ValoraColors.primary.withValues(alpha: 0.5),
          disabledForegroundColor: ValoraColors.surfaceLight.withValues(alpha: 0.7),
          elevation: _isHovered ? 4 : 2,
          padding: const EdgeInsets.symmetric(
            horizontal: ValoraSpacing.lg,
            vertical: ValoraSpacing.md,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
          ),
        );
      case ValoraButtonVariant.secondary:
        return ElevatedButton.styleFrom(
          backgroundColor: isDark ? ValoraColors.neutral700 : ValoraColors.neutral100,
          foregroundColor: isDark ? ValoraColors.neutral100 : ValoraColors.neutral900,
          elevation: 0,
          padding: const EdgeInsets.symmetric(
            horizontal: ValoraSpacing.lg,
            vertical: ValoraSpacing.md,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
          ),
        );
      case ValoraButtonVariant.outline:
        return OutlinedButton.styleFrom(
          foregroundColor: isDark ? ValoraColors.neutral100 : ValoraColors.neutral900,
          side: BorderSide(
            color: isDark ? ValoraColors.neutral600 : ValoraColors.neutral300,
          ),
          padding: const EdgeInsets.symmetric(
            horizontal: ValoraSpacing.lg,
            vertical: ValoraSpacing.md,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
          ),
        );
      case ValoraButtonVariant.ghost:
        return TextButton.styleFrom(
          foregroundColor: isDark ? ValoraColors.neutral100 : ValoraColors.neutral900,
          padding: const EdgeInsets.symmetric(
            horizontal: ValoraSpacing.md,
            vertical: ValoraSpacing.sm,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          ),
        );
    }
  }
}
