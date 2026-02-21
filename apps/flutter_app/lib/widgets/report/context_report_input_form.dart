import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:flutter_typeahead/flutter_typeahead.dart';
import 'package:latlong2/latlong.dart';
import 'package:intl/intl.dart';

import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../../core/theme/valora_typography.dart';
import '../../providers/context_report_provider.dart';
import '../../services/pdok_service.dart';
import '../valora_widgets.dart';
import 'location_picker.dart';

class ContextReportInputForm extends StatelessWidget {
  const ContextReportInputForm({
    super.key,
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
          duration: Duration(seconds: 1),
        ),
      );

      try {
        final String? address = await pdokService.reverseLookup(
          result.latitude,
          result.longitude,
        );

        if (!context.mounted) return;

        if (address != null) {
          controller.text = address;
          provider.generate(address);
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text(
                'Could not resolve an address for this location. Please try searching by text.',
              ),
              backgroundColor: ValoraColors.error,
            ),
          );
        }
      } catch (e) {
        if (!context.mounted) return;
        debugPrint('Error resolving address: $e');
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Failed to resolve address. Please try again.'),
            backgroundColor: ValoraColors.error,
          ),
        );
      }
    }
  }

  void _handleSubmit(BuildContext context, String value) {
    final trimmedValue = value.trim();
    if (trimmedValue.length >= 3) {
      provider.generate(trimmedValue);
    } else if (trimmedValue.isNotEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please enter at least 3 characters.'),
          backgroundColor: ValoraColors.warning,
        ),
      );
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
        const _ValoraHero(),

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
                    onPressed: provider.isLoading ? null : () => _pickLocation(context),
                  ),
                ],
              ),
              textInputAction: TextInputAction.search,
              onSubmitted: (val) => _handleSubmit(context, val),
            );
          },
          suggestionsCallback: (pattern) async {
            if (pattern.length < 3) return [];
            try {
              return await pdokService.search(pattern);
            } catch (e) {
              debugPrint('Error searching PDOK: $e');
              return [];
            }
          },
          itemBuilder: (context, suggestion) {
            return ListTile(
              leading: const Icon(Icons.location_on_outlined, size: 20),
              title: Text(suggestion.displayName, style: ValoraTypography.bodyMedium),
              subtitle: Text(suggestion.type, style: ValoraTypography.labelSmall),
              tileColor: theme.colorScheme.surface,
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
                    style: ValoraTypography.labelLarge.copyWith(fontWeight: FontWeight.bold),
                  ),
                  ValoraBadge(
                    label: '${provider.radiusMeters}m',
                    color: theme.colorScheme.primary,
                    size: ValoraBadgeSize.small,
                  ),
                ],
              ),
              const SizedBox(height: 12),
              SliderTheme(
                data: SliderTheme.of(context).copyWith(
                  trackHeight: 6,
                  thumbShape: const RoundSliderThumbShape(enabledThumbRadius: 10),
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
          child: ValueListenableBuilder<TextEditingValue>(
            valueListenable: controller,
            builder: (context, value, _) {
              return ValoraButton(
                label:
                    provider.isLoading ? 'Analyzing...' : 'Generate Full Report',
                isLoading: provider.isLoading,
                onPressed: provider.isLoading || value.text.isEmpty
                    ? null
                    : () => _handleSubmit(context, value.text),
                variant: ValoraButtonVariant.primary,
                isFullWidth: true,
                size: ValoraButtonSize.large,
              );
            },
          ),
        ),

        if (provider.error != null) ...[
          const SizedBox(height: 24),
          Builder(
            builder: (context) {
              debugPrint('Context Report Error: ${provider.error}');
              return ValoraEmptyState(
                icon: Icons.error_outline_rounded,
                title: 'Analysis Failed',
                subtitle: 'Something went wrong. Please try again.',
                actionLabel: 'Try Again',
                onAction: () => _handleSubmit(context, controller.text),
              );
            },
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
                style: ValoraTypography.titleMedium,
              ),
              TextButton(
                onPressed: () => _confirmClearHistory(context, provider),
                child: Text('Clear All', style: ValoraTypography.labelMedium.copyWith(color: theme.colorScheme.primary)),
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
                return SizedBox(
                  width: 200,
                  child: ValoraCard(
                    elevation: ValoraSpacing.elevationSm,
                    padding: const EdgeInsets.all(16),
                    onTap: provider.isLoading ? null : () {
                      controller.text = item.query;
                      provider.generate(item.query);
                    },
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Icon(Icons.history_rounded, size: 20, color: ValoraColors.neutral400),
                        const Spacer(),
                        Text(
                          item.query,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: ValoraTypography.labelMedium.copyWith(fontWeight: FontWeight.w600),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          _formatDate(item.timestamp),
                          style: ValoraTypography.labelSmall.copyWith(color: ValoraColors.neutral500),
                        ),
                      ],
                    ),
                  ).animate(delay: (index * 50).ms).fadeIn().slideX(begin: 0.1),
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

    if (midnightDate == today) return 'Today';
    if (midnightDate == yesterday) return 'Yesterday';

    return DateFormat('dd/MM/yyyy').format(date);
  }
}

class _ValoraHero extends StatelessWidget {
  const _ValoraHero();

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
