import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../widgets/valora_widgets.dart';
import '../../../core/theme/valora_colors.dart';
import '../../../core/theme/valora_typography.dart';
import '../../../providers/context_report_provider.dart';

class RadiusSelector extends StatelessWidget {
  const RadiusSelector({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return ValoraCard(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 12),
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
                    size: 18,
                    color: isDark
                        ? ValoraColors.neutral400
                        : ValoraColors.neutral500,
                  ),
                  const SizedBox(width: 8),
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
                  return ValoraBadge(
                    label: '${radiusMeters}m',
                    color: theme.colorScheme.primary,
                    size: ValoraBadgeSize.small,
                  );
                },
              ),
            ],
          ),
          const SizedBox(height: 8),
          Selector<ContextReportProvider, int>(
            selector: (_, p) => p.radiusMeters,
            builder: (context, radiusMeters, _) {
              return ValoraSlider(
                min: 200,
                max: 5000,
                divisions: 24,
                value: radiusMeters.toDouble(),
                onChanged: (value) => context.read<ContextReportProvider>().setRadiusMeters(value.round()),
              );
            },
          ),
        ],
      ),
    );
  }
}
