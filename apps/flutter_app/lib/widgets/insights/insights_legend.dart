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
          left: 12,
          bottom: 20,
          child: Container(
            key: const Key('insights_map_legend'),
            width: 200,
            padding: const EdgeInsets.fromLTRB(12, 11, 12, 12),
            decoration: BoxDecoration(
              color: isDark ? ValoraColors.glassBlackStrong : ValoraColors.glassWhiteStrong,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200),
              boxShadow: isDark ? ValoraShadows.mdDark : ValoraShadows.md,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Icon(
                      _getMetricIcon(metric),
                      size: 12,
                      color: isDark ? ValoraColors.neutral400 : ValoraColors.neutral500,
                    ),
                    const SizedBox(width: 5),
                    Text(
                      '${_getMetricLabel(metric)} score',
                      style: Theme.of(context).textTheme.labelSmall?.copyWith(
                        fontWeight: FontWeight.w700,
                        letterSpacing: 0.2,
                        color: isDark ? ValoraColors.neutral300 : ValoraColors.neutral600,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                // Gradient bar
                ClipRRect(
                  borderRadius: BorderRadius.circular(4),
                  child: Container(
                    height: 6,
                    decoration: const BoxDecoration(
                      gradient: LinearGradient(
                        colors: [
                          ValoraColors.error,
                          Colors.orange,
                          ValoraColors.warning,
                          ValoraColors.success,
                        ],
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 6),
                // Min/Max labels
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      '0',
                      style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.w600,
                        color: ValoraColors.error,
                      ),
                    ),
                    Text(
                      '100',
                      style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.w600,
                        color: ValoraColors.success,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 10),
                Divider(
                  height: 1,
                  color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200,
                ),
                const SizedBox(height: 8),
                _buildLegendRow(context, '80+  Excellent', ValoraColors.success),
                _buildLegendRow(context, '60–79  Good', ValoraColors.warning),
                _buildLegendRow(context, '40–59  Fair', Colors.orange),
                _buildLegendRow(context, '<40   Poor', ValoraColors.error),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildLegendRow(BuildContext context, String label, Color color) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 4),
      child: Row(
        children: [
          Container(
            width: 8,
            height: 8,
            decoration: BoxDecoration(color: color, shape: BoxShape.circle),
          ),
          const SizedBox(width: 7),
          Text(
            label,
            style: TextStyle(
              fontSize: 11,
              color: Theme.of(context).textTheme.bodySmall?.color,
              fontWeight: FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }

  IconData _getMetricIcon(InsightMetric metric) {
    switch (metric) {
      case InsightMetric.composite:
        return Icons.auto_awesome_rounded;
      case InsightMetric.safety:
        return Icons.shield_rounded;
      case InsightMetric.social:
        return Icons.people_rounded;
      case InsightMetric.amenities:
        return Icons.storefront_rounded;
    }
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
