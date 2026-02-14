import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../models/context_report.dart';
import '../../providers/context_report_provider.dart';
import 'report_widgets.dart';
import 'ai_insight_card.dart';

class ContextReportView extends StatelessWidget {
  const ContextReportView({
    super.key,
    required this.report,
    this.showHeader = true,
  });

  final ContextReport report;
  final bool showHeader;

  @override
  Widget build(BuildContext context) {
    return ListView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      itemCount: childCount(report, showHeader: showHeader),
      itemBuilder: (context, index) => buildChild(context, index, report, showHeader: showHeader),
    );
  }

  static int childCount(ContextReport report, {bool showHeader = true}) {
    int count = 0;
    if (showHeader) count += 2; // Header + Spacing
    count += 2; // AI Insight + Spacing
    count += 2; // Scores + Spacing

    if (report.socialMetrics.isNotEmpty) count++;
    if (report.crimeMetrics.isNotEmpty) count++;
    if (report.demographicsMetrics.isNotEmpty) count++;
    if (report.housingMetrics.isNotEmpty) count++;
    if (report.mobilityMetrics.isNotEmpty) count++;
    if (report.amenityMetrics.isNotEmpty) count++;
    if (report.environmentMetrics.isNotEmpty) count++;

    if (report.warnings.isNotEmpty) count += 2; // Spacing + Warnings
    count += 2; // Spacing + Sources
    count += 1; // Bottom Spacing

    return count;
  }

  static Widget buildChild(
    BuildContext context,
    int index,
    ContextReport report, {
    bool showHeader = true,
  }) {
    final theme = Theme.of(context);
    int currentIndex = 0;

    // Header
    if (showHeader) {
      if (index == currentIndex++) {
        return Container(
          key: const ValueKey('report-header'),
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            gradient: LinearGradient(
              colors: [
                theme.colorScheme.primaryContainer,
                theme.colorScheme.secondaryContainer,
              ],
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
            ),
            borderRadius: BorderRadius.circular(20),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(
                    Icons.location_on_rounded,
                    color: theme.colorScheme.primary,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      report.location.displayAddress,
                      style: theme.textTheme.titleLarge?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ],
              ),
              if (report.location.neighborhoodName != null) ...[
                const SizedBox(height: 8),
                Text(
                  [
                    report.location.neighborhoodName,
                    report.location.municipalityName
                  ].where((s) => s != null && s.isNotEmpty).join(', '),
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
              ],
            ],
          ),
        );
      }
      if (index == currentIndex++) return const SizedBox(key: ValueKey('report-header-spacing'), height: 24);
    }

    // AI Insight
    if (index == currentIndex++) {
      return AiInsightCard(
        key: const ValueKey('report-ai-insight'),
        report: report,
      );
    }
    if (index == currentIndex++) return const SizedBox(key: ValueKey('report-ai-insight-spacing'), height: 24);

    // Score overview
    if (index == currentIndex++) {
      return Row(
        key: const ValueKey('report-scores'),
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Center(
              child: ScoreGauge(
                score: report.compositeScore,
                size: 160,
                strokeWidth: 14,
              ),
            ),
          ),
          const SizedBox(width: 16),
          if (report.categoryScores.isNotEmpty) ...[
            const SizedBox(width: 16),
            Expanded(
              child: CategoryRadar(
                categoryScores: report.categoryScores,
                size: 160,
              ),
            ),
          ],
        ],
      );
    }
    if (index == currentIndex++) return const SizedBox(key: ValueKey('report-scores-spacing'), height: 24);

    // Category cards
    if (report.socialMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            key: const ValueKey('report-category-Social'),
            title: 'Social',
            icon: Icons.people_rounded,
            metrics: report.socialMetrics,
            score: report.categoryScores['Social'],
            accentColor: const Color(0xFF3B82F6),
            isExpanded: provider.isExpanded('Social', defaultValue: true),
            onToggle: (expanded) => provider.setExpanded('Social', expanded),
          ),
        );
      }
    }
    if (report.crimeMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            key: const ValueKey('report-category-Safety'),
            title: 'Safety',
            icon: Icons.shield_rounded,
            metrics: report.crimeMetrics,
            score: report.categoryScores['Safety'],
            accentColor: const Color(0xFF10B981),
            isExpanded: provider.isExpanded('Safety'),
            onToggle: (expanded) => provider.setExpanded('Safety', expanded),
          ),
        );
      }
    }
    if (report.demographicsMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            key: const ValueKey('report-category-Demographics'),
            title: 'Demographics',
            icon: Icons.family_restroom_rounded,
            metrics: report.demographicsMetrics,
            score: report.categoryScores['Demographics'],
            accentColor: const Color(0xFF8B5CF6),
            isExpanded: provider.isExpanded('Demographics'),
            onToggle: (expanded) => provider.setExpanded('Demographics', expanded),
          ),
        );
      }
    }
    if (report.housingMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            key: const ValueKey('report-category-Housing'),
            title: 'Housing',
            icon: Icons.home_work_rounded,
            metrics: report.housingMetrics,
            score: report.categoryScores['Housing'],
            accentColor: const Color(0xFFEC4899),
            isExpanded: provider.isExpanded('Housing'),
            onToggle: (expanded) => provider.setExpanded('Housing', expanded),
          ),
        );
      }
    }
    if (report.mobilityMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            key: const ValueKey('report-category-Mobility'),
            title: 'Mobility',
            icon: Icons.directions_car_rounded,
            metrics: report.mobilityMetrics,
            score: report.categoryScores['Mobility'],
            accentColor: const Color(0xFF0EA5E9),
            isExpanded: provider.isExpanded('Mobility'),
            onToggle: (expanded) => provider.setExpanded('Mobility', expanded),
          ),
        );
      }
    }
    if (report.amenityMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            key: const ValueKey('report-category-Amenities'),
            title: 'Amenities',
            icon: Icons.store_rounded,
            metrics: report.amenityMetrics,
            score: report.categoryScores['Amenities'],
            accentColor: const Color(0xFFF59E0B),
            isExpanded: provider.isExpanded('Amenities'),
            onToggle: (expanded) => provider.setExpanded('Amenities', expanded),
          ),
        );
      }
    }
    if (report.environmentMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            key: const ValueKey('report-category-Environment'),
            title: 'Environment',
            icon: Icons.eco_rounded,
            metrics: report.environmentMetrics,
            score: report.categoryScores['Environment'],
            accentColor: const Color(0xFF22C55E),
            isExpanded: provider.isExpanded('Environment'),
            onToggle: (expanded) => provider.setExpanded('Environment', expanded),
          ),
        );
      }
    }

    // Warnings
    if (report.warnings.isNotEmpty) {
      if (index == currentIndex++) return const SizedBox(key: ValueKey('report-warnings-spacing-top'), height: 12);
      if (index == currentIndex++) {
        return Container(
          key: const ValueKey('report-warnings'),
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: theme.colorScheme.tertiaryContainer.withValues(alpha: 0.5),
            borderRadius: BorderRadius.circular(14),
            border: Border.all(
              color: theme.colorScheme.outline.withValues(alpha: 0.2),
            ),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(
                    Icons.info_outline_rounded,
                    size: 20,
                    color: theme.colorScheme.tertiary,
                  ),
                  const SizedBox(width: 8),
                  Text(
                    'Data Notes',
                    style: theme.textTheme.titleSmall?.copyWith(
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              ...report.warnings.map(
                (warning) => Padding(
                  padding: const EdgeInsets.only(bottom: 4),
                  child: Text(
                    '• $warning',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ),
              ),
            ],
          ),
        );
      }
    }

    // Source attributions
    if (index == currentIndex++) return const SizedBox(key: ValueKey('report-sources-spacing-top'), height: 16);
    if (index == currentIndex++) {
      return ExpansionTile(
        key: const ValueKey('report-sources'),
        tilePadding: EdgeInsets.zero,
        title: Text(
          'Data Sources',
          style: theme.textTheme.labelLarge,
        ),
        children: report.sources
            .map(
              (source) => ListTile(
                dense: true,
                contentPadding: EdgeInsets.zero,
                title: Text(source.source),
                subtitle: Text('License: ${source.license}'),
              ),
            )
            .toList(),
      );
    }

    if (index == currentIndex++) return const SizedBox(key: ValueKey('report-bottom-spacing'), height: 24);

    return const SizedBox.shrink();
  }
}
