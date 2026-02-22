import 'package:flutter/material.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';

class ValoraSectionHeader extends StatelessWidget {
  const ValoraSectionHeader({
    super.key,
    required this.title,
    this.color,
  });

  final String title;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final effectiveColor = color ??
        (isDark ? ValoraColors.neutral400 : ValoraColors.neutral500);

    return Align(
      alignment: Alignment.centerLeft,
      child: Padding(
        padding: const EdgeInsets.only(
          left: ValoraSpacing.xs,
          bottom: ValoraSpacing.sm,
          top: ValoraSpacing.lg,
        ),
        child: Text(
          title.toUpperCase(),
          style: ValoraTypography.labelSmall.copyWith(
            color: effectiveColor,
            fontWeight: FontWeight.w700,
            letterSpacing: 1.0,
          ),
        ),
      ),
    );
  }
}
