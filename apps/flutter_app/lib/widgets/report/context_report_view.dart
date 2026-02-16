import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:flutter_animate/flutter_animate.dart';

import '../../models/context_report.dart';
import '../../providers/context_report_provider.dart';
import '../valora_widgets.dart';
import '../valora_glass_container.dart';
import 'ai_insight_card.dart';
import 'category_radar.dart';
import 'metric_category_card.dart';
import 'score_gauge.dart';
import 'smart_insights_grid.dart';

class ContextReportView extends StatelessWidget {
  const ContextReportView({super.key, required this.report});

  final ContextReport report;

  @override
  Widget build(BuildContext context) {
    return ListView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      itemCount: childCount(report),
      itemBuilder: (context, index) => buildChild(context, index, report),
    );
  }

  static int childCount(ContextReport report) {
    int count = 0;
    count++; // Header (Location)
    count++; // Spacing
    count++; // Score Overview (Gauge + Radar)
    count++; // Spacing
    count++; // Smart Insights Grid
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

  static Widget buildChild(BuildContext context, int index, ContextReport report) {
    final theme = Theme.of(context);
    int currentIndex = 0;

    // Header: Location
    if (index == currentIndex++) {
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
                    style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
                  ),
                  if (report.location.neighborhoodName != null)
                    Text(
                      '${report.location.neighborhoodName}, ${report.location.municipalityName}',
                      style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                    ),
                ],
              ),
            ),
          ],
        ),
      ).animate().fadeIn().slideY(begin: -0.1);
    }
    if (index == currentIndex++) return const SizedBox(height: 24);

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
            style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
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
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            title: 'Social',
            icon: Icons.people_rounded,
            metrics: report.socialMetrics,
            score: report.categoryScores['Social'],
            accentColor: const Color(0xFF3B82F6),
            isExpanded: provider.isExpanded('Social', defaultValue: true),
            onToggle: (v) => provider.setExpanded('Social', v),
          ),
        );
      }
    }
    if (report.crimeMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            title: 'Safety',
            icon: Icons.shield_rounded,
            metrics: report.crimeMetrics,
            score: report.categoryScores['Safety'],
            accentColor: const Color(0xFF10B981),
            isExpanded: provider.isExpanded('Safety'),
            onToggle: (v) => provider.setExpanded('Safety', v),
          ),
        );
      }
    }
    if (report.demographicsMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            title: 'Demographics',
            icon: Icons.family_restroom_rounded,
            metrics: report.demographicsMetrics,
            score: report.categoryScores['Demographics'],
            accentColor: const Color(0xFF8B5CF6),
            isExpanded: provider.isExpanded('Demographics'),
            onToggle: (v) => provider.setExpanded('Demographics', v),
          ),
        );
      }
    }
    if (report.housingMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            title: 'Housing',
            icon: Icons.home_work_rounded,
            metrics: report.housingMetrics,
            score: report.categoryScores['Housing'],
            accentColor: const Color(0xFFEC4899),
            isExpanded: provider.isExpanded('Housing'),
            onToggle: (v) => provider.setExpanded('Housing', v),
          ),
        );
      }
    }
    if (report.mobilityMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            title: 'Mobility',
            icon: Icons.directions_car_rounded,
            metrics: report.mobilityMetrics,
            score: report.categoryScores['Mobility'],
            accentColor: const Color(0xFF0EA5E9),
            isExpanded: provider.isExpanded('Mobility'),
            onToggle: (v) => provider.setExpanded('Mobility', v),
          ),
        );
      }
    }
    if (report.amenityMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            title: 'Amenities',
            icon: Icons.store_rounded,
            metrics: report.amenityMetrics,
            score: report.categoryScores['Amenities'],
            accentColor: const Color(0xFFF59E0B),
            isExpanded: provider.isExpanded('Amenities'),
            onToggle: (v) => provider.setExpanded('Amenities', v),
          ),
        );
      }
    }
    if (report.environmentMetrics.isNotEmpty) {
      if (index == currentIndex++) {
        return Consumer<ContextReportProvider>(
          builder: (context, provider, _) => MetricCategoryCard(
            title: 'Environment',
            icon: Icons.eco_rounded,
            metrics: report.environmentMetrics,
            score: report.categoryScores['Environment'],
            accentColor: const Color(0xFF22C55E),
            isExpanded: provider.isExpanded('Environment'),
            onToggle: (v) => provider.setExpanded('Environment', v),
          ),
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
              const Row(
                children: [
                  Icon(Icons.info_outline_rounded, size: 18),
                  SizedBox(width: 8),
                  Text('Data Notes', style: TextStyle(fontWeight: FontWeight.bold)),
                ],
              ),
              const SizedBox(height: 8),
              ...report.warnings.map((w) => Text('• $w', style: theme.textTheme.bodySmall)),
            ],
          ),
        );
      }
    }

    if (index == currentIndex++) return const SizedBox(height: 24);
    if (index == currentIndex++) {
      return ExpansionTile(
        title: const Text('Data Sources', style: TextStyle(fontSize: 14)),
        tilePadding: EdgeInsets.zero,
        children: report.sources.map((s) => ListTile(
          title: Text(s.source, style: const TextStyle(fontSize: 12)),
          subtitle: Text(s.license, style: const TextStyle(fontSize: 10)),
          dense: true,
        )).toList(),
      );
    }
    if (index == currentIndex++) return const SizedBox(height: 40);

    return const SizedBox.shrink();
  }
}
