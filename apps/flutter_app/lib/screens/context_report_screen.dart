import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../models/context_report.dart';
import '../providers/context_report_provider.dart';
import '../services/api_service.dart';
import '../widgets/report/report_widgets.dart';
import '../widgets/valora_widgets.dart';

class ContextReportScreen extends StatefulWidget {
  const ContextReportScreen({super.key});

  @override
  State<ContextReportScreen> createState() => _ContextReportScreenState();
}

class _ContextReportScreenState extends State<ContextReportScreen> {
  final TextEditingController _inputController = TextEditingController();

  @override
  void dispose() {
    _inputController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<ContextReportProvider>(
      create: (_) => ContextReportProvider(apiService: context.read<ApiService>()),
      child: Consumer<ContextReportProvider>(
        builder: (context, provider, _) {
          return Scaffold(
            appBar: AppBar(
              title: const Text('Property Analytics'),
              actions: [
                if (provider.report != null)
                  IconButton(
                    tooltip: 'New Report',
                    onPressed: provider.clear,
                    icon: const Icon(Icons.refresh_rounded),
                  ),
              ],
            ),
            body: SafeArea(
              child: provider.report != null
                  ? _ReportContent(report: provider.report!)
                  : _InputForm(
                      controller: _inputController,
                      provider: provider,
                    ),
            ),
          );
        },
      ),
    );
  }
}

class _InputForm extends StatelessWidget {
  const _InputForm({
    required this.controller,
    required this.provider,
  });

  final TextEditingController controller;
  final ContextReportProvider provider;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        // Hero section
        Container(
          padding: const EdgeInsets.all(24),
          decoration: BoxDecoration(
            gradient: LinearGradient(
              colors: [
                theme.colorScheme.primaryContainer,
                theme.colorScheme.primaryContainer.withValues(alpha: 0.5),
              ],
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
            ),
            borderRadius: BorderRadius.circular(20),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(
                Icons.analytics_rounded,
                size: 48,
                color: theme.colorScheme.primary,
              ),
              const SizedBox(height: 16),
              Text(
                'Neighborhood Analytics',
                style: theme.textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Get comprehensive insights about any Dutch address including demographics, safety, amenities, and environmental data.',
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 24),
        // Search field
        ValueListenableBuilder<TextEditingValue>(
          valueListenable: controller,
          builder: (context, value, _) {
            return TextField(
              controller: controller,
              decoration: InputDecoration(
                hintText: 'Enter address (e.g. Damrak 1 Amsterdam)',
                filled: true,
                fillColor: theme.colorScheme.surfaceContainerLow,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(14),
                  borderSide: BorderSide.none,
                ),
                prefixIcon: const Icon(Icons.search_rounded),
                suffixIcon: value.text.isNotEmpty
                    ? IconButton(
                        icon: const Icon(Icons.clear_rounded),
                        onPressed: () => controller.clear(),
                      )
                    : null,
              ),
              textInputAction: TextInputAction.search,
              onSubmitted: (_) => provider.generate(controller.text),
            );
          },
        ),
        const SizedBox(height: 20),
        // Radius slider
        Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: theme.colorScheme.surfaceContainerLow,
            borderRadius: BorderRadius.circular(14),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'Search Radius',
                    style: theme.textTheme.labelLarge,
                  ),
                  Text(
                    '${provider.radiusMeters}m',
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w600,
                      color: theme.colorScheme.primary,
                    ),
                  ),
                ],
              ),
              SliderTheme(
                data: SliderTheme.of(context).copyWith(
                  trackHeight: 6,
                  thumbShape: const RoundSliderThumbShape(enabledThumbRadius: 10),
                ),
                child: Slider(
                  min: 200,
                  max: 5000,
                  divisions: 24,
                  value: provider.radiusMeters.toDouble(),
                  onChanged: (value) => provider.setRadiusMeters(value.round()),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 24),
        // Generate button
        SizedBox(
          height: 56,
          child: FilledButton.icon(
            onPressed: provider.isLoading
                ? null
                : () => provider.generate(controller.text),
            style: FilledButton.styleFrom(
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(14),
              ),
            ),
            icon: provider.isLoading
                ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: Colors.white,
                    ),
                  )
                : const Icon(Icons.search_rounded),
            label: Text(
              provider.isLoading ? 'Analyzing...' : 'Generate Report',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
            ),
          ),
        ),
        if (provider.error != null) ...[
          const SizedBox(height: 16),
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: theme.colorScheme.errorContainer,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Row(
              children: [
                Icon(Icons.error_outline, color: theme.colorScheme.error),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    provider.error!,
                    style: TextStyle(color: theme.colorScheme.onErrorContainer),
                  ),
                ),
              ],
            ),
          ),
        ],
        // Recent Searches
        if (provider.history.isNotEmpty) ...[
          const SizedBox(height: 32),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Recent Searches',
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              TextButton(
                onPressed: () => _confirmClearHistory(context, provider),
                child: const Text('Clear All'),
              ),
            ],
          ),
          const SizedBox(height: 8),
          ...provider.history.map((item) {
            return ListTile(
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.history_rounded),
              title: Text(item.query),
              subtitle: Text(
                _formatDate(item.timestamp),
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
              trailing: IconButton(
                icon: const Icon(Icons.close_rounded, size: 20),
                onPressed: () => provider.removeFromHistory(item.query),
              ),
              onTap: () {
                controller.text = item.query;
                provider.generate(item.query);
              },
            );
          }),
        ],
      ],
    );
  }

  Future<void> _confirmClearHistory(
    BuildContext context,
    ContextReportProvider provider,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => ValoraDialog(
        title: 'Clear History?',
        actions: [
          ValoraButton(
            label: 'Cancel',
            variant: ValoraButtonVariant.ghost,
            onPressed: () => Navigator.pop(context, false),
          ),
          ValoraButton(
            label: 'Clear',
            variant: ValoraButtonVariant.primary,
            onPressed: () => Navigator.pop(context, true),
          ),
        ],
        child: const Text(
          'Are you sure you want to clear your search history?',
        ),
      ),
    );

    if (confirmed == true) {
      await provider.clearHistory();
    }
  }

  String _formatDate(DateTime date) {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final yesterday = today.subtract(const Duration(days: 1));
    final checkDate = DateTime(date.year, date.month, date.day);

    if (checkDate == today) {
      return 'Today';
    } else if (checkDate == yesterday) {
      return 'Yesterday';
    } else {
      return '${date.day}/${date.month}/${date.year}';
    }
  }
}

class _ReportContent extends StatelessWidget {
  const _ReportContent({required this.report});

  final ContextReport report;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // Header with address
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
                  [report.location.neighborhoodName, report.location.municipalityName]
                      .where((s) => s != null && s.isNotEmpty)
                      .join(', '),
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                  ),
                ),
              ],
            ],
          ),
        ),
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
            if (report.categoryScores.isNotEmpty)
              Expanded(
                child: CategoryRadar(
                  categoryScores: report.categoryScores,
                  size: 160,
                ),
              ),
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
