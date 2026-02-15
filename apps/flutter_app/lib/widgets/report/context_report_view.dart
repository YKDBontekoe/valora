import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/context_report.dart';
import '../../providers/context_report_provider.dart';
import 'ai_insight_card.dart';
import 'category_radar.dart';
import 'metric_category_card.dart';
import 'metric_category_shimmer.dart';
import 'score_gauge.dart';

class ContextReportView extends StatelessWidget {
  const ContextReportView({
    super.key,
    this.report,
    this.showHeader = true,
  });

  final ContextReport? report;
  final bool showHeader;

  @override
  Widget build(BuildContext context) {
    if (report != null) {
      return _buildStaticReport(context, report!);
    }

    return Consumer<ContextReportProvider>(
      builder: (context, provider, _) {
        final location = provider.location;
        if (location == null) return const SizedBox.shrink();

        return Column(
          children: [
            if (showHeader) ...[
              _buildHeader(context, location),
              const SizedBox(height: 24),
            ],

            _buildScoreOverview(context, provider),
            const SizedBox(height: 24),

            if (_hasAnyMetrics(provider)) ...[
              AiInsightCard(
                key: ValueKey('report-ai-insight-${location.displayAddress}'),
                report: provider.report!,
              ),
              const SizedBox(height: 24),
            ],

            _buildCategory(context, 'Social', Icons.people_rounded, provider.socialMetrics, provider.loadingSocial, const Color(0xFF3B82F6), provider.categoryScores['Social']),
            _buildCategory(context, 'Safety', Icons.shield_rounded, provider.crimeMetrics, provider.loadingCrime, const Color(0xFF10B981), provider.categoryScores['Safety']),
            _buildCategory(context, 'Demographics', Icons.family_restroom_rounded, provider.demographicsMetrics, provider.loadingDemographics, const Color(0xFF8B5CF6), provider.categoryScores['Demographics']),
            _buildCategory(context, 'Housing', Icons.home_work_rounded, provider.housingMetrics, provider.loadingHousing, const Color(0xFFEC4899), provider.categoryScores['Housing']),
            _buildCategory(context, 'Mobility', Icons.directions_car_rounded, provider.mobilityMetrics, provider.loadingMobility, const Color(0xFF0EA5E9), provider.categoryScores['Mobility']),
            _buildCategory(context, 'Amenities', Icons.store_rounded, provider.amenityMetrics, provider.loadingAmenities, const Color(0xFFF59E0B), provider.categoryScores['Amenities']),
            _buildCategory(context, 'Environment', Icons.eco_rounded, provider.environmentMetrics, provider.loadingEnvironment, const Color(0xFF22C55E), provider.categoryScores['Environment']),
          ],
        );
      },
    );
  }

  Widget _buildScoreOverview(BuildContext context, ContextReportProvider provider) {
    // If anything is loading, we could show a shimmer for the whole row or just wait
    // Let's show the radar if at least some scores are present
    final scores = provider.categoryScores;
    final compositeScore = provider.report?.compositeScore ?? 0;

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Center(
            child: ScoreGauge(
              score: compositeScore,
              size: 160,
              strokeWidth: 14,
            ),
          ),
        ),
        const SizedBox(width: 16),
        Expanded(
          child: scores.isNotEmpty
            ? CategoryRadar(categoryScores: scores, size: 160)
            : const MetricCategoryShimmer(), // Using this as a simple placeholder for radar
        ),
      ],
    );
  }

  Widget _buildStaticReport(BuildContext context, ContextReport report) {
    return Column(
      children: [
        if (showHeader) ...[
          _buildHeader(context, report.location),
          const SizedBox(height: 24),
        ],
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(child: Center(child: ScoreGauge(score: report.compositeScore, size: 160, strokeWidth: 14))),
            const SizedBox(width: 16),
            if (report.categoryScores.isNotEmpty)
              Expanded(child: CategoryRadar(categoryScores: report.categoryScores, size: 160)),
          ],
        ),
        const SizedBox(height: 24),
        AiInsightCard(
          key: ValueKey('report-ai-insight-${report.location.displayAddress}'),
          report: report,
        ),
        const SizedBox(height: 24),
        _buildStaticCategory('Social', Icons.people_rounded, report.socialMetrics, const Color(0xFF3B82F6), report.categoryScores['Social']),
        _buildStaticCategory('Safety', Icons.shield_rounded, report.crimeMetrics, const Color(0xFF10B981), report.categoryScores['Safety']),
        _buildStaticCategory('Demographics', Icons.family_restroom_rounded, report.demographicsMetrics, const Color(0xFF8B5CF6), report.categoryScores['Demographics']),
        _buildStaticCategory('Housing', Icons.home_work_rounded, report.housingMetrics, const Color(0xFFEC4899), report.categoryScores['Housing']),
        _buildStaticCategory('Mobility', Icons.directions_car_rounded, report.mobilityMetrics, const Color(0xFF0EA5E9), report.categoryScores['Mobility']),
        _buildStaticCategory('Amenities', Icons.store_rounded, report.amenityMetrics, const Color(0xFFF59E0B), report.categoryScores['Amenities']),
        _buildStaticCategory('Environment', Icons.eco_rounded, report.environmentMetrics, const Color(0xFF22C55E), report.categoryScores['Environment']),
      ],
    );
  }

  Widget _buildStaticCategory(String title, IconData icon, List<ContextMetric> metrics, Color accentColor, double? score) {
    if (metrics.isEmpty) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: MetricCategoryCard(
        title: title,
        icon: icon,
        metrics: metrics,
        score: score,
        accentColor: accentColor,
        isExpanded: title == 'Social',
      ),
    );
  }

  bool _hasAnyMetrics(ContextReportProvider provider) {
    return provider.socialMetrics != null ||
        provider.crimeMetrics != null ||
        provider.demographicsMetrics != null ||
        provider.housingMetrics != null ||
        provider.mobilityMetrics != null ||
        provider.amenityMetrics != null ||
        provider.environmentMetrics != null;
  }

  Widget _buildHeader(BuildContext context, ContextLocation location) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [
            theme.colorScheme.surfaceContainerHigh,
            theme.colorScheme.surfaceContainerLow,
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
                  location.displayAddress,
                  style: theme.textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ],
          ),
          if (location.neighborhoodName != null) ...[
            const SizedBox(height: 8),
            Text(
              [
                location.neighborhoodName,
                location.municipalityName
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

  Widget _buildCategory(
    BuildContext context,
    String title,
    IconData icon,
    List<ContextMetric>? metrics,
    bool isLoading,
    Color accentColor,
    double? score,
  ) {
    if (isLoading) {
      return const MetricCategoryShimmer();
    }

    if (metrics == null || metrics.isEmpty) {
      return const SizedBox.shrink();
    }

    final provider = context.read<ContextReportProvider>();

    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: MetricCategoryCard(
        key: ValueKey('report-category-$title'),
        title: title,
        icon: icon,
        metrics: metrics,
        score: score,
        accentColor: accentColor,
        isExpanded: provider.isExpanded(title, defaultValue: title == 'Social'),
        onToggle: (expanded) => provider.setExpanded(title, expanded),
      ),
    );
  }

  static int childCount(ContextReport? report) => 1;
  static Widget buildChild(BuildContext context, int index, ContextReport? report) => ContextReportView(report: report);
}
