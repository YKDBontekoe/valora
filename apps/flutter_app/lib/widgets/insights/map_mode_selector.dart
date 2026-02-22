import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_shadows.dart';
import '../../providers/insights_provider.dart';

class MapModeSelector extends StatelessWidget {
  const MapModeSelector({super.key});

  @override
  Widget build(BuildContext context) {
    final mode = context.select<InsightsProvider, MapMode>((p) => p.mapMode);

    return Container(
      height: 44,
      margin: const EdgeInsets.symmetric(horizontal: 16),
      decoration: BoxDecoration(
        color: Theme.of(context).cardColor,
        borderRadius: BorderRadius.circular(12),
        boxShadow: ValoraShadows.sm,
      ),
      child: Row(
        children: [
          _buildSegment(context, 'Cities', MapMode.cities, mode),
          _buildSegment(context, 'Overlays', MapMode.overlays, mode),
          _buildSegment(context, 'Amenities', MapMode.amenities, mode),
        ],
      ),
    );
  }

  Widget _buildSegment(BuildContext context, String label, MapMode value, MapMode current) {
    final isSelected = value == current;
    return Expanded(
      child: GestureDetector(
        onTap: () => context.read<InsightsProvider>().setMapMode(value),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          margin: const EdgeInsets.all(4),
          decoration: BoxDecoration(
            color: isSelected ? ValoraColors.primary : Colors.transparent,
            borderRadius: BorderRadius.circular(8),
          ),
          alignment: Alignment.center,
          child: Text(
            label,
            style: TextStyle(
              color: isSelected ? Colors.white : Theme.of(context).textTheme.bodyMedium?.color,
              fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
              fontSize: 13,
            ),
          ),
        ),
      ),
    );
  }
}
