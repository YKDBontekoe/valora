import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import 'package:flutter_typeahead/flutter_typeahead.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:latlong2/latlong.dart';
import 'package:intl/intl.dart';

import '../repositories/context_report_repository.dart';
import '../repositories/ai_repository.dart';
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
  const ContextReportScreen({super.key, this.pdokService, this.onFabChanged});
  final PdokService? pdokService;
  /// Called whenever the desired FAB widget changes (or becomes null).
  final ValueChanged<Widget?>? onFabChanged;

  @override
  State<ContextReportScreen> createState() => _ContextReportScreenState();
}

class _ContextReportScreenState extends State<ContextReportScreen> {
  final TextEditingController _inputController = TextEditingController();
  late final PdokService _pdokService;
  bool _isComparisonMode = false;
  Widget? _lastFab;
  int _lastComparisonCount = -1;
  bool _lastComparisonMode = false;

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

  void _reportFab(Widget? fab) {
    if (widget.onFabChanged != null) {
      // Defer to avoid calling setState during build
      WidgetsBinding.instance.addPostFrameCallback((_) {
        widget.onFabChanged!(fab);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<ContextReportProvider>(
      create: (_) => ContextReportProvider(
        contextReportRepository: context.read<ContextReportRepository>(),
        aiRepository: context.read<AiRepository>(),
      ),
      child: Material(
        type: MaterialType.transparency,
        child: Consumer<ContextReportProvider>(
          builder: (context, provider, _) {
            final hasReport = provider.report != null;
            final isLoading = provider.isLoading;
            final comparisonCount = provider.comparisonIds.length;

            // Report the FAB to the parent HomeScreen
            final Widget? fab = comparisonCount > 0 && !_isComparisonMode
                ? FloatingActionButton.extended(
                    onPressed: () => setState(() => _isComparisonMode = true),
                    backgroundColor: ValoraColors.primary,
                    foregroundColor: Colors.white,
                    elevation: 6,
                    label: Text(
                      'Compare ($comparisonCount)',
                      style: const TextStyle(fontWeight: FontWeight.bold),
                    ),
                    icon: const Icon(Icons.compare_arrows_rounded),
                  )
                : null;

            // Only report if changed (avoid infinite loop)
            if (fab?.runtimeType != _lastFab?.runtimeType ||
                comparisonCount != _lastComparisonCount ||
                _isComparisonMode != _lastComparisonMode) {
              _lastFab = fab;
              _lastComparisonCount = comparisonCount;
              _lastComparisonMode = _isComparisonMode;
              _reportFab(fab);
            }

            if (_isComparisonMode) {
              return _ComparisonLayout(
                onBack: () => setState(() => _isComparisonMode = false),
                onClear: provider.clearComparison,
              );
            }

            return hasReport || isLoading
                ? _ReportLayout(
                    inputController: _inputController,
                    provider: provider,
                    pdokService: _pdokService,
                    isLoading: isLoading,
                  )
                : _SearchLayout(
                    inputController: _inputController,
                    provider: provider,
                    pdokService: _pdokService,
                  );
          },
        ),
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════════════
// SEARCH LAYOUT — The initial beautiful search screen
// ═══════════════════════════════════════════════════════════════════

class _SearchLayout extends StatelessWidget {
  const _SearchLayout({
    required this.inputController,
    required this.provider,
    required this.pdokService,
  });

  final TextEditingController inputController;
  final ContextReportProvider provider;
  final PdokService pdokService;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return CustomScrollView(
      slivers: [
        SliverToBoxAdapter(
          child: SizedBox(
            height: MediaQuery.of(context).padding.top + 16,
          ),
        ),

        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        gradient: const LinearGradient(
                          colors: [ValoraColors.primary, ValoraColors.primaryLight],
                          begin: Alignment.bottomLeft,
                          end: Alignment.topRight,
                        ),
                        borderRadius: BorderRadius.circular(14),
                      ),
                      child: const Icon(
                        Icons.analytics_rounded,
                        size: 24,
                        color: Colors.white,
                      ),
                    ),
                    const SizedBox(width: 14),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Property Analytics',
                          style: ValoraTypography.headlineSmall.copyWith(
                            fontWeight: FontWeight.w800,
                            letterSpacing: -0.3,
                          ),
                        ),
                        Text(
                          'Neighborhood insights for any Dutch address',
                          style: ValoraTypography.bodySmall.copyWith(
                            color: isDark
                                ? ValoraColors.neutral400
                                : ValoraColors.neutral500,
                          ),
                        ),
                      ],
                    ),
                  ],
                ).animate().fadeIn(duration: 400.ms).slideX(begin: -0.05),
                const SizedBox(height: 32),

                _SearchField(
                  controller: inputController,
                  provider: provider,
                  pdokService: pdokService,
                ).animate().fadeIn(duration: 400.ms, delay: 100.ms).slideY(begin: 0.1),

                const SizedBox(height: 16),

                _RadiusSelector(provider: provider)
                    .animate()
                    .fadeIn(duration: 400.ms, delay: 200.ms)
                    .slideY(begin: 0.1),

                const SizedBox(height: 20),

                _GenerateButton(
                  controller: inputController,
                  provider: provider,
                ).animate().fadeIn(duration: 400.ms, delay: 300.ms).slideY(begin: 0.1),

                if (provider.error != null) ...[
                  const SizedBox(height: 24),
                  ValoraEmptyState(
                    icon: Icons.error_outline_rounded,
                    title: 'Analysis Failed',
                    subtitle: provider.error,
                    actionLabel: 'Try Again',
                    onAction: () => provider.generate(inputController.text),
                  ),
                ],
              ],
            ),
          ),
        ),

        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(20, 32, 20, 0),
            child: _QuickActions(pdokService: pdokService, provider: provider, controller: inputController)
                .animate()
                .fadeIn(duration: 400.ms, delay: 350.ms),
          ),
        ),

        SliverToBoxAdapter(
          child: _HistorySection(
            controller: inputController,
            provider: provider,
          ),
        ),

        const SliverToBoxAdapter(
          child: SizedBox(height: 120),
        ),
      ],
    );
  }
}

