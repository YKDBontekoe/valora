import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:flutter_typeahead/flutter_typeahead.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:latlong2/latlong.dart';
import 'package:intl/intl.dart';

import '../services/api_service.dart';
import '../services/pdok_service.dart';
import '../providers/context_report_provider.dart';
import '../widgets/report/context_report_view.dart';
import '../widgets/report/context_report_skeleton.dart';
import '../widgets/report/location_picker.dart';
import '../widgets/valora_widgets.dart';
import '../widgets/report/comparison_view.dart';
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_animations.dart';
import '../core/theme/valora_typography.dart';
import '../core/theme/valora_spacing.dart';

class ContextReportScreen extends StatefulWidget {
  const ContextReportScreen({super.key, this.pdokService});
  final PdokService? pdokService;

  @override
  State<ContextReportScreen> createState() => _ContextReportScreenState();
}

class _ContextReportScreenState extends State<ContextReportScreen> {
  final TextEditingController _inputController = TextEditingController();
  late final PdokService _pdokService;
  bool _isComparisonMode = false;

  @override
  void initState() {
    super.initState();
    _pdokService = widget.pdokService ?? PdokService();
  }

  @override
  void dispose() {
    _inputController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<ContextReportProvider>(
      create: (_) =>
          ContextReportProvider(apiService: context.read<ApiService>()),
      child: Consumer<ContextReportProvider>(
        builder: (context, provider, _) {
          final hasReport = provider.report != null;
          final isLoading = provider.isLoading;
          final comparisonCount = provider.comparisonIds.length;

          return Scaffold(
            appBar: AppBar(
              title: Text(_isComparisonMode ? 'Comparison' : 'Property Analytics'),
              leading: _isComparisonMode ? IconButton(
                icon: const Icon(Icons.arrow_back),
                onPressed: () => setState(() => _isComparisonMode = false),
              ) : null,
              actions: [
                if (!_isComparisonMode && hasReport) ...[
                   IconButton(
                     tooltip: provider.isComparing(provider.report!.location.query, provider.radiusMeters)
                       ? 'Remove from Compare'
                       : 'Add to Compare',
                     icon: Icon(
                       provider.isComparing(provider.report!.location.query, provider.radiusMeters)
                         ? Icons.playlist_add_check_rounded
                         : Icons.playlist_add_rounded
                     ),
                     onPressed: () {
                       provider.toggleComparison(provider.report!.location.query, provider.radiusMeters);
                     },
                   ),
                   IconButton(
                    tooltip: 'New Report',
                    onPressed: provider.clear,
                    icon: const Icon(Icons.refresh_rounded),
                  ),
                ],
                if (_isComparisonMode)
                   IconButton(
                    tooltip: 'Clear Comparison',
                    onPressed: provider.clearComparison,
                    icon: const Icon(Icons.delete_sweep_rounded),
                  ),
              ],
            ),
            floatingActionButton: comparisonCount > 0 && !_isComparisonMode ? FloatingActionButton.extended(
              onPressed: () => setState(() => _isComparisonMode = true),
              label: Text('Compare ($comparisonCount)'),
              icon: const Icon(Icons.compare_arrows_rounded),
            ) : null,
            body: SafeArea(
              child: AnimatedSwitcher(
                duration: ValoraAnimations.normal,
                child: _isComparisonMode
                  ? const ComparisonView()
                  : isLoading
                    ? const ContextReportSkeleton(key: ValueKey('loading'))
                    : hasReport
                        ? Selector<ContextReportProvider, dynamic>(
                            selector: (_, p) => p.report,
                            builder: (context, report, _) {
                              return ListView.builder(
                                key: const ValueKey('report-list'),
                                padding: const EdgeInsets.symmetric(
                                    horizontal: 4, vertical: 4),
                                itemCount: ContextReportView.childCount(report),
                                itemBuilder: (context, index) => Padding(
                                  padding: const EdgeInsets.symmetric(
                                      horizontal: 16),
                                  child: ContextReportView.buildChild(
                                    context,
                                    index,
                                    report,
                                  ),
                                ),
                              );
                            },
                          )
                        : _InputForm(
                            controller: _inputController,
                            provider: provider,
                            pdokService: _pdokService,
                          ),
              ),
            ),
          );
        },
      ),
    );
  }
}

class _HeroSection extends StatelessWidget {
  const _HeroSection();

