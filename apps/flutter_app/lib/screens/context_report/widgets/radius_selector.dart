import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../core/theme/valora_colors.dart';
import '../../../core/theme/valora_spacing.dart';
import '../../../core/theme/valora_typography.dart';
import '../../../providers/context_report_provider.dart';

class RadiusSelector extends StatelessWidget {
  const RadiusSelector({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return Container(
      decoration: BoxDecoration(
        boxShadow: [
          BoxShadow(
            color: Theme.of(context).colorScheme.shadow.withValues(alpha: 0.05),
            blurRadius: ValoraSpacing.md,
            offset: const Offset(0, 2),
          )
        ],
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusMd),
      ),
      child: ValoraCard(
        padding: const EdgeInsets.fromLTRB(ValoraSpacing.lg, ValoraSpacing.md, ValoraSpacing.lg, ValoraSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    Icon(
                      Icons.radar_rounded,
                      size: ValoraSpacing.iconSizeSm,
                      color: isDark
                          ? ValoraColors.neutral400
                          : ValoraColors.neutral500,
                    ),
                    const SizedBox(width: ValoraSpacing.sm),
                    Text(
                      'Analysis Radius',
                      style: ValoraTypography.labelLarge
                          .copyWith(fontWeight: FontWeight.w600),
                    ),
                  ],
                ),
                Selector<ContextReportProvider, int>(
                  selector: (_, p) => p.radiusMeters,
                  builder: (context, radiusMeters, _) {
                    return TweenAnimationBuilder<double>(
                      key: ValueKey<int>(radiusMeters),
                      tween: Tween<double>(begin: 0.8, end: 1.0),
                      duration: const Duration(milliseconds: 300),
                      curve: Curves.elasticOut,
                      builder: (context, scale, child) {
                        return Transform.scale(
                          scale: scale,
                          child: child,
                        );
                      },
                      child: ValoraBadge(
                        label: '${radiusMeters}m',
                        color: theme.colorScheme.primary,
                        size: ValoraBadgeSize.small,
                      ),
                    );
                  },
                ),
              ],
            ),
            const SizedBox(height: ValoraSpacing.sm),
            Selector<ContextReportProvider, int>(
              selector: (_, p) => p.radiusMeters,
              builder: (context, radiusMeters, _) {
                return MouseRegion(
                  cursor: SystemMouseCursors.click,
                  child: ValoraSlider(
                    min: 200,
                    max: 5000,
                    divisions: 24,
                    value: radiusMeters.toDouble(),
                    onChanged: (value) => context.read<ContextReportProvider>().setRadiusMeters(value.round()),
                  ),
                );
              },
            ),
          ],
        ),
      ),
    );
  }
}
