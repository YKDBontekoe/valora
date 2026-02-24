import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../repositories/context_report_repository.dart';
import '../../services/pdok_service.dart';
import '../../providers/context_report_provider.dart';
import '../../core/theme/valora_colors.dart';

import 'layouts/search_layout.dart';
import 'layouts/report_layout.dart';
import 'layouts/comparison_layout.dart';

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
      create: (_) =>
          ContextReportProvider(repository: context.read<ContextReportRepository>()),
      child: Material(
        type: MaterialType.transparency,
        child: Selector<ContextReportProvider, ({bool hasReport, bool isLoading, int comparisonCount, bool isComparisonMode})>(
          selector: (_, p) => (
            hasReport: p.report != null,
            isLoading: p.isLoading,
            comparisonCount: p.comparisonIds.length,
            isComparisonMode: _isComparisonMode,
          ),
          builder: (context, data, _) {
            final provider = context.read<ContextReportProvider>();
            final hasReport = data.hasReport;
            final isLoading = data.isLoading;
            final comparisonCount = data.comparisonCount;

            // Report the FAB to the parent HomeScreen
            final Widget? fab = comparisonCount > 0 && !_isComparisonMode
                ? FloatingActionButton.extended(
                    onPressed: () =>
                        setState(() => _isComparisonMode = true),
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
              return ComparisonLayout(
                onBack: () => setState(() => _isComparisonMode = false),
                onClear: provider.clearComparison,
              );
            }

            return hasReport || isLoading
                ? ReportLayout(
                    inputController: _inputController,
                    provider: provider,
                    pdokService: _pdokService,
                    isLoading: isLoading,
                  )
                : SearchLayout(
                    inputController: _inputController,
                    provider: provider,
                    pdokService: _pdokService,
                  );
          },
        ),
      ),
    );
  }

  int _lastComparisonCount = -1;
  bool _lastComparisonMode = false;
}