  @override
  Widget build(BuildContext context) {
    return ValoraCard(
      padding: const EdgeInsets.all(28),
      gradient: ValoraColors.heroGradient,
      elevation: ValoraSpacing.elevationLg,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.2),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(
                color: Colors.white.withValues(alpha: 0.3),
                width: 1,
              ),
            ),
            child: const Icon(
              Icons.analytics_rounded,
              size: 32,
              color: Colors.white,
            ),
          ),
          const SizedBox(height: 20),
          Text(
            'Property Analytics',
            style: ValoraTypography.headlineMedium.copyWith(
              color: Colors.white,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 12),
          Text(
            'Get deep neighborhood insights and environmental data for any Dutch address.',
            style: ValoraTypography.bodyLarge.copyWith(
              color: Colors.white.withValues(alpha: 0.9),
              height: 1.4,
            ),
          ),
        ],
      ),
    ).animate().fadeIn(duration: 400.ms).slideY(begin: 0.1);
  }
}

class _InputForm extends StatelessWidget {
  const _InputForm({
    required this.controller,
    required this.provider,
    required this.pdokService,
  });

  final TextEditingController controller;
  final ContextReportProvider provider;
  final PdokService pdokService;

  Future<void> _pickLocation(BuildContext context) async {
    final LatLng? result = await Navigator.push<LatLng>(
      context,
      MaterialPageRoute(builder: (context) => const LocationPicker()),
    );

    if (!context.mounted) return;
    if (result != null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
            content: Text('Resolving address...'),
            duration: Duration(seconds: 1)),
      );

      final String? address =
          await pdokService.reverseLookup(result.latitude, result.longitude);

      if (!context.mounted) return;

