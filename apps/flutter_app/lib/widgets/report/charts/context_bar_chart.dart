import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../../../models/context_report.dart';

class ContextBarChart extends StatelessWidget {
  const ContextBarChart({
    super.key,
    required this.metrics,
    this.height = 180,
    this.barColor,
    this.showLabels = true,
  });

  final List<ContextMetric> metrics;
  final double height;
  final Color? barColor;
  final bool showLabels;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final color = barColor ?? theme.colorScheme.primary;

    if (metrics.isEmpty) return const SizedBox.shrink();

    final maxVal = metrics.fold<double>(0, (max, e) => (e.value ?? 0) > max ? (e.value ?? 0) : max);

    return SizedBox(
      height: height,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: metrics.asMap().entries.map((entry) {
          final index = entry.key;
          final metric = entry.value;
          final value = metric.value ?? 0;
          final percentage = maxVal > 0 ? value / maxVal : 0.0;

          return Expanded(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 6),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  Text(
                    value.toStringAsFixed(0),
                    style: theme.textTheme.labelSmall?.copyWith(
                      fontWeight: FontWeight.bold,
                      fontSize: 10,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Flexible(
                    child: FractionallySizedBox(
                      heightFactor: percentage.clamp(0.05, 1.0),
                      child: Container(
                        decoration: BoxDecoration(
                          gradient: LinearGradient(
                            begin: Alignment.topCenter,
                            end: Alignment.bottomCenter,
                            colors: [
                              color,
                              color.withValues(alpha: 0.6),
                            ],
                          ),
                          borderRadius: const BorderRadius.vertical(top: Radius.circular(6)),
                        ),
                      ),
                    ).animate().scaleY(
                      begin: 0,
                      alignment: Alignment.bottomCenter,
                      duration: 600.ms,
                      curve: Curves.easeOutBack,
                      delay: (index * 50).ms,
                    ),
                  ),
                  if (showLabels) ...[
                    const SizedBox(height: 8),
                    Transform.rotate(
                      angle: -0.5,
                      child: Text(
                        _shortenLabel(metric.label),
                        style: theme.textTheme.bodySmall?.copyWith(fontSize: 9),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ],
              ),
            ),
          );
        }).toList(),
      ),
    );
  }

  String _shortenLabel(String label) {
    return label.replaceAll('years', 'y').replaceAll('Plus', '+');
  }
}
