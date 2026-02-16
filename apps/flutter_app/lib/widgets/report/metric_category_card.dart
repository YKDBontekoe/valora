import 'package:flutter/material.dart';
import '../../models/context_report.dart';
import 'charts/context_bar_chart.dart';
import 'charts/context_pie_chart.dart';
import 'charts/proximity_chart.dart';

/// A collapsible card displaying metrics for a category with premium styling and charts.
class MetricCategoryCard extends StatefulWidget {
  const MetricCategoryCard({
    super.key,
    required this.title,
    required this.icon,
    required this.metrics,
    required this.score,
    this.accentColor,
    this.isExpanded = false,
    this.onToggle,
  });

  final String title;
  final IconData icon;
  final List<ContextMetric> metrics;
  final double? score;
  final Color? accentColor;
  final bool isExpanded;
  final ValueChanged<bool>? onToggle;

  @override
  State<MetricCategoryCard> createState() => _MetricCategoryCardState();
}

class _MetricCategoryCardState extends State<MetricCategoryCard>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _expandAnimation;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 300),
    );
    _expandAnimation = CurvedAnimation(
      parent: _controller,
      curve: Curves.fastOutSlowIn,
    );
    if (widget.isExpanded) _controller.value = 1.0;
  }

  @override
  void didUpdateWidget(MetricCategoryCard oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.isExpanded != oldWidget.isExpanded) {
      if (widget.isExpanded) {
        _controller.forward();
      } else {
        _controller.reverse();
      }
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Widget? _buildChart() {
    if (widget.title == 'Demographics') {
      final ageMetrics = widget.metrics.where((m) => m.key.startsWith('age_')).toList();
      if (ageMetrics.isNotEmpty) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Age Distribution', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
            const SizedBox(height: 12),
            ContextBarChart(metrics: ageMetrics, height: 120),
            const SizedBox(height: 24),
          ],
        );
      }
    }

    if (widget.title == 'Housing') {
      final housingTypeMetrics = widget.metrics.where((m) => m.key.startsWith('housing_')).toList();
      if (housingTypeMetrics.isNotEmpty) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Housing Profile', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
            const SizedBox(height: 12),
            ContextPieChart(metrics: housingTypeMetrics, size: 140),
            const SizedBox(height: 24),
          ],
        );
      }
    }

    if (widget.title == 'Amenities') {
      final distMetrics = widget.metrics.where((m) => m.key.startsWith('dist_')).toList();
      if (distMetrics.isNotEmpty) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Proximity to Amenities', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
            const SizedBox(height: 12),
            ProximityChart(metrics: distMetrics),
            const SizedBox(height: 24),
          ],
        );
      }
    }

    return null;
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final accentColor = widget.accentColor ?? theme.colorScheme.primary;

    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(20),
        color: theme.colorScheme.surface,
        border: Border.all(
          color: theme.colorScheme.outlineVariant.withValues(alpha: 0.5),
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.03),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        children: [
          // Header
          InkWell(
            onTap: () => widget.onToggle?.call(!widget.isExpanded),
            borderRadius: BorderRadius.circular(20),
            child: Padding(
              padding: const EdgeInsets.all(18),
              child: Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: accentColor.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(14),
                    ),
                    child: Icon(widget.icon, color: accentColor, size: 24),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          widget.title,
                          style: theme.textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        if (widget.score != null) ...[
                          const SizedBox(height: 4),
                          Row(
                            children: [
                              _MiniScoreBar(score: widget.score!, color: accentColor),
                              const SizedBox(width: 8),
                              Text(
                                '${widget.score!.round()}% Score',
                                style: theme.textTheme.bodySmall?.copyWith(
                                  color: theme.colorScheme.onSurfaceVariant,
                                  fontWeight: FontWeight.w500,
                                ),
                              ),
                            ],
                          ),
                        ],
                      ],
                    ),
                  ),
                  Icon(
                    widget.isExpanded ? Icons.keyboard_arrow_up_rounded : Icons.keyboard_arrow_down_rounded,
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ],
              ),
            ),
          ),
          // Expandable content
          SizeTransition(
            sizeFactor: _expandAnimation,
            child: Padding(
              padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Divider(height: 1),
                  const SizedBox(height: 20),
                  // We remove the conditional check for widget.isExpanded here
                  // to prevent children from vanishing before animation finishes.
                  _buildChart() ?? const SizedBox.shrink(),
                  ...widget.metrics.map((metric) => _MetricRow(metric: metric)),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _MiniScoreBar extends StatelessWidget {
  const _MiniScoreBar({required this.score, required this.color});
  final double score;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 60,
      height: 6,
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(3),
      ),
      child: FractionallySizedBox(
        alignment: Alignment.centerLeft,
        widthFactor: (score / 100).clamp(0.0, 1.0),
        child: Container(
          decoration: BoxDecoration(
            color: color,
            borderRadius: BorderRadius.circular(3),
          ),
        ),
      ),
    );
  }
}