// ═══════════════════════════════════════════════════════════════════
// REPORT LAYOUT — Report view with persistent search at top
// ═══════════════════════════════════════════════════════════════════

class _ReportLayout extends StatelessWidget {
  const _ReportLayout({
    required this.inputController,
    required this.provider,
    required this.pdokService,
    required this.isLoading,
  });

  final TextEditingController inputController;
  final ContextReportProvider provider;
  final PdokService pdokService;
  final bool isLoading;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return CustomScrollView(
      slivers: [
        SliverAppBar(
          pinned: true,
          floating: true,
          snap: true,
          automaticallyImplyLeading: false,
          backgroundColor: colorScheme.surface.withValues(alpha: 0.97),
          surfaceTintColor: Colors.transparent,
          toolbarHeight: 76,
          title: Row(
            children: [
              Expanded(
                child: _CompactSearchField(
                  controller: inputController,
                  provider: provider,
                  pdokService: pdokService,
                ),
              ),
              const SizedBox(width: 8),
              if (provider.report != null)
                _CompareButton(provider: provider),
            ],
          ),
        ),

        if (isLoading)
          const SliverFillRemaining(
            child: ContextReportSkeleton(),
          )
        else if (provider.report != null)
          SliverPadding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
            sliver: SliverList(
              delegate: SliverChildBuilderDelegate(
                (context, index) => Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 4),
                  child: ContextReportView.buildChild(
                    context,
                    index,
                    provider.report!,
                  ),
                ),
                childCount: ContextReportView.childCount(provider.report!),
              ),
            ),
          ),

        const SliverToBoxAdapter(
          child: SizedBox(height: 120),
        ),
      ],
    );
  }
}

// ═══════════════════════════════════════════════════════════════════
// COMPARISON LAYOUT
// ═══════════════════════════════════════════════════════════════════

class _ComparisonLayout extends StatelessWidget {
  const _ComparisonLayout({
    required this.onBack,
    required this.onClear,
  });

  final VoidCallback onBack;
  final VoidCallback onClear;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(
        backgroundColor: colorScheme.surface.withValues(alpha: 0.95),
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: onBack,
        ),
        title: Text(
          'Compare Properties',
          style: ValoraTypography.titleLarge.copyWith(
            fontWeight: FontWeight.bold,
          ),
        ),
        actions: [
          IconButton(
            tooltip: 'Clear all',
            icon: const Icon(Icons.delete_sweep_rounded),
            onPressed: onClear,
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: const ComparisonView(),
    );
  }
}

