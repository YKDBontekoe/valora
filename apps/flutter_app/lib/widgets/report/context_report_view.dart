import 'package:flutter/material.dart';
import '../../models/context_report.dart';
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
    final theme = Theme.of(context);

    // We use a Column instead of ListView so it can be embedded in other ScrollViews
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // Header with address
        if (showHeader) ...[
          Container(
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
          ),
          const SizedBox(height: 24),
        ],

        // AI Insight
        AiInsightCard(report: report),
        const SizedBox(height: 24),

        // Score overview
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Main gauge
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
            // Radar chart
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
        ),
        const SizedBox(height: 24),
        // Category cards
        if (report.socialMetrics.isNotEmpty)
          MetricCategoryCard(
            title: 'Social',
            icon: Icons.people_rounded,
            metrics: report.socialMetrics,
            score: report.categoryScores['Social'],
            accentColor: const Color(0xFF3B82F6),
            initiallyExpanded: true,
          ),
        if (report.crimeMetrics.isNotEmpty)
          MetricCategoryCard(
            title: 'Safety',
            icon: Icons.shield_rounded,
            metrics: report.crimeMetrics,
            score: report.categoryScores['Safety'],
            accentColor: const Color(0xFF10B981),
          ),
        if (report.demographicsMetrics.isNotEmpty)
          MetricCategoryCard(
            title: 'Demographics',
            icon: Icons.family_restroom_rounded,
            metrics: report.demographicsMetrics,
            score: report.categoryScores['Demographics'],
            accentColor: const Color(0xFF8B5CF6),
          ),
        if (report.amenityMetrics.isNotEmpty)
          MetricCategoryCard(
            title: 'Amenities',
            icon: Icons.store_rounded,
            metrics: report.amenityMetrics,
            score: report.categoryScores['Amenities'],
            accentColor: const Color(0xFFF59E0B),
          ),
        if (report.environmentMetrics.isNotEmpty)
          MetricCategoryCard(
            title: 'Environment',
            icon: Icons.eco_rounded,
            metrics: report.environmentMetrics,
            score: report.categoryScores['Environment'],
            accentColor: const Color(0xFF22C55E),
          ),
        // Warnings
        if (report.warnings.isNotEmpty) ...[
          const SizedBox(height: 12),
          Container(
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
          ),
        ],
        // Source attributions
        const SizedBox(height: 16),
        ExpansionTile(
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
        ),
        const SizedBox(height: 24),
      ],
    );
  }
}
