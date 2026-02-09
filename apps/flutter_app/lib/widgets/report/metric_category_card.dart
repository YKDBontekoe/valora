import 'package:flutter/material.dart';
import '../../models/context_report.dart';

/// A collapsible card displaying metrics for a category with premium styling.
class MetricCategoryCard extends StatefulWidget {
  const MetricCategoryCard({
    super.key,
    required this.title,
    required this.icon,
    required this.metrics,
    required this.score,
    this.accentColor,
    this.initiallyExpanded = false,
  });

  final String title;
  final IconData icon;
  final List<ContextMetric> metrics;
  final double? score;
  final Color? accentColor;
  final bool initiallyExpanded;

  @override
  State<MetricCategoryCard> createState() => _MetricCategoryCardState();
}

class _MetricCategoryCardState extends State<MetricCategoryCard>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _expandAnimation;
  late bool _isExpanded;

  @override
  void initState() {
    super.initState();
    _isExpanded = widget.initiallyExpanded;
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 250),
    );
    _expandAnimation = CurvedAnimation(
      parent: _controller,
      curve: Curves.easeInOut,
    );
    if (_isExpanded) _controller.value = 1.0;
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _toggleExpanded() {
    setState(() {
      _isExpanded = !_isExpanded;
      if (_isExpanded) {
        _controller.forward();
      } else {
        _controller.reverse();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final accentColor = widget.accentColor ?? theme.colorScheme.primary;

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(16),
        color: theme.colorScheme.surfaceContainerLow,
        border: Border.all(
          color: theme.colorScheme.outlineVariant.withValues(alpha: 0.3),
        ),
      ),
      child: Column(
        children: [
          // Header
          InkWell(
            onTap: _toggleExpanded,
            borderRadius: BorderRadius.circular(16),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: accentColor.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Icon(widget.icon, color: accentColor, size: 22),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          widget.title,
                          style: theme.textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        if (widget.score != null)
                          Text(
                            'Score: ${widget.score!.round()}/100',
                            style: theme.textTheme.bodySmall?.copyWith(
                              color: theme.colorScheme.onSurfaceVariant,
                            ),
                          ),
                      ],
                    ),
                  ),
                  AnimatedRotation(
                    turns: _isExpanded ? 0.5 : 0,
                    duration: const Duration(milliseconds: 250),
                    child: Icon(
                      Icons.expand_more,
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
          ),
          // Expandable content
          SizeTransition(
            sizeFactor: _expandAnimation,
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
              child: Column(
                children: [
                  const Divider(height: 1),
                  const SizedBox(height: 12),
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
      case 'age_15_24':
      case 'age_25_44':
      case 'age_45_64':
      case 'age_65_plus':
      case 'age_65_plus':
        return Icons.cake_rounded;
      // Phase 2: Housing
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
      // Phase 2: Mobility
      case 'mobility_cars_household':
      case 'mobility_total_cars':
        return Icons.directions_car_rounded;
      case 'mobility_car_density':
        return Icons.traffic_rounded;
      // Phase 2: Proximity
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
        return null; // No icon for others
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
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center, // Align to center for icon
        children: [
          Expanded(
            flex: 3,
            child: Row(
              children: [
                if (icon != null) ...[
                  Icon(
                    icon,
                    size: 16,
                    color: theme.colorScheme.primary.withValues(alpha: 0.7),
                  ),
                  const SizedBox(width: 8),
                ],
                Expanded(
                  child: Text(
                    metric.label,
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            flex: 2,
            child: Text(
              displayValue,
              textAlign: TextAlign.end,
              style: theme.textTheme.bodyMedium?.copyWith(
                fontWeight: FontWeight.w500,
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
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        score.round().toString(),
        style: TextStyle(
          color: color,
          fontSize: 12,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}