// ═══════════════════════════════════════════════════════════════════
// SHARED WIDGETS
// ═══════════════════════════════════════════════════════════════════

class _SearchField extends StatelessWidget {
  const _SearchField({
    required this.controller,
    required this.provider,
    required this.pdokService,
  });

  final TextEditingController controller;
  final ContextReportProvider provider;
  final PdokService pdokService;

  @override
  Widget build(BuildContext context) {
    return TypeAheadField<PdokSuggestion>(
      controller: controller,
      builder: (context, controller, focusNode) {
        return ValoraTextField(
          controller: controller,
          focusNode: focusNode,
          hint: 'Search city, zip, or address...',
          label: 'Address',
          prefixIcon: const Icon(Icons.search_rounded),
          suffixIcon: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (controller.text.isNotEmpty)
                IconButton(
                  icon: const Icon(Icons.clear_rounded, size: 20),
                  onPressed: () => controller.clear(),
                ),
            ],
          ),
          textInputAction: TextInputAction.search,
          onSubmitted: (val) => _handleSubmit(context, val),
        );
      },
      suggestionsCallback: (pattern) async {
        if (pattern.length < 3) return [];
        return await pdokService.search(pattern);
      },
      itemBuilder: (context, suggestion) {
        return ListTile(
          leading: const Icon(Icons.location_on_outlined, size: 20),
          title: Text(suggestion.displayName,
              style: ValoraTypography.bodyMedium),
          subtitle: Text(suggestion.type,
              style: ValoraTypography.labelSmall),
        );
      },
      onSelected: (suggestion) {
        controller.text = suggestion.displayName;
        provider.generate(suggestion.displayName);
      },
    );
  }

  void _handleSubmit(BuildContext context, String value) {
    FocusScope.of(context).unfocus();
    provider.generate(value);
  }
}

class _CompactSearchField extends StatelessWidget {
  const _CompactSearchField({
    required this.controller,
    required this.provider,
    required this.pdokService,
  });

  final TextEditingController controller;
  final ContextReportProvider provider;
  final PdokService pdokService;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return TypeAheadField<PdokSuggestion>(
      controller: controller,
      builder: (context, controller, focusNode) {
        return Container(
          height: 48,
          decoration: BoxDecoration(
            color: isDark
                ? ValoraColors.surfaceDark
                : ValoraColors.neutral50,
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
            border: Border.all(
              color: isDark
                  ? ValoraColors.neutral700.withValues(alpha: 0.4)
                  : ValoraColors.neutral200,
            ),
          ),
          child: TextField(
            controller: controller,
            focusNode: focusNode,
            textInputAction: TextInputAction.search,
            onSubmitted: (val) {
              FocusScope.of(context).unfocus();
              provider.generate(val);
            },
            style: ValoraTypography.bodyMedium.copyWith(
              color: isDark
                  ? ValoraColors.neutral50
                  : ValoraColors.neutral900,
            ),
            decoration: InputDecoration(
              hintText: 'Search another address...',
              hintStyle: ValoraTypography.bodyMedium.copyWith(
                color: isDark
                    ? ValoraColors.neutral500
                    : ValoraColors.neutral400,
              ),
              prefixIcon: Icon(
                Icons.search_rounded,
                size: 20,
                color: isDark
                    ? ValoraColors.neutral500
                    : ValoraColors.neutral400,
              ),
              suffixIcon: controller.text.isNotEmpty
                  ? IconButton(
                      icon: Icon(
                        Icons.close_rounded,
                        size: 18,
                        color: isDark
                            ? ValoraColors.neutral400
                            : ValoraColors.neutral50,
                      ),
                      onPressed: () {
                        controller.clear();
                        provider.clear();
                      },
                    )
                  : null,
              border: InputBorder.none,
              contentPadding:
                  const EdgeInsets.symmetric(horizontal: 0, vertical: 14),
            ),
          ),
        );
      },
      suggestionsCallback: (pattern) async {
        if (pattern.length < 3) return [];
        return await pdokService.search(pattern);
      },
      itemBuilder: (context, suggestion) {
        return ListTile(
          leading: const Icon(Icons.location_on_outlined, size: 20),
          title: Text(suggestion.displayName,
              style: ValoraTypography.bodyMedium),
          subtitle: Text(suggestion.type,
              style: ValoraTypography.labelSmall),
        );
      },
      onSelected: (suggestion) {
        controller.text = suggestion.displayName;
        provider.generate(suggestion.displayName);
      },
    );
  }
}

