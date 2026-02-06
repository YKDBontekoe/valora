import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_animations.dart';

/// A premium choice chip with selection animations.
class ValoraChip extends StatelessWidget {
  const ValoraChip({
    super.key,
    required this.label,
    required this.isSelected,
    required this.onSelected,
  });

  final String label;
  final bool isSelected;
  final ValueChanged<bool> onSelected;

  @override
  Widget build(BuildContext context) {
    return AnimatedContainer(
      duration: ValoraAnimations.normal,
      curve: ValoraAnimations.standard,
      child: FilterChip(
        label: Text(label),
        selected: isSelected,
        onSelected: onSelected,
        checkmarkColor: isSelected ? Colors.white : null,
        labelStyle: ValoraTypography.labelMedium.copyWith(
          color: isSelected
              ? Colors.white
              : Theme.of(context).colorScheme.onSurface,
        ),
        backgroundColor: Theme.of(context).colorScheme.surface,
        selectedColor: ValoraColors.primary,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
          side: BorderSide(
            color: isSelected
                ? Colors.transparent
                : Theme.of(context).colorScheme.outline,
          ),
        ),
        showCheckmark: false,
        padding: const EdgeInsets.symmetric(
          horizontal: ValoraSpacing.sm,
          vertical: ValoraSpacing.xs,
        ),
      ),
    ).animate(target: isSelected ? 1 : 0)
    .scale(end: const Offset(1.05, 1.05), duration: ValoraAnimations.normal, curve: ValoraAnimations.emphatic)
    .then()
    .scale(end: const Offset(1.0, 1.0), duration: ValoraAnimations.normal);
  }
}
