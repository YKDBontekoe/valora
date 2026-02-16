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
import '../core/theme/valora_colors.dart';
import '../core/theme/valora_animations.dart';

class ContextReportScreen extends StatefulWidget {
  const ContextReportScreen({super.key, this.pdokService});
  final PdokService? pdokService;

  @override
  State<ContextReportScreen> createState() => _ContextReportScreenState();
}

class _ContextReportScreenState extends State<ContextReportScreen> {
  final TextEditingController _inputController = TextEditingController();
  late final PdokService _pdokService;

  @override
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
      create: (_) => ContextReportProvider(apiService: context.read<ApiService>()),
      child: Consumer<ContextReportProvider>(
        builder: (context, provider, _) {
          final report = provider.report;
          final isLoading = provider.isLoading;

          return Scaffold(
            appBar: AppBar(
              title: const Text('Property Analytics'),
              actions: [
                if (report != null)
                  IconButton(
                    tooltip: 'New Report',
                    onPressed: provider.clear,
                    icon: const Icon(Icons.refresh_rounded),
                  ),
              ],
            ),
            body: SafeArea(
              child: AnimatedSwitcher(
                duration: ValoraAnimations.normal,
                child: isLoading
                    ? const ContextReportSkeleton(key: ValueKey('loading'))
                    : report != null
                        ? ListView.builder(
                            key: const ValueKey('report-list'),
                            padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 4),
                            itemCount: ContextReportView.childCount(report),
                            itemBuilder: (context, index) => Padding(
                              padding: const EdgeInsets.symmetric(horizontal: 16),
                              child: ContextReportView.buildChild(
                                context,
                                index,
                                report,
                              ),
                            ),
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
        const SnackBar(content: Text('Resolving address...'), duration: Duration(seconds: 1)),
      );

      final String? address = await pdokService.reverseLookup(result.latitude, result.longitude);

      if (!context.mounted) return;

      if (address != null) {
        controller.text = address;
        provider.generate(address);
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Could not resolve an address for this location. Please try searching by text.'),
            backgroundColor: ValoraColors.error,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ListView(
      key: const ValueKey('input-form'),
      padding: const EdgeInsets.all(24),
      children: [
        // Hero section
        Container(
          padding: const EdgeInsets.all(28),
          decoration: BoxDecoration(
            gradient: LinearGradient(
              colors: [
                theme.colorScheme.primaryContainer,
                theme.colorScheme.primaryContainer.withValues(alpha: 0.6),
              ],
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
            ),
            borderRadius: BorderRadius.circular(24),
            boxShadow: [
              BoxShadow(
                color: theme.colorScheme.primary.withValues(alpha: 0.1),
                blurRadius: 20,
                offset: const Offset(0, 10),
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.3),
                  borderRadius: BorderRadius.circular(16),
                ),
                child: Icon(
                  Icons.analytics_rounded,
                  size: 32,
                  color: theme.colorScheme.primary,
                ),
              ),
              const SizedBox(height: 20),
              Text(
                'Property Analytics',
                style: theme.textTheme.headlineMedium?.copyWith(
                  fontWeight: FontWeight.w800,
                  letterSpacing: -0.5,
                ),
              ),
              const SizedBox(height: 12),
              Text(
                'Get deep neighborhood insights and environmental data for any Dutch address.',
                style: theme.textTheme.bodyLarge?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant.withValues(alpha: 0.8),
                  height: 1.4,
                ),
              ),
            ],
          ),
        ).animate().fadeIn(duration: 400.ms).slideY(begin: 0.1),

        const SizedBox(height: 32),

        Text(
          'Search Property',
          style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 12),

        // Search field with TypeAhead
        TypeAheadField<PdokSuggestion>(
          controller: controller,
          builder: (context, controller, focusNode) => TextField(
            controller: controller,
            focusNode: focusNode,
            decoration: InputDecoration(
              hintText: 'Address or location...',
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
                    onPressed: provider.isLoading ? null : () => _pickLocation(context),
                  ),
                ],
              ),
              filled: true,
              fillColor: theme.colorScheme.surfaceContainerLow,
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(16),
                borderSide: BorderSide.none,
              ),
              contentPadding: const EdgeInsets.symmetric(vertical: 18, horizontal: 20),
            ),
            textInputAction: TextInputAction.search,
            onSubmitted: (val) => val.isNotEmpty ? provider.generate(val) : null,
          ),
          suggestionsCallback: (pattern) async {
            if (pattern.length < 3) return [];
            return await pdokService.search(pattern);
          },
          itemBuilder: (context, suggestion) {
            return ListTile(
              leading: const Icon(Icons.location_on_outlined, size: 20),
              title: Text(suggestion.displayName),
              subtitle: Text(suggestion.type, style: const TextStyle(fontSize: 12)),
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
                    style: theme.textTheme.labelLarge?.copyWith(fontWeight: FontWeight.bold),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                    decoration: BoxDecoration(
                      color: theme.colorScheme.primary.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Text(
                      '${provider.radiusMeters}m',
                      style: theme.textTheme.bodyMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                        color: theme.colorScheme.primary,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              SliderTheme(
                data: SliderTheme.of(context).copyWith(
                  trackHeight: 8,
                  thumbShape: const RoundSliderThumbShape(enabledThumbRadius: 12),
                  overlayShape: const RoundSliderOverlayShape(overlayRadius: 20),
                  activeTrackColor: theme.colorScheme.primary,
                  inactiveTrackColor: theme.colorScheme.primary.withValues(alpha: 0.1),
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

        const SizedBox(height: 32),

        // Generate button
        SizedBox(
          height: 60,
          child: ValoraButton(
            label: provider.isLoading ? 'Analyzing...' : 'Generate Full Report',
            isLoading: provider.isLoading,
            onPressed: provider.isLoading || controller.text.isEmpty
                ? null
                : () => provider.generate(controller.text),
            variant: ValoraButtonVariant.primary,
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
        if (provider.history.isNotEmpty) ...[
          const SizedBox(height: 40),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Recent Searches',
                style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
              ),
              TextButton(
                onPressed: () => _confirmClearHistory(context, provider),
                child: const Text('Clear All'),
              ),
            ],
          ),
          const SizedBox(height: 16),
          SizedBox(
            height: 120,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: provider.history.length,
              separatorBuilder: (context, index) => const SizedBox(width: 12),
              itemBuilder: (context, index) {
                final item = provider.history[index];
                return GestureDetector(
                  onTap: provider.isLoading ? null : () {
                    controller.text = item.query;
                    provider.generate(item.query);
                  },
                  child: Container(
                    width: 200,
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: theme.colorScheme.surface,
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: theme.colorScheme.outlineVariant),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.05),
                          blurRadius: 10,
                          offset: const Offset(0, 4),
                        ),
                      ],
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Icon(Icons.history_rounded, size: 20, color: ValoraColors.neutral400),
                        const Spacer(),
                        Text(
                          item.query,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          _formatDate(item.timestamp),
                          style: theme.textTheme.bodySmall?.copyWith(color: ValoraColors.neutral500),
                        ),
                      ],
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ],
    );
  }

  Future<void> _confirmClearHistory(BuildContext context, ContextReportProvider provider) async {
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
        child: const Text('Are you sure you want to clear your search history?'),
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
