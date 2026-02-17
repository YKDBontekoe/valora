import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../providers/insights_provider.dart';

class InsightsMetricSelector extends StatelessWidget {
  const InsightsMetricSelector({super.key});

  @override
  Widget build(BuildContext context) {
    return Positioned(
      top: 92,
      left: 16,
      right: 16,
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Selector<InsightsProvider, InsightMetric>(
          selector: (_, p) => p.selectedMetric,
          builder: (context, selectedMetric, _) {
            return Row(
              children: InsightMetric.values.map((metric) {
                final isSelected = selectedMetric == metric;
                return Padding(
                  padding: const EdgeInsets.only(right: 8),
                  child: FilterChip(
                    label: Text(_getMetricLabel(metric)),
                    selected: isSelected,
                    onSelected:
                        (_) =>
                            context.read<InsightsProvider>().setMetric(metric),
                    checkmarkColor: ValoraColors.primaryDark,
                    side: BorderSide(
                      color:
                          isSelected
                              ? ValoraColors.primary
                              : ValoraColors.neutral300,
                    ),
                    backgroundColor: Colors.white.withValues(alpha: 0.88),
                    selectedColor: ValoraColors.primaryLight.withValues(
                      alpha: 0.25,
                    ),
                    shadowColor: Colors.black.withValues(alpha: 0.08),
                    elevation: 2,
                  ),
                );
              }).toList(),
            );
          },
        ),
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
