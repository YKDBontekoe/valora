import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_shadows.dart';
import '../../providers/insights_provider.dart';
import '../../widgets/valora_widgets.dart';
import '../../widgets/insights/insights_header.dart';
import '../../widgets/insights/insights_legend.dart';
import '../../widgets/insights/insights_controls.dart';
import '../../widgets/insights/insights_metric_selector.dart';
import '../../widgets/insights/insights_map.dart';
import '../../widgets/insights/map_mode_selector.dart';
import '../../widgets/insights/persistent_details_panel.dart';

class InsightsScreen extends StatefulWidget {
  const InsightsScreen({super.key});

  @override
  State<InsightsScreen> createState() => _InsightsScreenState();
}

class _InsightsScreenState extends State<InsightsScreen> {
  final MapController _mapController = MapController();
  Timer? _debounceTimer;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<InsightsProvider>().loadInsights();
    });
  }

  @override
  void dispose() {
    _debounceTimer?.cancel();
    _mapController.dispose();
    super.dispose();
  }

  void _onMapChanged() {
    if (_debounceTimer?.isActive ?? false) _debounceTimer!.cancel();
    _debounceTimer = Timer(const Duration(milliseconds: 500), () {
      if (!mounted) return;
      final bounds = _mapController.camera.visibleBounds;
      context.read<InsightsProvider>().fetchMapData(
        minLat: bounds.south,
        minLon: bounds.west,
        maxLat: bounds.north,
        maxLon: bounds.east,
        zoom: _mapController.camera.zoom,
      );
    });
  }

  void _zoomMap(double delta) {
    final current = _mapController.camera;
    final targetZoom = (current.zoom + delta).clamp(6.0, 18.0);
    _mapController.move(current.center, targetZoom);
    _onMapChanged();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Theme.of(context).scaffoldBackgroundColor,
      body: Selector<InsightsProvider, (bool, bool, String?, MapMode)>(
        selector: (_, p) => (p.isLoading, p.cities.isEmpty, p.error, p.mapMode),
        shouldRebuild: (prev, next) {
          // If map mode changed, trigger data fetch for current viewport
          if (prev.$4 != next.$4) {
            // Schedule fetch after build to ensure provider state is settled
            WidgetsBinding.instance.addPostFrameCallback((_) => _onMapChanged());
          }
          return prev != next;
        },
        builder: (context, state, child) {
          final isLoading = state.$1;
          final isEmpty = state.$2;
          final error = state.$3;

          if (isLoading && isEmpty) {
            return const Center(child: CircularProgressIndicator());
          }
          if (error != null && isEmpty) {
            return Center(
              child: ValoraEmptyState(
                icon: Icons.error_outline_rounded,
                title: 'Failed to load insights',
                subtitle: 'An unexpected error occurred while loading data.',
                actionLabel: 'Retry',
                onAction: context.read<InsightsProvider>().loadInsights,
              ),
            );
          }

          return SafeArea(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(12, 12, 12, 6),
              child: DecoratedBox(
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(26),
                  boxShadow: ValoraShadows.lg,
                ),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(26),
                  child: Stack(
                    children: [
                      InsightsMap(
                        mapController: _mapController,
                        onMapChanged: _onMapChanged,
                      ),
                      IgnorePointer(
                        child: DecoratedBox(
                          decoration: BoxDecoration(
                            gradient: LinearGradient(
                              begin: Alignment.topCenter,
                              end: Alignment.bottomCenter,
                              colors: [
                                Theme.of(context).scaffoldBackgroundColor.withValues(alpha: 0.72),
                                Theme.of(context).scaffoldBackgroundColor.withValues(alpha: 0.02),
                                Theme.of(context).scaffoldBackgroundColor.withValues(alpha: 0.14),
                              ],
                              stops: const [0, 0.42, 1],
                            ),
                          ),
                          child: const SizedBox.expand(),
                        ),
                      ),
                      const InsightsHeader(),
                      const InsightsMetricSelector(),
                      const Positioned(
                        top: 140,
                        left: 0,
                        right: 0,
                        child: MapModeSelector(),
                      ),
                      const InsightsLegend(),
                      InsightsControls(
                        onZoomIn: () => _zoomMap(0.7),
                        onZoomOut: () => _zoomMap(-0.7),
                        onMapChanged: _onMapChanged,
                      ),
                      Selector<InsightsProvider, String?>(
                        selector: (_, p) => p.mapError,
                        builder: (context, mapError, _) {
                          if (mapError == null) return const SizedBox.shrink();
                          return Positioned(
                            bottom: 152,
                            left: 16,
                            right: 16,
                            child: _buildMapError(mapError),
                          );
                        },
                      ),
                      const PersistentDetailsPanel(),
                    ],
                  ),
                ),
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildMapError(String error) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: ValoraColors.neutral800.withValues(alpha: 0.82),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        child: Text(
          error,
          style: const TextStyle(color: Colors.white, fontSize: 12.5),
          textAlign: TextAlign.center,
        ),
      ),
    );
  }
}