      if (address != null) {
        controller.text = address;
        provider.generate(address);
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
                'Could not resolve an address for this location. Please try searching by text.'),
            backgroundColor: ValoraColors.error,
          ),
        );
      }
    }
  }

  Future<void> _handleSubmit(BuildContext context, String value) async {
    FocusScope.of(context).unfocus();
    await provider.generate(value);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return SingleChildScrollView(
      key: const ValueKey('input-form'),
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const _HeroSection(),
          const SizedBox(height: 32),

          Text(
            'Search Property',
            style: ValoraTypography.titleMedium,
          ),
          const SizedBox(height: 12),

          // Search field with TypeAhead
          TypeAheadField<PdokSuggestion>(
            controller: controller,
            builder: (context, controller, focusNode) {
              return ValoraTextField(
                controller: controller,
                focusNode: focusNode,
                hint: 'Address or location...',
                prefixIcon: const Icon(Icons.search_rounded),
                suffixIcon: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    if (controller.text.isNotEmpty)
                      IconButton(
                        icon: const Icon(Icons.clear_rounded),
                        onPressed: () => controller.clear(),
                      ),
                    IconButton(
                      tooltip: 'Pick on Map',
                      icon: const Icon(Icons.map_outlined),
                      onPressed: provider.isLoading
                          ? null
                          : () => _pickLocation(context),
                    ),
                  ],
                ),
                textInputAction: TextInputAction.search,
                onSubmitted: (val) => _handleSubmit(context, val),
              );
            },
            debounceDuration: const Duration(milliseconds: 300),
            suggestionsCallback: (pattern) async {
              if (pattern.length < 3) return [];
              return await pdokService.search(pattern);
            },
            itemBuilder: (context, suggestion) {
              return ListTile(
                leading: const Icon(Icons.location_on_outlined, size: 20),
                title: Text(suggestion.displayName,
                    style: ValoraTypography.bodyMedium),
                subtitle:
                    Text(suggestion.type, style: ValoraTypography.labelSmall),
              );
            },
            onSelected: (suggestion) {
              controller.text = suggestion.displayName;
              provider.generate(suggestion.displayName);
            },
          ),

          const SizedBox(height: 24),

          // Radius Selector
          ValoraCard(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'Search Radius',
                      style: ValoraTypography.labelLarge
                          .copyWith(fontWeight: FontWeight.bold),
                    ),
                    Selector<ContextReportProvider, int>(
                      selector: (_, p) => p.radiusMeters,
                      builder: (context, radiusMeters, _) {
                        return ValoraBadge(
                          label: '${radiusMeters}m',
                          color: theme.colorScheme.primary,
                          size: ValoraBadgeSize.small,
                        );
                      },
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                Selector<ContextReportProvider, int>(
                  selector: (_, p) => p.radiusMeters,
                  builder: (context, radiusMeters, _) {
                    return SliderTheme(
                      data: SliderTheme.of(context).copyWith(
                        trackHeight: 6,
                        thumbShape:
                            const RoundSliderThumbShape(enabledThumbRadius: 10),
                        overlayShape:
                            const RoundSliderOverlayShape(overlayRadius: 20),
                        activeTrackColor: theme.colorScheme.primary,
                        inactiveTrackColor:
                            theme.colorScheme.primary.withValues(alpha: 0.1),
                      ),
                      child: Slider(
                        min: 200,
                        max: 5000,
                        divisions: 24,
                        value: radiusMeters.toDouble(),
                        onChanged: (value) =>
                            provider.setRadiusMeters(value.round()),
                      ),
                    );
                  },
                ),
              ],
            ),
          ),

          const SizedBox(height: 32),

          // Generate button
          SizedBox(
            height: 60,
            child: ValoraButton(
              label: provider.isLoading ? 'Analyzing...' : 'Generate Full Report',
              isLoading: provider.isLoading,
              onPressed: provider.isLoading || controller.text.isEmpty
                  ? null
                  : () => _handleSubmit(context, controller.text),
              variant: ValoraButtonVariant.primary,
              isFullWidth: true,
              size: ValoraButtonSize.large,
            ),
          ),

          if (provider.error != null) ...[
            const SizedBox(height: 24),
            ValoraEmptyState(
              icon: Icons.error_outline_rounded,
              title: 'Analysis Failed',
              subtitle: provider.error,
              actionLabel: 'Try Again',
              onAction: () => provider.generate(controller.text),
            ),
          ],

          // Recent Searches - Cards
          Selector<ContextReportProvider, List<dynamic>>(
              selector: (_, p) => p.history,
              builder: (context, history, _) {
                if (history.isEmpty) return const SizedBox.shrink();
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const SizedBox(height: 40),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          'Recent Searches',
                          style: ValoraTypography.titleMedium,
                        ),
                        TextButton(
                          onPressed: () =>
                              _confirmClearHistory(context, provider),
                          child: Text('Clear All',
                              style: ValoraTypography.labelMedium
                                  .copyWith(color: theme.colorScheme.primary)),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    SizedBox(
                      height: 120,
                      child: ListView.separated(
                        scrollDirection: Axis.horizontal,
                        itemCount: history.length,
                        separatorBuilder: (context, index) =>
                            const SizedBox(width: 12),
                        itemBuilder: (context, index) {
                          final item = history[index];
                          return SizedBox(
                            width: 200,
                            child: ValoraCard(
                              padding: const EdgeInsets.all(16),
                              onTap: provider.isLoading
                                  ? null
                                  : () {
                                      controller.text = item.query;
                                      provider.generate(item.query);
                                    },
                              child: Stack(
                                children: [
                                  Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      const Icon(Icons.history_rounded,
                                          size: 20, color: ValoraColors.neutral400),
                                      const Spacer(),
                                      Text(
                                        item.query,
                                        maxLines: 2,
                                        overflow: TextOverflow.ellipsis,
                                        style: ValoraTypography.labelMedium
                                            .copyWith(fontWeight: FontWeight.w600),
                                      ),
                                      const SizedBox(height: 4),
                                      Text(
                                        _formatDate(item.timestamp),
                                        style: ValoraTypography.labelSmall
                                            .copyWith(color: ValoraColors.neutral500),
                                      ),
                                    ],
                                  ),
                                  Positioned(
                                    top: -12,
                                    right: -12,
                                    child: IconButton(
                                      tooltip: provider.isComparing(item.query, provider.radiusMeters)
                                        ? 'Remove from Compare'
                                        : 'Add to Compare',
                                      icon: Icon(
                                        provider.isComparing(item.query, provider.radiusMeters)
                                          ? Icons.playlist_add_check_rounded
                                          : Icons.playlist_add_rounded,
                                        size: 20,
                                        color: theme.colorScheme.primary,
                                      ),
                                      onPressed: () => provider.toggleComparison(item.query, provider.radiusMeters),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          );
                        },
                      ),
                    ),
                  ],
                );
              }),
        ],
      ),
    );
  }

  Future<void> _confirmClearHistory(
      BuildContext context, ContextReportProvider provider) async {
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
        child:
            const Text('Are you sure you want to clear your search history?'),
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
    final midnightDate = DateTime(date.year, date.month, date.day);

    if (midnightDate.isAfter(today)) {
      return DateFormat('dd/MM/yyyy').format(date);
    }

    if (midnightDate == today) return 'Today';
    if (midnightDate == yesterday) return 'Yesterday';

    return DateFormat('dd/MM/yyyy').format(date);
  }
}
