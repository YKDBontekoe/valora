import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:flutter_animate/flutter_animate.dart';

import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../models/context_report.dart';
import '../../providers/context_report_provider.dart';
import '../valora_glass_container.dart';
import 'ai_insight_card.dart';
import 'category_radar.dart';
import 'metric_category_card.dart';
import 'score_gauge.dart';
import 'smart_insights_grid.dart';

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
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (showHeader) ...[
          _buildHeader(context),
          const SizedBox(height: ValoraSpacing.lg),
        ],

        // Score Overview
        _buildScoreOverview(),
        const SizedBox(height: ValoraSpacing.xl),

        // Neighborhood Profile
        _buildNeighborhoodProfile(context),
        const SizedBox(height: ValoraSpacing.xl),

        // AI Insight
        AiInsightCard(report: report),
        const SizedBox(height: ValoraSpacing.xl),

        // Categories
        ..._buildCategories(context),

        // Warnings
        if (report.warnings.isNotEmpty) ...[
          const SizedBox(height: ValoraSpacing.md),
          _buildWarnings(context),
        ],

        const SizedBox(height: ValoraSpacing.lg),

        // Sources
        _buildSources(context),

        // Bottom padding
        const SizedBox(height: ValoraSpacing.xxl),
      ],
    );
  }

  Widget _buildHeader(BuildContext context) {
    final theme = Theme.of(context);
    final subtitleParts = [
      report.location.neighborhoodName,
      report.location.municipalityName,
    ].where((s) => s != null && s.isNotEmpty).toList();

    return ValoraGlassContainer(
      padding: const EdgeInsets.all(ValoraSpacing.lg),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(ValoraSpacing.sm + 4),
            decoration: BoxDecoration(
              color: theme.colorScheme.primary.withValues(alpha: 0.1),
              shape: BoxShape.circle,
            ),
            child: Icon(Icons.location_on_rounded, color: theme.colorScheme.primary),
          ),
          const SizedBox(width: ValoraSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  report.location.displayAddress,
                  style: ValoraTypography.titleLarge.copyWith(fontWeight: FontWeight.bold),
                ),
                if (subtitleParts.isNotEmpty)
                  Text(
                    subtitleParts.join(', '),
                    style: ValoraTypography.bodyMedium.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    ).animate().fadeIn().slideY(begin: -0.1);
  }

  Widget _buildScoreOverview() {
    return Row(
      children: [
        Expanded(
          flex: 4,
          child: ScoreGauge(
            score: report.compositeScore,
            size: 140,
            strokeWidth: 12,
          ),
        ),
        const SizedBox(width: ValoraSpacing.lg),
        Expanded(
          flex: 6,
          child: CategoryRadar(
            categoryScores: report.categoryScores,
            size: 160,
          ),
        ),
      ],
    ).animate().fadeIn(delay: 200.ms);
  }

  Widget _buildNeighborhoodProfile(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Neighborhood Profile',
          style: ValoraTypography.titleMedium.copyWith(fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: ValoraSpacing.md),
        SmartInsightsGrid(report: report),
      ],
    ).animate().fadeIn(delay: 400.ms);
  }

  List<Widget> _buildCategories(BuildContext context) {
    final categories = <Widget>[];

    void addCategory({
      required String title,
      required IconData icon,
      required List<ContextMetric> metrics,
      required Color color,
      required String key,
    }) {
      if (metrics.isNotEmpty) {
        categories.add(
          Consumer<ContextReportProvider>(
            builder: (context, provider, _) => MetricCategoryCard(
              title: title,
              icon: icon,
              metrics: metrics,
              score: report.categoryScores[key],
              accentColor: color,
              isExpanded: provider.isExpanded(key, defaultValue: key == 'Social'),
              onToggle: (v) => provider.setExpanded(key, v),
            ),
          ),
        );
        categories.add(const SizedBox(height: ValoraSpacing.lg));
      }
    }

    addCategory(
      title: 'Social',
      icon: Icons.people_rounded,
      metrics: report.socialMetrics,
      color: const Color(0xFF3B82F6), // Blue
      key: 'Social',
    );

    addCategory(
      title: 'Safety',
      icon: Icons.shield_rounded,
      metrics: report.crimeMetrics,
      color: ValoraColors.scoreExcellent, // Green
      key: 'Safety',
    );

    addCategory(
      title: 'Demographics',
      icon: Icons.family_restroom_rounded,
      metrics: report.demographicsMetrics,
      color: const Color(0xFF8B5CF6), // Purple
      key: 'Demographics',
    );

    addCategory(
      title: 'Housing',
      icon: Icons.home_work_rounded,
      metrics: report.housingMetrics,
      color: ValoraColors.newBadge, // Pink
      key: 'Housing',
    );

    addCategory(
      title: 'Mobility',
      icon: Icons.directions_car_rounded,
      metrics: report.mobilityMetrics,
      color: ValoraColors.info, // Sky
      key: 'Mobility',
    );

    addCategory(
      title: 'Amenities',
      icon: Icons.store_rounded,
      metrics: report.amenityMetrics,
      color: ValoraColors.warning, // Amber
      key: 'Amenities',
    );

    addCategory(
      title: 'Environment',
      icon: Icons.eco_rounded,
      metrics: report.environmentMetrics,
      color: const Color(0xFF22C55E), // Green
      key: 'Environment',
    );

    return categories;
  }

  Widget _buildWarnings(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.all(ValoraSpacing.md),
      decoration: BoxDecoration(
        color: theme.colorScheme.tertiaryContainer.withValues(alpha: 0.3),
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusLg),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(Icons.info_outline_rounded, size: 18),
              SizedBox(width: ValoraSpacing.sm),
              Text('Data Notes', style: TextStyle(fontWeight: FontWeight.bold)),
            ],
          ),
          const SizedBox(height: ValoraSpacing.sm),
          ...report.warnings.map((w) => Text('• $w', style: ValoraTypography.bodySmall)),
        ],
      ),
    );
  }

  Widget _buildSources(BuildContext context) {
    return ExpansionTile(
      title: Text('Data Sources', style: ValoraTypography.labelLarge),
      tilePadding: EdgeInsets.zero,
      children: report.sources.map((s) => ListTile(
        title: Text(s.source, style: ValoraTypography.labelMedium),
        subtitle: Text(s.license, style: ValoraTypography.labelSmall.copyWith(fontSize: 10)),
        dense: true,
        contentPadding: const EdgeInsets.symmetric(horizontal: ValoraSpacing.sm),
      )).toList(),
    );
  }
}
