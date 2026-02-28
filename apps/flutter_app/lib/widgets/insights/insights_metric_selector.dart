import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_shadows.dart';
import '../../providers/insights_provider.dart';

class InsightsMetricSelector extends StatelessWidget {
  const InsightsMetricSelector({super.key});

  @override
  Widget build(BuildContext context) {
    return Positioned(
      top: 90,
      left: 12,
      right: 12,
      child: Selector<InsightsProvider, InsightMetric>(
        selector: (_, p) => p.selectedMetric,
        builder: (context, selectedMetric, _) {
          final isDark = Theme.of(context).brightness == Brightness.dark;
          return Container(
            height: 48,
            decoration: BoxDecoration(
              color: isDark ? ValoraColors.glassBlackStrong : ValoraColors.glassWhiteStrong,
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200),
              boxShadow: isDark ? ValoraShadows.smDark : ValoraShadows.sm,
            ),
            child: Padding(
              padding: const EdgeInsets.all(4),
              child: Row(
                children: InsightMetric.values.map((metric) {
                  final isSelected = selectedMetric == metric;
                  return Expanded(
                    child: Semantics(
                      button: true,
                      enabled: true,
                      selected: isSelected,
                      label: _getMetricLabel(metric),
                      child: Material(
                        color: Colors.transparent,
                        child: InkWell(
                          onTap: () => context.read<InsightsProvider>().setMetric(metric),
                          borderRadius: BorderRadius.circular(10),
                          child: AnimatedContainer(
                            duration: const Duration(milliseconds: 220),
                            curve: Curves.easeInOut,
                            decoration: BoxDecoration(
                              color: isSelected ? ValoraColors.primary : Colors.transparent,
                              borderRadius: BorderRadius.circular(10),
                              boxShadow: isSelected ? [
                                BoxShadow(
                                  color: ValoraColors.primary.withValues(alpha: 0.30),
                                  blurRadius: 8,
                                  offset: const Offset(0, 2),
                                ),
                              ] : null,
                            ),
                            child: Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Icon(
                                  _getMetricIcon(metric),
                                  size: 13,
                                  color: isSelected
                                      ? Colors.white
                                      : (isDark ? ValoraColors.neutral400 : ValoraColors.neutral500),
                                ),
                                const SizedBox(width: 4),
                                AnimatedDefaultTextStyle(
                                  duration: const Duration(milliseconds: 220),
                                  style: TextStyle(
                                    fontSize: 12,
                                    fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
                                    color: isSelected
                                        ? Colors.white
                                        : (isDark ? ValoraColors.neutral400 : ValoraColors.neutral500),
                                  ),
                                  child: Text(_getMetricLabel(metric)),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ),
                    ),
                  );
                }).toList(),
              ),
            ),
          );
        },
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
