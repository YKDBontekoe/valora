import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';
import '../../core/theme/valora_shadows.dart';

/// A premium choice chip with selection animations.
class ValoraChip extends StatefulWidget {
  const ValoraChip({
    super.key,
    required this.label,
    required this.isSelected,
    required this.onSelected,
    this.icon,
    this.backgroundColor,
    this.textColor,
  });

  final String label;
  final bool isSelected;
  final ValueChanged<bool>? onSelected;
  final IconData? icon;
  final Color? backgroundColor;
  final Color? textColor;

  @override
  State<ValoraChip> createState() => _ValoraChipState();
}

class _ValoraChipState extends State<ValoraChip> {
  bool _isHovered = false;
  bool _isPressed = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    // Determine colors
    final bg = widget.isSelected
        ? (widget.backgroundColor ?? ValoraColors.primary)
        : (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight);

    final text = widget.isSelected
        ? (widget.textColor ?? Colors.white)
        : (isDark ? ValoraColors.neutral400 : ValoraColors.neutral500);

    final border = widget.isSelected
        ? Colors.transparent
        : (isDark ? ValoraColors.neutral700 : ValoraColors.neutral200);

    return MouseRegion(
          onEnter: (_) => setState(() => _isHovered = true),
          onExit: (_) => setState(() {
            _isHovered = false;
            _isPressed = false;
          }),
          child: Listener(
            onPointerDown: (_) => setState(() => _isPressed = true),
            onPointerUp: (_) => setState(() => _isPressed = false),
            onPointerCancel: (_) => setState(() => _isPressed = false),
            child: AnimatedContainer(
              duration: ValoraAnimations.normal,
              curve: ValoraAnimations.standard,
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
                boxShadow: widget.isSelected
                    ? ValoraShadows.primary
                    : (_isHovered ? ValoraShadows.sm : []),
              ),
              child: Material(
                color: bg,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
                  side: BorderSide(color: border),
                ),
                child: InkWell(
                  onTap: widget.onSelected != null
                      ? () {
                          HapticFeedback.selectionClick();
                          widget.onSelected!(!widget.isSelected);
                        }
                      : null,
                  borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: ValoraSpacing.md,
                      vertical: ValoraSpacing.sm,
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        if (widget.icon != null) ...[
                          Icon(widget.icon, size: 16, color: text),
                          const SizedBox(width: ValoraSpacing.xs),
                        ],
                        Text(
                          widget.label,
                          style: ValoraTypography.labelSmall.copyWith(
                            color: text,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        )
        .animate(target: _isPressed ? 1 : 0)
        .scale(end: const Offset(0.95, 0.95), duration: ValoraAnimations.fast)
        .animate(target: widget.isSelected ? 1 : 0) // Pulse on selection
        .scale(
          end: const Offset(1.05, 1.05),
          duration: ValoraAnimations.normal,
          curve: ValoraAnimations.emphatic,
        )
        .then()
        .scale(
          begin: const Offset(1.05, 1.05),
          end: const Offset(1.0, 1.0),
          duration: ValoraAnimations.normal,
        );
  }
}
