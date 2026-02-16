import 'package:flutter/material.dart';
import '../../models/context_report.dart';
import '../valora_glass_container.dart';
import '../../core/theme/valora_colors.dart';

class SmartInsightsGrid extends StatelessWidget {
  const SmartInsightsGrid({super.key, required this.report});

  final ContextReport report;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    final insights = [
      _InsightData(
        title: 'Family Friendly',
        value: _calculateFamilyScore(),
        icon: Icons.family_restroom_rounded,
        color: Colors.blue,
        label: _getFamilyLabel(),
      ),
      _InsightData(
        title: 'Safety Level',
        value: report.categoryScores['Safety'] ?? 0,
        icon: Icons.shield_rounded,
        color: Colors.green,
        label: _getSafetyLabel(),
      ),
      _InsightData(
        title: 'Connectivity',
        value: report.categoryScores['Amenities'] ?? 0,
        icon: Icons.directions_bus_rounded,
        color: Colors.orange,
        label: _getConnectivityLabel(),
      ),
      _InsightData(
        title: 'Economic Class',
        value: _calculateEconomicScore(),
        icon: Icons.euro_rounded,
        color: Colors.purple,
        label: _getEconomicLabel(),
      ),
    ];

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        crossAxisSpacing: 16,
        mainAxisSpacing: 16,
        childAspectRatio: 1.4,
      ),
      itemCount: insights.length,
      itemBuilder: (context, index) {
        final insight = insights[index];
        return ValoraGlassContainer(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Icon(insight.icon, color: insight.color, size: 20),
                  Text(
                    '${insight.value.round()}%',
                    style: TextStyle(
                      color: insight.color,
                      fontWeight: FontWeight.bold,
                      fontSize: 12,
                    ),
                  ),
                ],
              ),
              const Spacer(),
              Text(
                insight.title,
                style: theme.textTheme.labelMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                  color: theme.colorScheme.onSurface,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                insight.label,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                  fontSize: 11,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        );
      },
    );
  }

  double _calculateFamilyScore() {
    final schoolScore = report.amenityMetrics.firstWhere((m) => m.key == 'dist_school', orElse: () => ContextMetric(key: '', label: '', source: '')).score ?? 70;
    final familyMetric = report.demographicsMetrics.firstWhere((m) => m.key == 'households_with_children', orElse: () => ContextMetric(key: '', label: '', source: '')).score ?? 60;
    return (schoolScore + familyMetric) / 2;
  }

  String _getFamilyLabel() {
    final score = _calculateFamilyScore();
    if (score > 80) return 'Excellent for kids';
    if (score > 60) return 'Very family friendly';
    return 'Typical neighborhood';
  }

  String _getSafetyLabel() {
    final score = report.categoryScores['Safety'] ?? 0;
    if (score > 80) return 'Very safe area';
    if (score > 60) return 'Safe neighborhood';
    return 'Moderate safety';
  }

  String _getConnectivityLabel() {
    final score = report.categoryScores['Amenities'] ?? 0;
    if (score > 80) return 'Excellent transit';
    if (score > 60) return 'Well connected';
    return 'Car dependent';
  }

  double _calculateEconomicScore() {
    final incomeScore = report.demographicsMetrics.firstWhere((m) => m.key == 'avg_income_inhabitant', orElse: () => ContextMetric(key: '', label: '', source: '')).score ?? 50;
    final wozScore = report.demographicsMetrics.firstWhere((m) => m.key == 'average_woz', orElse: () => ContextMetric(key: '', label: '', source: '')).score ?? 50;
    return (incomeScore + wozScore) / 2;
  }

  String _getEconomicLabel() {
    final score = _calculateEconomicScore();
    if (score > 80) return 'Affluent area';
    if (score > 60) return 'Upper middle class';
    return 'Balanced economy';
  }
}

class _InsightData {
  final String title;
  final double value;
  final IconData icon;
  final Color color;
  final String label;

  _InsightData({
    required this.title,
    required this.value,
    required this.icon,
    required this.color,
    required this.label,
  });
}