class _MetricRow extends StatelessWidget {
  const _MetricRow({required this.metric});

  final ContextMetric metric;

  IconData? _getIconForKey(String key) {
    switch (key) {
      case 'residents':
        return Icons.groups_rounded;
      case 'population_density':
        return Icons.group_work_rounded;
      case 'low_income_households':
        return Icons.savings_outlined;
      case 'average_woz':
        return Icons.home_work_rounded;
      case 'income_per_recipient':
      case 'income_per_inhabitant':
      case 'avg_income_recipient':
      case 'avg_income_inhabitant':
        return Icons.euro_rounded;
      case 'urbanity':
        return Icons.location_city_rounded;
      case 'education_low':
      case 'education_medium':
      case 'education_high':
        return Icons.school_rounded;
      case 'gender_men':
        return Icons.male_rounded;
      case 'gender_women':
        return Icons.female_rounded;
      case 'households_with_children':
        return Icons.family_restroom_rounded;
      case 'households_without_children':
        return Icons.person_outline_rounded;
      case 'single_households':
        return Icons.person_rounded;
      case 'age_0_14':
      case 'age_0_15':
      case 'age_15_24':
      case 'age_15_25':
      case 'age_25_44':
      case 'age_25_45':
      case 'age_45_64':
      case 'age_45_65':
      case 'age_65_plus':
        return Icons.cake_rounded;
      case 'housing_owner':
      case 'housing_rental':
        return Icons.home_rounded;
      case 'housing_social':
        return Icons.holiday_village_rounded;
      case 'housing_multifamily':
        return Icons.apartment_rounded;
      case 'housing_pre2000':
      case 'housing_post2000':
        return Icons.calendar_today_rounded;
      case 'mobility_cars_household':
      case 'mobility_total_cars':
        return Icons.directions_car_rounded;
      case 'mobility_car_density':
        return Icons.traffic_rounded;
      case 'dist_supermarket':
        return Icons.shopping_cart_rounded;
      case 'dist_gp':
        return Icons.local_hospital_rounded;
      case 'dist_school':
      case 'schools_3km':
        return Icons.school_rounded;
      case 'dist_daycare':
        return Icons.child_care_rounded;
      default:
        return null;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final icon = _getIconForKey(metric.key);

    String displayValue;
    if (metric.value != null) {
      displayValue = metric.value!.toStringAsFixed(
        metric.value! == metric.value!.roundToDouble() ? 0 : 1,
      );
      if (metric.unit != null) {
        displayValue = '$displayValue ${metric.unit}';
      }
    } else if (metric.note != null) {
      displayValue = metric.note!;
    } else {
      displayValue = '—';
    }

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        children: [
          if (icon != null) ...[
            Icon(
              icon,
              size: 18,
              color: theme.colorScheme.onSurfaceVariant.withValues(alpha: 0.6),
            ),
            const SizedBox(width: 12),
          ],
          Expanded(
            child: Text(
              metric.label,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          ),
          const SizedBox(width: 12),
          Flexible(
            child: Text(
              displayValue,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              textAlign: TextAlign.end,
              style: theme.textTheme.bodyMedium?.copyWith(
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          if (metric.score != null) ...[
            const SizedBox(width: 12),
            _ScoreBadge(score: metric.score!),
          ],
        ],
      ),
    );
  }
}

class _ScoreBadge extends StatelessWidget {
  const _ScoreBadge({required this.score});

  final double score;

  Color _getColor() {
    if (score >= 80) return const Color(0xFF10B981);
    if (score >= 60) return const Color(0xFF3B82F6);
    if (score >= 40) return const Color(0xFFF59E0B);
    return const Color(0xFFEF4444);
  }

  @override
  Widget build(BuildContext context) {
    final color = _getColor();
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        score.round().toString(),
        style: TextStyle(
          color: color,
          fontSize: 11,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}
