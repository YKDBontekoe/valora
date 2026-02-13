import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_animations.dart';
import '../../core/theme/valora_typography.dart';

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
    final colorScheme = Theme.of(context).colorScheme;

    // 1. Build the child content (Loading indicator OR Icon + Text)
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
                  Icon(widget.icon, size: ValoraSpacing.iconSizeSm + 2),
                  const SizedBox(width: ValoraSpacing.sm),
                ],
                Text(
                  widget.label,
                  style: ValoraTypography.labelLarge.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
    );

    // 2. Determine the button widget based on variant
    final effectiveOnPressed = widget.isLoading ? null : widget.onPressed;
    Widget button;

    switch (widget.variant) {
      case ValoraButtonVariant.primary:
        button = ElevatedButton(
          onPressed: effectiveOnPressed,
          style: ElevatedButton.styleFrom(
            backgroundColor: colorScheme.primary,
            foregroundColor: colorScheme.onPrimary,
            disabledBackgroundColor: ValoraColors.neutral200,
            disabledForegroundColor: ValoraColors.neutral400,
            elevation: _isHovered ? ValoraSpacing.elevationMd : ValoraSpacing.elevationNone,
            shadowColor: colorScheme.primary.withValues(alpha: 0.4),
          ),
          child: childContent,
        );
        break;
      case ValoraButtonVariant.secondary:
        button = ElevatedButton(
          onPressed: effectiveOnPressed,
          style: ElevatedButton.styleFrom(
            backgroundColor: colorScheme.primary.withValues(alpha: 0.1),
            foregroundColor: colorScheme.primary,
            elevation: 0,
            disabledBackgroundColor: ValoraColors.neutral100,
            disabledForegroundColor: ValoraColors.neutral300,
          ),
          child: childContent,
        );
        break;
      case ValoraButtonVariant.outline:
        button = OutlinedButton(
          onPressed: effectiveOnPressed,
          style: OutlinedButton.styleFrom(
            foregroundColor: colorScheme.primary,
            side: BorderSide(color: colorScheme.primary),
            disabledForegroundColor: ValoraColors.neutral400,
          ),
          child: childContent,
        );
        break;
      case ValoraButtonVariant.ghost:
        button = TextButton(
          onPressed: effectiveOnPressed,
          style: TextButton.styleFrom(
            foregroundColor: ValoraColors.neutral600,
          ),
          child: childContent,
        );
        break;
    }

    // 3. Wrap with interactivity (Hover/Press)
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

    // 4. Add animations
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
}
