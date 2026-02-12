import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';

class ValoraTag extends StatefulWidget {
  const ValoraTag({
    super.key,
    required this.label,
    this.icon,
    this.backgroundColor,
    this.textColor,
    this.borderColor,
    this.onTap,
  });

  final String label;
  final IconData? icon;
  final Color? backgroundColor;
  final Color? textColor;
  final Color? borderColor;
  final VoidCallback? onTap;

  @override
  State<ValoraTag> createState() => _ValoraTagState();
}

class _ValoraTagState extends State<ValoraTag> {
  bool _isHovered = false;
  bool _isPressed = false;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final bg = widget.backgroundColor ??
        colorScheme.secondaryContainer.withValues(alpha: 0.5);
    final text = widget.textColor ?? colorScheme.onSecondaryContainer;
    final border = widget.borderColor ??
        colorScheme.outlineVariant.withValues(alpha: 0.3);

    final container = AnimatedContainer(
      duration: ValoraAnimations.fast,
      padding: const EdgeInsets.symmetric(
        horizontal: ValoraSpacing.md - 4,
        vertical: ValoraSpacing.sm,
      ),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
        border: Border.all(color: border),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (widget.icon != null) ...[
            Icon(widget.icon, size: ValoraSpacing.iconSizeSm, color: text),
            const SizedBox(width: ValoraSpacing.sm),
          ],
          Text(
            widget.label,
            style: ValoraTypography.labelLarge.copyWith(color: text),
          ),
        ],
      ),
    );

    if (widget.onTap == null) {
      return container;
    }

    return MouseRegion(
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() {
        _isHovered = false;
        _isPressed = false;
      }),
      child: GestureDetector(
        onTapDown: (_) => setState(() => _isPressed = true),
        onTapUp: (_) => setState(() => _isPressed = false),
        onTapCancel: () => setState(() => _isPressed = false),
        onTap: widget.onTap,
        child: container
            .animate(target: _isPressed ? 1 : 0)
            .scale(
              end: const Offset(0.95, 0.95),
              duration: ValoraAnimations.fast,
            )
            .animate(target: _isHovered && !_isPressed ? 1 : 0)
            .scale(
              end: const Offset(1.05, 1.05),
              duration: ValoraAnimations.normal,
              curve: ValoraAnimations.standard,
            ),
      ),
    );
  }
}
