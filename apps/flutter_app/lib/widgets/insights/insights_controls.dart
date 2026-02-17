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
          return Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              if (provider.showOverlays)
                Container(
                  margin: const EdgeInsets.only(bottom: 8),
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.94),
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(color: ValoraColors.neutral200),
                    boxShadow: ValoraShadows.sm,
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
              _buildActionButton(
                key: const Key('insights_zoom_in_button'),
                icon: Icons.add_rounded,
                onPressed: onZoomIn,
              ),
              const SizedBox(height: 8),
              _buildActionButton(
                key: const Key('insights_zoom_out_button'),
                icon: Icons.remove_rounded,
                onPressed: onZoomOut,
              ),
              const SizedBox(height: 8),
              _buildActionButton(
                icon: Icons.place_rounded,
                isActive: provider.showAmenities,
                onPressed: () {
                  provider.toggleAmenities();
                  onMapChanged();
                },
              ),
              const SizedBox(height: 8),
              _buildActionButton(
                icon: Icons.layers_rounded,
                isActive: provider.showOverlays,
                onPressed: () {
                  provider.toggleOverlays();
                  onMapChanged();
                },
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildActionButton({
    Key? key,
    required IconData icon,
    required VoidCallback onPressed,
    bool isActive = false,
  }) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: isActive
            ? ValoraColors.primary
            : Colors.white.withValues(alpha: 0.94),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isActive ? ValoraColors.primaryDark : ValoraColors.neutral200,
        ),
        boxShadow: ValoraShadows.sm,
      ),
      child: SizedBox(
        width: 44,
        height: 44,
        child: IconButton(
          key: key,
          onPressed: onPressed,
          icon: Icon(
            icon,
            color: isActive ? Colors.white : ValoraColors.neutral800,
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
