import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../../models/context_report.dart';

class ProximityChart extends StatelessWidget {
  const ProximityChart({super.key, required this.metrics});

  final List<ContextMetric> metrics;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    // Sort by distance (value) ascending
    final sortedMetrics = List<ContextMetric>.from(metrics)
      ..sort((a, b) => (a.value ?? 0).compareTo(b.value ?? 0));

    // Limit to top 6 closest
    final topMetrics = sortedMetrics.take(6).toList();

    return Column(
      children: topMetrics.asMap().entries.map((entry) {
        final index = entry.key;
        final metric = entry.value;
        final distance = metric.value ?? 0;

        // Normalize distance for bar width (max 5km)
        const double maxDistance = 5.0;
        final double percentage = (distance / maxDistance).clamp(0.05, 1.0);

        Color color;
        if (distance < 1.0) {
          color = Colors.green;
        } else if (distance < 3.0) {
          color = Colors.orange;
        } else {
          color = Colors.red;
        }

        return Padding(
          padding: const EdgeInsets.symmetric(vertical: 8),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    metric.label,
                    style: theme.textTheme.bodySmall?.copyWith(
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                  Text(
                    '${distance.toStringAsFixed(1)} km',
                    style: theme.textTheme.labelSmall?.copyWith(
                      color: color,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              Stack(
                children: [
                  Container(
                    height: 8,
                    width: double.infinity,
                    decoration: BoxDecoration(
                      color: theme.colorScheme.surfaceContainerHighest,
                      borderRadius: BorderRadius.circular(4),
                    ),
                  ),
                  FractionallySizedBox(
                    widthFactor: percentage,
                    child: Container(
                      height: 8,
                      decoration: BoxDecoration(
                        color: color.withValues(alpha: 0.8),
                        borderRadius: BorderRadius.circular(4),
                      ),
                    ),
                  ).animate().scaleX(
                    begin: 0,
                    alignment: Alignment.centerLeft,
                    duration: 600.ms,
                    curve: Curves.easeOutQuart,
                    delay: (index * 100).ms,
                  ),
                ],
              ),
            ],
          ),
        );
      }).toList(),
    );
  }
}
