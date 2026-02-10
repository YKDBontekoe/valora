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
    this.isSelected = false,
    this.onSelected,
    this.icon,
    this.backgroundColor,
    this.textColor,
    this.onDeleted,
  });

  final String label;
  final bool isSelected;
  final ValueChanged<bool>? onSelected;
  final IconData? icon;
  final Color? backgroundColor;
  final Color? textColor;
  final VoidCallback? onDeleted;

  @override
  State<ValoraChip> createState() => _ValoraChipState();
}

class _ValoraChipState extends State<ValoraChip> {
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

    return ValueListenableBuilder<bool>(
      valueListenable: _isHovered,
      builder: (context, isHovered, _) {
        return ValueListenableBuilder<bool>(
          valueListenable: _isPressed,
          builder: (context, isPressed, _) {
            // Determine colors
            final bg = widget.isSelected
                ? (widget.backgroundColor ?? ValoraColors.primary)
                : (widget.backgroundColor ??
                    (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight));

            final text = widget.isSelected
                ? (widget.textColor ?? Colors.white)
                : (widget.textColor ??
                    (isDark ? ValoraColors.neutral400 : ValoraColors.neutral500));

            final border = widget.isSelected
                ? Colors.transparent
                : (isDark ? ValoraColors.neutral700 : ValoraColors.neutral200);

            // Shadow logic
            final List<BoxShadow> shadows = widget.isSelected
                ? ValoraShadows.primary
                : (isHovered && widget.onSelected != null
                    ? ValoraShadows.sm
                    : const <BoxShadow>[]);

            Widget content = AnimatedContainer(
              duration: ValoraAnimations.normal,
              curve: ValoraAnimations.standard,
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
                boxShadow: shadows,
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
                  onHover: (bool val) => _isHovered.value = val,
                  onTapDown: (TapDownDetails _) => _isPressed.value = true,
                  onTapUp: (TapUpDetails _) => _isPressed.value = false,
                  onTapCancel: () => _isPressed.value = false,
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
                        if (widget.onDeleted != null) ...[
                          const SizedBox(width: ValoraSpacing.xs),
                          InkWell(
                            onTap: () {
                              HapticFeedback.lightImpact();
                              widget.onDeleted!();
                            },
                            borderRadius: BorderRadius.circular(12),
                            child: Icon(
                              Icons.close_rounded,
                              size: 16,
                              color: text.withValues(alpha: 0.8),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                ),
              ),
            );

            // Animate scale
            return content
                .animate(target: isPressed ? 1 : 0)
                .scale(
                  end: const Offset(0.95, 0.95),
                  duration: ValoraAnimations.fast,
                )
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
          },
        );
      },
    );
  }
}
