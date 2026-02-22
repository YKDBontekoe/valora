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
      right: 16,
      child: Consumer<InsightsProvider>(
        builder: (context, provider, _) {
          final isDark = Theme.of(context).brightness == Brightness.dark;
          return Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              if (provider.showOverlays)
                Container(
                  margin: const EdgeInsets.only(bottom: 8),
                  decoration: BoxDecoration(
                    color: isDark ? ValoraColors.glassBlackStrong : ValoraColors.glassWhiteStrong,
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200),
                    boxShadow: isDark ? ValoraShadows.smDark : ValoraShadows.sm,
                  ),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 12),
                    child: DropdownButton<MapOverlayMetric>(
                      value: provider.selectedOverlayMetric,
                      underline: const SizedBox(),
                      icon: const Icon(Icons.keyboard_arrow_down_rounded),
                      items: MapOverlayMetric.values.map((m) {
                        return DropdownMenuItem(
                          value: m,
                          child: Text(
                            _getOverlayLabel(m),
                            style: const TextStyle(fontSize: 12),
                          ),
                        );
                      }).toList(),
                      onChanged: (m) {
                        if (m != null) {
                          provider.setOverlayMetric(m);
                          onMapChanged();
                        }
                      },
                    ),
                  ),
                ),
              _buildActionButton(context,
                key: const Key('insights_zoom_in_button'),
                icon: Icons.add_rounded,
                onPressed: onZoomIn,
              ),
              const SizedBox(height: 8),
              _buildActionButton(context,
                key: const Key('insights_zoom_out_button'),
                icon: Icons.remove_rounded,
                onPressed: onZoomOut,
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildActionButton(BuildContext context, {
    Key? key,
    required IconData icon,
    required VoidCallback onPressed,
    bool isActive = false,
  }) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: isActive
            ? ValoraColors.primary
            : (Theme.of(context).brightness == Brightness.dark ? ValoraColors.glassBlackStrong : ValoraColors.glassWhiteStrong),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isActive ? ValoraColors.primaryDark : (Theme.of(context).brightness == Brightness.dark ? ValoraColors.neutral700 : ValoraColors.neutral200),
        ),
        boxShadow: Theme.of(context).brightness == Brightness.dark ? ValoraShadows.smDark : ValoraShadows.sm,
      ),
      child: SizedBox(
        width: 44,
        height: 44,
        child: IconButton(
          key: key,
          onPressed: onPressed,
          icon: Icon(
            icon,
            color: isActive ? Colors.white : Theme.of(context).iconTheme.color,
            size: 21,
          ),
        ),
      ),
    );
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
