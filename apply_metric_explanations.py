import sys

with open('apps/flutter_app/lib/widgets/report/metric_category_card.dart', 'r') as f:
    content = f.read()

# Import ValoraGlassContainer and ValoraButton
if "import '../valora_glass_container.dart';" not in content:
    content = "import '../valora_glass_container.dart';\nimport '../common/valora_button.dart';\n" + content

# Update _MetricRow to include info button
metric_row_build = '    final icon = _getIconForKey(metric.key);'
new_metric_row_content = metric_row_build + """

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
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
                  child: GestureDetector(
                    onTap: () => _showExplanation(context, metric),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Flexible(
                          child: Text(
                            metric.label,
                            style: theme.textTheme.bodyMedium?.copyWith(
                              color: theme.colorScheme.onSurfaceVariant,
                            ),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        const SizedBox(width: 4),
                        Icon(
                          Icons.info_outline_rounded,
                          size: 12,
                          color: theme.colorScheme.onSurfaceVariant.withValues(alpha: 0.5),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
"""
# We need to replace the whole build method of _MetricRow or just the part we want
# Let's find the start of build method in _MetricRow

content = content.replace(metric_row_build, new_metric_row_content)

# Remove the old Row implementation that we just replaced part of
# This is tricky because of nested Widgets.
# I'll just rewrite the _MetricRow class.

metric_row_class_start = 'class _MetricRow extends StatelessWidget {'
metric_row_replacement = """
class _MetricRow extends StatelessWidget {
  const _MetricRow({required this.metric});

  final ContextMetric metric;

  void _showExplanation(BuildContext context, ContextMetric metric) {
    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) => ValoraGlassContainer(
        borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: Colors.blue.withValues(alpha: 0.1),
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(Icons.info_outline_rounded, color: Colors.blue),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    metric.label,
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Text(
              _getExplanationText(metric.key),
              style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                color: Theme.of(context).colorScheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              child: ValoraButton(
                label: 'Got it',
                onPressed: () => Navigator.pop(context),
              ),
            ),
          ],
        ),
      ),
    );
  }

  String _getExplanationText(String key) {
    switch (key) {
      case 'residents': return 'Total number of people living in this area.';
      case 'population_density': return 'Number of inhabitants per square kilometer.';
      case 'average_woz': return 'Average tax appraisal value of residential properties in the neighborhood.';
      case 'crime_theft': return 'Reported incidents of theft, including burglary and shoplifting.';
      case 'crime_violence': return 'Reported incidents of physical violence or assault.';
      case 'dist_supermarket': return 'Walking or driving distance to the nearest grocery store.';
      case 'air_quality_index': return 'A measure of how clean or polluted the air is, based on multiple pollutants.';
      default: return 'This metric provides detailed insight into this category for the specified location.';
    }
  }

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
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
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
                  child: InkWell(
                    onTap: () => _showExplanation(context, metric),
                    borderRadius: BorderRadius.circular(4),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Flexible(
                          child: Text(
                            metric.label,
                            style: theme.textTheme.bodyMedium?.copyWith(
                              color: theme.colorScheme.onSurfaceVariant,
                            ),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        const SizedBox(width: 4),
                        Icon(
                          Icons.info_outline_rounded,
                          size: 12,
                          color: theme.colorScheme.onSurfaceVariant.withValues(alpha: 0.5),
                        ),
                      ],
                    ),
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
"""

# Find where _MetricRow ends and replace it
import re
new_content = re.sub(r'class _MetricRow.*?}\n}', metric_row_replacement + '}', content, flags=re.DOTALL)

with open('apps/flutter_app/lib/widgets/report/metric_category_card.dart', 'w') as f:
    f.write(new_content)
