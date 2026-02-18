import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_shadows.dart';
import '../../providers/insights_provider.dart';

class InsightsLegend extends StatelessWidget {
  const InsightsLegend({super.key});

  @override
  Widget build(BuildContext context) {
    return Selector<InsightsProvider, InsightMetric>(
      selector: (_, p) => p.selectedMetric,
      builder: (context, metric, _) {
        final isDark = Theme.of(context).brightness == Brightness.dark;
        return Positioned(
          left: 16,
          bottom: 24,
          child: Container(
            key: const Key('insights_map_legend'),
            width: 168,
            padding: const EdgeInsets.fromLTRB(12, 10, 12, 10),
            decoration: BoxDecoration(
              color: isDark ? ValoraColors.glassBlackStrong : ValoraColors.glassWhiteStrong,
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200),
              boxShadow: isDark ? ValoraShadows.mdDark : ValoraShadows.md,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${_getMetricLabel(metric)} score',
                  style: Theme.of(context).textTheme.labelLarge?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 8),
                _buildLegendRow(context, '80+', ValoraColors.success),
                _buildLegendRow(context, '60-79', ValoraColors.warning),
                _buildLegendRow(context, '40-59', Colors.orange),
                _buildLegendRow(context, '<40', ValoraColors.error),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildLegendRow(BuildContext context, String label, Color color) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 5),
      child: Row(
        children: [
          Container(
            width: 9,
            height: 9,
            decoration: BoxDecoration(color: color, shape: BoxShape.circle),
          ),
          const SizedBox(width: 8),
          Text(
            label,
            style: TextStyle(
              fontSize: 12,
              color: Theme.of(context).textTheme.bodySmall?.color,
              fontWeight: FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }

  String _getMetricLabel(InsightMetric metric) {
    switch (metric) {
      case InsightMetric.composite:
        return 'Overall';
      case InsightMetric.safety:
        return 'Safety';
      case InsightMetric.social:
        return 'Social';
      case InsightMetric.amenities:
        return 'Amenities';
    }
  }
}