class _CompareButton extends StatelessWidget {
  const _CompareButton({required this.provider});
  final ContextReportProvider provider;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final isComparing = provider.isComparing(
        provider.report!.location.query, provider.radiusMeters);

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () {
          HapticFeedback.lightImpact();
          provider.toggleComparison(
              provider.report!.location.query, provider.radiusMeters);
        },
        borderRadius: BorderRadius.circular(12),
        child: AnimatedContainer(
          duration: ValoraAnimations.normal,
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          decoration: BoxDecoration(
            color: isComparing
                ? ValoraColors.primary.withValues(alpha: 0.15)
                : (isDark
                    ? ValoraColors.surfaceDark
                    : ValoraColors.neutral100),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: isComparing
                  ? ValoraColors.primary.withValues(alpha: 0.3)
                  : (isDark
                      ? ValoraColors.neutral700.withValues(alpha: 0.4)
                      : ValoraColors.neutral200),
            ),
          ),
          child: Icon(
            isComparing
                ? Icons.playlist_add_check_rounded
                : Icons.playlist_add_rounded,
            color:
                isComparing ? ValoraColors.primary : ValoraColors.neutral500,
            size: 22,
          ),
        ),
      ),
    );
  }
}

class _RadiusSelector extends StatelessWidget {
  const _RadiusSelector({required this.provider});
  final ContextReportProvider provider;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return ValoraCard(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Icon(
                    Icons.radar_rounded,
                    size: 18,
                    color: isDark
                        ? ValoraColors.neutral400
                        : ValoraColors.neutral500,
                  ),
                  const SizedBox(width: 8),
                  Text(
                    'Analysis Radius',
                    style: ValoraTypography.labelLarge
                        .copyWith(fontWeight: FontWeight.w600),
                  ),
                ],
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
          const SizedBox(height: 8),
          Selector<ContextReportProvider, int>(
            selector: (_, p) => p.radiusMeters,
            builder: (context, radiusMeters, _) {
              return SliderTheme(
                data: SliderTheme.of(context).copyWith(
                  trackHeight: 4,
                  thumbShape:
                      const RoundSliderThumbShape(enabledThumbRadius: 8),
                  overlayShape:
                      const RoundSliderOverlayShape(overlayRadius: 18),
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
    );
  }
}

class _GenerateButton extends StatelessWidget {
  const _GenerateButton({
    required this.controller,
    required this.provider,
  });

  final TextEditingController controller;
  final ContextReportProvider provider;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 56,
      child: ValoraButton(
        label: 'Generate Report',
        isLoading: provider.isLoading,
        onPressed: provider.isLoading || controller.text.isEmpty
            ? null
            : () {
                FocusScope.of(context).unfocus();
                provider.generate(controller.text);
              },
        variant: ValoraButtonVariant.primary,
        isFullWidth: true,
        size: ValoraButtonSize.large,
      ),
    );
  }
}

class _QuickActions extends StatelessWidget {
  const _QuickActions({
    required this.pdokService,
    required this.provider,
    required this.controller,
  });

  final PdokService pdokService;
  final ContextReportProvider provider;
  final TextEditingController controller;

  Future<void> _pickLocation(BuildContext context) async {
    final LatLng? result = await Navigator.push<LatLng>(
      context,
      MaterialPageRoute(builder: (context) => const LocationPicker()),
    );

    if (!context.mounted) return;
    if (result != null) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: const Text('Resolving address…'),
          duration: const Duration(seconds: 1),
          behavior: SnackBarBehavior.floating,
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      );

      final String? address =
          await pdokService.reverseLookup(result.latitude, result.longitude);

      if (!context.mounted) return;

