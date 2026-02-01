import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
import '../core/theme/valora_typography.dart';
import '../models/filter_chip_model.dart';

class ValoraFilterChipGroup extends StatelessWidget {
  final List<ValoraFilterChipModel> chips;
  final ValueChanged<ValoraFilterChipModel> onTap;

  const ValoraFilterChipGroup({
    super.key,
    required this.chips,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    if (chips.isEmpty) {
      return const SizedBox.shrink();
    }

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      clipBehavior: Clip.none,
      child: Row(
        children: chips
            .map((chip) => _buildFilterChip(context, chip))
            .expand((widget) => [widget, const SizedBox(width: ValoraSpacing.xs)])
            .toList()
          ..removeLast(),
      ),
    );
  }

  Widget _buildFilterChip(BuildContext context, ValoraFilterChipModel chip) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final bool isActive = chip.isActive;
    final Color backgroundColor = isActive
        ? ValoraColors.primary
        : (isDark ? ValoraColors.surfaceDark : ValoraColors.surfaceLight);
    final Color textColor = isActive
        ? Colors.white
        : (isDark ? ValoraColors.neutral400 : ValoraColors.neutral500);
    final Color borderColor = isActive
        ? Colors.transparent
        : (isDark ? ValoraColors.neutral700 : ValoraColors.neutral200);

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () {
          HapticFeedback.selectionClick();
          onTap(chip);
        },
        borderRadius: BorderRadius.circular(100),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          decoration: BoxDecoration(
            color: backgroundColor,
            borderRadius: BorderRadius.circular(100),
            border: Border.all(color: borderColor),
            boxShadow: isActive
                ? [
                    BoxShadow(
                      color: ValoraColors.primary.withValues(alpha: 0.25),
                      blurRadius: 8,
                      offset: const Offset(0, 4),
                    ),
                  ]
                : [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.02),
                      blurRadius: 2,
                      offset: const Offset(0, 1),
                    ),
                  ],
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (chip.icon != null) ...[
                Icon(chip.icon, size: 16, color: textColor),
                const SizedBox(width: 6),
              ],
              Text(
                chip.label,
                style: ValoraTypography.labelSmall.copyWith(
                  color: textColor,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
