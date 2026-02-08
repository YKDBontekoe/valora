import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';

import '../core/theme/valora_colors.dart';
import '../core/theme/valora_spacing.dart';
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
                padding: const EdgeInsets.all(ValoraSpacing.md),
                children: [
                  Text(
                    'Paste any address or listing link to generate public-data context.',
                    style: ValoraTypography.bodyMedium.copyWith(
                      color: Theme.of(context).colorScheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: ValoraSpacing.md),
                  ValoraTextField(
                    controller: _inputController,
                    label: 'Address or URL',
                    hint: 'e.g. Damrak 1 Amsterdam or listing URL',
                    prefixIcon: Icons.location_on_rounded,
                    textInputAction: TextInputAction.search,
                    onFieldSubmitted: (_) => provider.generate(_inputController.text),
                  ),
                  const SizedBox(height: ValoraSpacing.md),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                       Text(
                        'Radius',
                        style: ValoraTypography.labelLarge,
                      ),
                      Text(
                        '${provider.radiusMeters}m',
                        style: ValoraTypography.labelLarge.copyWith(fontWeight: FontWeight.bold),
                      ),
                    ],
                  ),
                  Slider(
                    min: 200,
                    max: 5000,
                    divisions: 24,
                    value: provider.radiusMeters.toDouble(),
                    onChanged: (value) => provider.setRadiusMeters(value.round()),
                  ),
                  const SizedBox(height: ValoraSpacing.sm),
                  ValoraButton(
                    onPressed: provider.isLoading ? null : () => provider.generate(_inputController.text),
                    icon: Icons.analytics_rounded,
                    label: 'Generate Report',
                    isLoading: provider.isLoading,
                    isFullWidth: true,
                  ),
                  if (provider.error != null) ...[
                    const SizedBox(height: ValoraSpacing.md),
                    ValoraCard(
                      backgroundColor: ValoraColors.error.withValues(alpha: 0.1),
                      child: Row(
                         children: [
                           const Icon(Icons.error_outline_rounded, color: ValoraColors.error),
                           const SizedBox(width: ValoraSpacing.sm),
                           Expanded(
                             child: Text(
                                provider.error!,
                                style: TextStyle(color: ValoraColors.errorDark),
                              ),
                           ),
                         ],
                      )
                    ).animate().fade().slideY(begin: 0.2, end: 0),
                  ],
                  if (provider.report != null) ...[
                    const SizedBox(height: ValoraSpacing.xl),
                    _LocationCard(location: provider.report!.location, score: provider.report!.compositeScore)
                        .animate().fade().slideY(begin: 0.1, end: 0, delay: 100.ms),
                    const SizedBox(height: ValoraSpacing.md),
                    _MetricSection(title: 'Social', metrics: provider.report!.socialMetrics)
                        .animate().fade().slideY(begin: 0.1, end: 0, delay: 200.ms),
                    const SizedBox(height: ValoraSpacing.md),
                    _MetricSection(title: 'Safety', metrics: provider.report!.safetyMetrics)
                        .animate().fade().slideY(begin: 0.1, end: 0, delay: 300.ms),
                    const SizedBox(height: ValoraSpacing.md),
                    _MetricSection(title: 'Amenities', metrics: provider.report!.amenityMetrics)
                        .animate().fade().slideY(begin: 0.1, end: 0, delay: 400.ms),
                    const SizedBox(height: ValoraSpacing.md),
                    _MetricSection(title: 'Environment', metrics: provider.report!.environmentMetrics)
                         .animate().fade().slideY(begin: 0.1, end: 0, delay: 500.ms),
                    if (provider.report!.warnings.isNotEmpty) ...[
                      const SizedBox(height: ValoraSpacing.md),
                      ValoraCard(
                        padding: const EdgeInsets.all(ValoraSpacing.md),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                const Icon(Icons.warning_amber_rounded, color: ValoraColors.warning),
                                const SizedBox(width: ValoraSpacing.sm),
                                Text('Warnings', style: ValoraTypography.titleMedium),
                              ],
                            ),
                            const SizedBox(height: ValoraSpacing.sm),
                            for (final warning in provider.report!.warnings)
                              Padding(
                                padding: const EdgeInsets.only(bottom: 4),
                                child: Text('• $warning'),
                              ),
                          ],
                        ),
                      ).animate().fade().slideY(begin: 0.1, end: 0, delay: 600.ms),
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
    return ValoraCard(
      backgroundColor: ValoraColors.primary.withValues(alpha: 0.08),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(location.displayAddress, style: ValoraTypography.titleMedium),
          const SizedBox(height: ValoraSpacing.sm),
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: ValoraColors.primary,
                  borderRadius: BorderRadius.circular(ValoraSpacing.radiusSm),
                ),
                child: Text(
                  score.toStringAsFixed(1),
                  style: ValoraTypography.labelMedium.copyWith(color: Colors.white),
                ),
              ),
              const SizedBox(width: ValoraSpacing.sm),
              Text('Composite Score', style: ValoraTypography.bodyMedium),
            ],
          ),
          if (location.neighborhoodName != null || location.municipalityName != null) ...[
             const SizedBox(height: ValoraSpacing.md),
             if (location.neighborhoodName != null)
              Text('Neighborhood: ${location.neighborhoodName}', style: ValoraTypography.bodySmall),
             if (location.municipalityName != null)
              Text('Municipality: ${location.municipalityName}', style: ValoraTypography.bodySmall),
          ]
        ],
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
    return ValoraCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: ValoraTypography.titleMedium),
          const SizedBox(height: ValoraSpacing.md),
          if (metrics.isEmpty)
            Text(
              'No data available.',
              style: TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant),
            )
          else
            for (final metric in metrics)
              Padding(
                padding: const EdgeInsets.only(bottom: ValoraSpacing.sm),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Text(
                        metric.label,
                        style: ValoraTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600),
                      ),
                    ),
                    const SizedBox(width: ValoraSpacing.md),
                    Text(_valueText(metric), style: ValoraTypography.bodyMedium),
                  ],
                ),
              ),
        ],
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