      if (address != null) {
        controller.text = address;
        provider.generate(address);
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: const Text(
                'Could not resolve an address. Try searching by text.'),
            backgroundColor: ValoraColors.error,
            behavior: SnackBarBehavior.floating,
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12)),
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Row(
      children: [
        _QuickActionChip(
          icon: Icons.map_rounded,
          label: 'Pick on Map',
          onTap: () => _pickLocation(context),
          isDark: isDark,
        ),
        const SizedBox(width: 10),
        _QuickActionChip(
          icon: Icons.gps_fixed_rounded,
          label: 'My Location',
          onTap: () {
            // TODO: implement current location lookup
          },
          isDark: isDark,
        ),
      ],
    );
  }
}

class _QuickActionChip extends StatelessWidget {
  const _QuickActionChip({
    required this.icon,
    required this.label,
    required this.onTap,
    required this.isDark,
  });

  final IconData icon;
  final String label;
  final VoidCallback onTap;
  final bool isDark;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () {
          HapticFeedback.lightImpact();
          onTap();
        },
        borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          decoration: BoxDecoration(
            color: isDark
                ? ValoraColors.surfaceDark
                : ValoraColors.neutral50,
            borderRadius: BorderRadius.circular(ValoraSpacing.radiusFull),
            border: Border.all(
              color: isDark
                  ? ValoraColors.neutral700.withValues(alpha: 0.4)
                  : ValoraColors.neutral200,
            ),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 16, color: ValoraColors.primary),
              const SizedBox(width: 6),
              Text(
                label,
                style: ValoraTypography.labelMedium.copyWith(
                  color: isDark
                      ? ValoraColors.neutral200
                      : ValoraColors.neutral700,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _HistorySection extends StatelessWidget {
  const _HistorySection({
    required this.controller,
    required this.provider,
  });

  final TextEditingController controller;
  final ContextReportProvider provider;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return Selector<ContextReportProvider, List<dynamic>>(
      selector: (_, p) => p.history,
      builder: (context, history, _) {
        if (history.isEmpty) return const SizedBox.shrink();
        return Padding(
          padding: const EdgeInsets.fromLTRB(20, 32, 20, 0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'Recent',
                    style: ValoraTypography.titleMedium.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  TextButton(
                    onPressed: () =>
                        _confirmClearHistory(context, provider),
                    child: Text(
                      'Clear',
                      style: ValoraTypography.labelMedium
                          .copyWith(color: theme.colorScheme.primary),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              ...history.take(5).map((item) {
                return Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: ValoraCard(
                    onTap: provider.isLoading
                        ? null
                        : () {
                            controller.text = item.query;
                            provider.generate(item.query);
                          },
                    padding: const EdgeInsets.symmetric(
                        horizontal: 16, vertical: 14),
                    child: Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: ValoraColors.primary
                                .withValues(alpha: 0.08),
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(
                            Icons.history_rounded,
                            size: 16,
                            color: ValoraColors.primary,
                          ),
                        ),
                        const SizedBox(width: 14),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                item.query,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: ValoraTypography.bodyMedium.copyWith(
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                _formatDate(item.timestamp),
                                style: ValoraTypography.labelSmall.copyWith(
                                  color: isDark
                                      ? ValoraColors.neutral500
                                      : ValoraColors.neutral400,
                                ),
                              ),
                            ],
                          ),
                        ),
                        IconButton(
                          tooltip: provider.isComparing(
                                  item.query, provider.radiusMeters)
                              ? 'Remove from Compare'
                              : 'Add to Compare',
                          icon: Icon(
                            provider.isComparing(
                                    item.query, provider.radiusMeters)
                                ? Icons.playlist_add_check_rounded
                                : Icons.playlist_add_rounded,
                            size: 20,
                            color: provider.isComparing(
                                    item.query, provider.radiusMeters)
                                ? ValoraColors.primary
                                : ValoraColors.neutral400,
                          ),
                          onPressed: () => provider.toggleComparison(
                              item.query, provider.radiusMeters),
                        ),
                        Icon(
                          Icons.chevron_right_rounded,
                          size: 20,
                          color: isDark
                              ? ValoraColors.neutral500
                              : ValoraColors.neutral400,
                        ),
                      ],
                    ),
                  ),
                );
              }),
            ],
          ),
        );
      },
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