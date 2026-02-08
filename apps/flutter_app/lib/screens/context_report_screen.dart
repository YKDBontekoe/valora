import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/theme/valora_colors.dart';
import '../core/theme/valora_typography.dart';
import '../models/context_report.dart';
import '../providers/context_report_provider.dart';
import '../services/api_service.dart';
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
              title: const Text('Location Context'),
              actions: [
                IconButton(
                  tooltip: 'Clear',
                  onPressed: provider.clear,
                  icon: const Icon(Icons.refresh_rounded),
                ),
              ],
            ),
            body: SafeArea(
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  Text(
                    'Paste any address or listing link to generate public-data context.',
                    style: ValoraTypography.bodyMedium.copyWith(
                      color: Theme.of(context).colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _inputController,
                    decoration: const InputDecoration(
                      hintText: 'e.g. Damrak 1 Amsterdam or listing URL',
                      border: OutlineInputBorder(),
                      prefixIcon: Icon(Icons.location_on_rounded),
                    ),
                    textInputAction: TextInputAction.search,
                    onSubmitted: (_) => provider.generate(_inputController.text),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    'Radius: ${provider.radiusMeters}m',
                    style: ValoraTypography.labelLarge,
                  ),
                  Slider(
                    min: 200,
                    max: 5000,
                    divisions: 24,
                    value: provider.radiusMeters.toDouble(),
                    onChanged: (value) => provider.setRadiusMeters(value.round()),
                  ),
                  const SizedBox(height: 8),
                  FilledButton.icon(
                    onPressed: provider.isLoading ? null : () => provider.generate(_inputController.text),
                    icon: provider.isLoading
                        ? const SizedBox(
                            width: 16,
                            height: 16,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Icon(Icons.analytics_rounded),
                    label: Text(provider.isLoading ? 'Generating...' : 'Generate Report'),
                  ),
                  if (provider.error != null) ...[
                    const SizedBox(height: 24),
                    ValoraEmptyState(
                      icon: Icons.error_outline_rounded,
                      title: 'Report Generation Failed',
                      subtitle: provider.error,
                      action: ValoraButton(
                        label: 'Retry',
                        onPressed: () => provider.generate(_inputController.text),
                      ),
                    ),
                  ],
                  if (provider.report != null) ...[
                    const SizedBox(height: 20),
                    _LocationCard(location: provider.report!.location, score: provider.report!.compositeScore),
                    const SizedBox(height: 12),
                    _MetricSection(title: 'Social', metrics: provider.report!.socialMetrics),
                    const SizedBox(height: 12),
                    _MetricSection(title: 'Safety', metrics: provider.report!.safetyMetrics),
                    const SizedBox(height: 12),
                    _MetricSection(title: 'Amenities', metrics: provider.report!.amenityMetrics),
                    const SizedBox(height: 12),
                    _MetricSection(title: 'Environment', metrics: provider.report!.environmentMetrics),
                    if (provider.report!.warnings.isNotEmpty) ...[
                      const SizedBox(height: 12),
                      Card(
                        child: Padding(
                          padding: const EdgeInsets.all(12),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text('Warnings', style: ValoraTypography.titleSmall),
                              const SizedBox(height: 8),
                              for (final warning in provider.report!.warnings)
                                Padding(
                                  padding: const EdgeInsets.only(bottom: 4),
                                  child: Text('• $warning'),
                                ),
                            ],
                          ),
                        ),
                      ),
                    ],
                  ],
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}

class _LocationCard extends StatelessWidget {
  const _LocationCard({required this.location, required this.score});

  final ContextLocation location;
  final double score;

  @override
  Widget build(BuildContext context) {
    return Card(
      color: ValoraColors.primary.withValues(alpha: 0.08),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(location.displayAddress, style: ValoraTypography.titleMedium),
            const SizedBox(height: 8),
            Text('Composite score: ${score.toStringAsFixed(1)} / 100'),
            if (location.neighborhoodName != null)
              Text('Neighborhood: ${location.neighborhoodName}'),
            if (location.municipalityName != null)
              Text('Municipality: ${location.municipalityName}'),
          ],
        ),
      ),
    );
  }
}

class _MetricSection extends StatelessWidget {
  const _MetricSection({required this.title, required this.metrics});

  final String title;
  final List<ContextMetric> metrics;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: ValoraTypography.titleSmall),
            const SizedBox(height: 8),
            if (metrics.isEmpty)
              Text(
                'No data available.',
                style: TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant),
              )
            else
              for (final metric in metrics)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: Text(
                          metric.label,
                          style: ValoraTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Text(_valueText(metric)),
                    ],
                  ),
                ),
          ],
        ),
      ),
    );
  }

  String _valueText(ContextMetric metric) {
    if (metric.value == null) {
      return metric.note ?? '-';
    }

    final String numeric = metric.value!.toStringAsFixed(metric.value! % 1 == 0 ? 0 : 1);
    return metric.unit == null ? numeric : '$numeric ${metric.unit}';
  }
}
