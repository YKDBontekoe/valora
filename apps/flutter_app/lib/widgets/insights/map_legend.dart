import 'package:flutter/material.dart';
import '../../models/map_overlay.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_spacing.dart';
import '../valora_glass_container.dart';

class MapLegend extends StatelessWidget {
  final MapOverlayMetric metric;

  const MapLegend({super.key, required this.metric});

  @override
  Widget build(BuildContext context) {
    return ValoraGlassContainer(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      borderRadius: BorderRadius.circular(16),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            _getLabel(),
            style: const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 8),
          _buildScale(),
        ],
      ),
    );
  }

  String _getLabel() {
    switch (metric) {
      case MapOverlayMetric.pricePerSquareMeter:
        return 'Price / m²';
      case MapOverlayMetric.crimeRate:
        return 'Crime Rate';
      case MapOverlayMetric.populationDensity:
        return 'Pop. Density';
      case MapOverlayMetric.averageWoz:
        return 'Avg WOZ';
    }
  }

  Widget _buildScale() {
    final List<Color> colors;
    final List<String> labels;

    if (metric == MapOverlayMetric.pricePerSquareMeter) {
      colors = [Colors.green, Colors.yellow, Colors.orange, Colors.red];
      labels = ['<3k', '3k-4.5k', '4.5k-6k', '>6k'];
    } else if (metric == MapOverlayMetric.crimeRate) {
      colors = [Colors.green, Colors.yellow, Colors.orange, Colors.red];
      labels = ['Low', 'Med', 'High', 'Very High'];
    } else {
      colors = [Colors.red, Colors.orange, Colors.green];
      labels = ['Low', 'Med', 'High'];
    }

    return Column(
      children: List.generate(colors.length, (index) {
        return Padding(
          padding: const EdgeInsets.symmetric(vertical: 2),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 12,
                height: 12,
                decoration: BoxDecoration(
                  color: colors[index].withValues(alpha: 0.8),
                  shape: BoxShape.circle,
                ),
              ),
              const SizedBox(width: 8),
              Text(
                labels[index],
                style: const TextStyle(fontSize: 10),
              ),
            ],
          ),
        );
      }),
    );
  }
}
