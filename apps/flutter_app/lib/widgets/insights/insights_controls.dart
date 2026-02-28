import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_shadows.dart';
import '../../providers/insights_provider.dart';
import '../../models/map_overlay.dart';

class InsightsControls extends StatelessWidget {
  final VoidCallback onZoomIn;
  final VoidCallback onZoomOut;
  final VoidCallback onMapChanged;

  const InsightsControls({
    super.key,
    required this.onZoomIn,
    required this.onZoomOut,
    required this.onMapChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Positioned(
      bottom: 24,
      right: 12,
      child: Selector<InsightsProvider, bool>(
        selector: (_, p) => p.selectedFeature != null,
        builder: (context, hasSelection, _) {
          return AnimatedContainer(
            duration: const Duration(milliseconds: 300),
            curve: Curves.easeOutQuint,
            transform: Matrix4.translationValues(0, hasSelection ? -220 : 0, 0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Consumer<InsightsProvider>(
                  builder: (context, provider, _) {
                    if (!provider.showOverlays) return const SizedBox.shrink();
                    return _OverlayMetricPicker(
                      selected: provider.selectedOverlayMetric,
                      onChanged: (m) {
                        provider.setOverlayMetric(m);
                        onMapChanged();
                      },
                    );
                  },
                ),
                _buildZoomButton(
                  context,
                  key: const Key('insights_zoom_in_button'),
                  icon: Icons.add_rounded,
                  onPressed: onZoomIn,
                ),
                const SizedBox(height: 6),
                _buildZoomButton(
                  context,
                  key: const Key('insights_zoom_out_button'),
                  icon: Icons.remove_rounded,
                  onPressed: onZoomOut,
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildZoomButton(BuildContext context, {
    Key? key,
    required IconData icon,
    required VoidCallback onPressed,
  }) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return DecoratedBox(
      decoration: BoxDecoration(
        color: isDark ? ValoraColors.glassBlackStrong : ValoraColors.glassWhiteStrong,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200,
        ),
        boxShadow: isDark ? ValoraShadows.smDark : ValoraShadows.sm,
      ),
      child: SizedBox(
        width: 42,
        height: 42,
        child: IconButton(
          key: key,
          onPressed: onPressed,
          icon: Icon(icon, color: Theme.of(context).iconTheme.color, size: 20),
        ),
      ),
    );
  }
}

class _OverlayMetricPicker extends StatelessWidget {
  final MapOverlayMetric selected;
  final ValueChanged<MapOverlayMetric> onChanged;

  const _OverlayMetricPicker({required this.selected, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      constraints: const BoxConstraints(maxWidth: 148),
      decoration: BoxDecoration(
        color: isDark ? ValoraColors.glassBlackStrong : ValoraColors.glassWhiteStrong,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200),
        boxShadow: isDark ? ValoraShadows.smDark : ValoraShadows.sm,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(10, 8, 10, 4),
            child: Text(
              'Overlay',
              style: TextStyle(
                fontSize: 10,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.6,
                color: isDark ? ValoraColors.neutral500 : ValoraColors.neutral400,
              ),
            ),
          ),
          ...MapOverlayMetric.values.map((m) {
            final isSelected = m == selected;
            return GestureDetector(
              onTap: () => onChanged(m),
              child: Container(
                margin: const EdgeInsets.fromLTRB(4, 0, 4, 4),
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                decoration: BoxDecoration(
                  color: isSelected
                      ? ValoraColors.primary.withValues(alpha: 0.15)
                      : Colors.transparent,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Row(
                  children: [
                    Icon(
                      _getOverlayIcon(m),
                      size: 12,
                      color: isSelected
                          ? ValoraColors.primary
                          : (isDark ? ValoraColors.neutral400 : ValoraColors.neutral500),
                    ),
                    const SizedBox(width: 6),
                    Expanded(
                      child: Text(
                        _getOverlayLabel(m),
                        style: TextStyle(
                          fontSize: 11.5,
                          fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
                          color: isSelected
                              ? ValoraColors.primary
                              : (isDark ? ValoraColors.neutral300 : ValoraColors.neutral700),
                        ),
                      ),
                    ),
                    if (isSelected)
                      const Icon(Icons.check_rounded, size: 12, color: ValoraColors.primary),
                  ],
                ),
              ),
            );
          }),
        ],
      ),
    );
  }

  IconData _getOverlayIcon(MapOverlayMetric metric) {
    switch (metric) {
      case MapOverlayMetric.pricePerSquareMeter:
        return Icons.euro_rounded;
      case MapOverlayMetric.crimeRate:
        return Icons.gpp_bad_rounded;
      case MapOverlayMetric.populationDensity:
        return Icons.groups_rounded;
      case MapOverlayMetric.averageWoz:
        return Icons.home_work_rounded;
    }
  }

  String _getOverlayLabel(MapOverlayMetric metric) {
    switch (metric) {
      case MapOverlayMetric.pricePerSquareMeter:
        return 'Price/m²';
      case MapOverlayMetric.crimeRate:
        return 'Crime Rate';
      case MapOverlayMetric.populationDensity:
        return 'Pop. Density';
      case MapOverlayMetric.averageWoz:
        return 'Avg WOZ';
    }
  }
}
