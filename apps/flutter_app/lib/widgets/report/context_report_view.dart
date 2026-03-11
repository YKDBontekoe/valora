import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:flutter_animate/flutter_animate.dart';

import '../../models/context_report.dart';
import '../../providers/context_report_provider.dart';
import '../valora_glass_container.dart';
import '../../core/theme/valora_typography.dart';
import '../../core/theme/valora_colors.dart';
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
    final count = childCount(report, showHeader: showHeader);
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: List.generate(
        count,
        (index) => buildChild(
          context,
          index,
          report,
          showHeader: showHeader,
        ),
      ),
    );
  }

  static int childCount(ContextReport report, {bool showHeader = true}) {
    int count = 0;
    if (showHeader) {
      count++; // Header (Location)
      count++; // Spacing
    }
    count++; // Score Overview (Gauge + Radar)
    count++; // Spacing
    count++; // Neighborhood Profile Title + Grid
    count++; // Spacing
    count++; // AI Insight
    count++; // Spacing
    if (report.socialMetrics.isNotEmpty) count++;
    if (report.crimeMetrics.isNotEmpty) count++;
    if (report.demographicsMetrics.isNotEmpty) count++;
    if (report.housingMetrics.isNotEmpty) count++;
    if (report.mobilityMetrics.isNotEmpty) count++;
    if (report.amenityMetrics.isNotEmpty) count++;
    if (report.environmentMetrics.isNotEmpty) count++;
    if (report.warnings.isNotEmpty) {
      count++; // Spacing
      count++; // Warnings
    }
    count++; // Spacing
    count++; // Sources
    count++; // Final Spacing
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

    // Header: Location
    if (showHeader) {
      if (index == currentIndex++) {
        final subtitleParts = [
          report.location.neighborhoodName,
          report.location.municipalityName,
        ].where((s) => s != null && s.isNotEmpty).toList();

        return ValoraGlassContainer(
          padding: const EdgeInsets.all(24),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: theme.colorScheme.primary.withValues(alpha: 0.1),
                  shape: BoxShape.circle,
                ),
                child: Icon(Icons.location_on_rounded, color: theme.colorScheme.primary),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      report.location.displayAddress,
                      style: ValoraTypography.titleLarge,
                    ),
                    if (subtitleParts.isNotEmpty)
                      Text(
                        subtitleParts.join(', '),
                        style: ValoraTypography.bodyMedium.copyWith(color: theme.colorScheme.onSurfaceVariant),
                      ),
                  ],
                ),
              ),
            ],
          ),
        ).animate().fadeIn().slideY(begin: -0.1);
      }
      if (index == currentIndex++) return const SizedBox(height: 24);
    }

    // Score Overview
    if (index == currentIndex++) {
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
          const SizedBox(width: 24),
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
    if (index == currentIndex++) return const SizedBox(height: 32);

    // Smart Insights
    if (index == currentIndex++) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Neighborhood Profile',
            style: ValoraTypography.titleMedium,
          ),
          const SizedBox(height: 16),
          SmartInsightsGrid(report: report),
        ],
      ).animate().fadeIn(delay: 400.ms);
    }
    if (index == currentIndex++) return const SizedBox(height: 32);

    // AI Insight
    if (index == currentIndex++) {
      return AiInsightCard(report: report);
    }
    if (index == currentIndex++) return const SizedBox(height: 32);

    // Categories
    if (report.socialMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return _buildCategoryCard(
          context: context,
          category: 'Social',
          icon: Icons.people_rounded,
          metrics: report.socialMetrics,
          accentColor: const Color(0xFF3B82F6),
          categoryScores: report.categoryScores,
          defaultExpanded: true,
        );
      }
    }
    if (report.crimeMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return _buildCategoryCard(
          context: context,
          category: 'Safety',
          icon: Icons.shield_rounded,
          metrics: report.crimeMetrics,
          accentColor: const Color(0xFF10B981),
          categoryScores: report.categoryScores,
        );
      }
    }
    if (report.demographicsMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return _buildCategoryCard(
          context: context,
          category: 'Demographics',
          icon: Icons.family_restroom_rounded,
          metrics: report.demographicsMetrics,
          accentColor: const Color(0xFF8B5CF6),
          categoryScores: report.categoryScores,
        );
      }
    }
    if (report.housingMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return _buildCategoryCard(
          context: context,
          category: 'Housing',
          icon: Icons.home_work_rounded,
          metrics: report.housingMetrics,
          accentColor: const Color(0xFFEC4899),
          categoryScores: report.categoryScores,
        );
      }
    }
    if (report.mobilityMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return _buildCategoryCard(
          context: context,
          category: 'Mobility',
          icon: Icons.directions_car_rounded,
          metrics: report.mobilityMetrics,
          accentColor: const Color(0xFF0EA5E9),
          categoryScores: report.categoryScores,
        );
      }
    }
    if (report.amenityMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return _buildCategoryCard(
          context: context,
          category: 'Amenities',
          icon: Icons.store_rounded,
          metrics: report.amenityMetrics,
          accentColor: const Color(0xFFF59E0B),
          categoryScores: report.categoryScores,
        );
      }
    }
    if (report.environmentMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return _buildCategoryCard(
          context: context,
          category: 'Environment',
          icon: Icons.eco_rounded,
          metrics: report.environmentMetrics,
          accentColor: const Color(0xFF22C55E),
          categoryScores: report.categoryScores,
        );
      }
    }

    // Warnings
    if (report.warnings.isNotEmpty) {
      if (index == currentIndex++) return const SizedBox(height: 12);
      if (index == currentIndex++) {
        return Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: theme.colorScheme.tertiaryContainer.withValues(alpha: 0.3),
            borderRadius: BorderRadius.circular(16),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.info_outline_rounded, size: 18),
                  const SizedBox(width: 8),
                  Text('Data Notes', style: ValoraTypography.labelLarge.copyWith(fontWeight: FontWeight.bold)),
                ],
              ),
              const SizedBox(height: 8),
              ...report.warnings.map((w) => Text('• $w', style: ValoraTypography.bodySmall)),
            ],
          ),
        );
      }
    }

    if (index == currentIndex++) return const SizedBox(height: 24);
    if (index == currentIndex++) {
      return ExpansionTile(
        title: Text('Data Sources', style: ValoraTypography.labelMedium),
        tilePadding: EdgeInsets.zero,
        children: report.sources.map((s) => ListTile(
          title: Text(s.source, style: ValoraTypography.labelSmall),
          subtitle: Text(s.license, style: ValoraTypography.labelSmall.copyWith(color: ValoraColors.neutral500)),
          dense: true,
        )).toList(),
      );
    }
    if (index == currentIndex++) return const SizedBox(height: 40);

    return const SizedBox.shrink();
  }

  static Widget _buildCategoryCard({
    required BuildContext context,
    required String category,
    required IconData icon,
    required List<ContextMetric> metrics,
    required Color accentColor,
    required Map<String, double?> categoryScores,
    bool defaultExpanded = false,
  }) {
    return Selector<ContextReportProvider, bool>(
      selector: (_, p) => p.isExpanded(category, defaultValue: defaultExpanded),
      builder: (context, isExpanded, _) => MetricCategoryCard(
        title: category,
        icon: icon,
        metrics: metrics,
        score: categoryScores[category],
        accentColor: accentColor,
        isExpanded: isExpanded,
        onToggle: (v) => context.read<ContextReportProvider>().setExpanded(category, v),
      ),
    );
  }
}
