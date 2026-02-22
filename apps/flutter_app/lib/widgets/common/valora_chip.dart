import 'package:flutter/material.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';

/// Selectable chip with premium surface treatment.
class ValoraChip extends StatefulWidget {
  const ValoraChip({
    super.key,
    required this.label,
    this.isSelected = false,
    this.onSelected,
    this.onDeleted,
    this.icon,
    this.selectedColor,
  });

  final String label;
  final bool isSelected;
  final ValueChanged<bool>? onSelected;
  final VoidCallback? onDeleted;
  final IconData? icon;
  final Color? selectedColor;

  @override
  State<ValoraChip> createState() => _ValoraChipState();
}

class _ValoraChipState extends State<ValoraChip> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final selectedColor = widget.selectedColor ?? ValoraColors.primary;

    final bgColor = widget.isSelected
        ? selectedColor.withValues(alpha: isDark ? 0.18 : 0.1)
        : (_isHovered
              ? (isDark
                    ? ValoraColors.neutral800.withValues(alpha: 0.6)
                    : ValoraColors.neutral100)
              : (isDark
                    ? ValoraColors.surfaceVariantDark.withValues(alpha: 0.5)
                    : ValoraColors.surfaceLight));

    final borderColor = widget.isSelected
        ? selectedColor.withValues(alpha: 0.4)
        : (isDark
              ? ValoraColors.neutral700.withValues(alpha: 0.4)
              : ValoraColors.neutral200.withValues(alpha: 0.8));

    final textColor = widget.isSelected
        ? selectedColor
        : (isDark ? ValoraColors.neutral200 : ValoraColors.neutral600);

    return MouseRegion(
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      cursor: SystemMouseCursors.click,
      child: GestureDetector(
        onTap: widget.onSelected != null
            ? () => widget.onSelected!(!widget.isSelected)
            : null,
        child: AnimatedContainer(
          duration: ValoraAnimations.normal,
          curve: ValoraAnimations.smooth,
          padding: const EdgeInsets.symmetric(
            horizontal: ValoraSpacing.md,
            vertical: ValoraSpacing.sm,
          ),
          decoration: BoxDecoration(
            color: bgColor,
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
            border: Border.all(color: borderColor, width: 1),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (widget.icon != null) ...[
                Icon(
                  widget.icon,
                  size: ValoraSpacing.iconSizeSm,
                  color: textColor,
                ),
                const SizedBox(width: ValoraSpacing.xs),
              ],
              Text(
                widget.label,
                style: ValoraTypography.labelMedium.copyWith(
                  color: textColor,
                  fontWeight: widget.isSelected
                      ? FontWeight.w600
                      : FontWeight.w500,
                ),
              ),
              if (widget.onDeleted != null) ...[
                const SizedBox(width: ValoraSpacing.xs),
                GestureDetector(
                  onTap: widget.onDeleted,
                  child: Icon(
                    Icons.close_rounded,
                    size: 14,
                    color: textColor.withValues(alpha: 0.6),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
