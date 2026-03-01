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
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      height: 48,
      decoration: BoxDecoration(
        color: isDark ? ValoraColors.glassBlackStrong : ValoraColors.glassWhiteStrong,
        borderRadius: BorderRadius.circular(13),
        border: Border.all(color: isDark ? ValoraColors.neutral700 : ValoraColors.neutral200),
        boxShadow: isDark ? ValoraShadows.smDark : ValoraShadows.sm,
      ),
      child: Padding(
        padding: const EdgeInsets.all(4),
        child: Row(
          children: MapMode.values
              .map((m) => _buildSegment(context, m, mode, isDark))
              .toList(),
        ),
      ),
    );
  }

  Widget _buildSegment(BuildContext context, MapMode value, MapMode current, bool isDark) {
    final isSelected = value == current;
    final (label, icon) = _getModeInfo(value);

    return Expanded(
      child: Semantics(
        button: true,
        enabled: true,
        selected: isSelected,
        label: label,
        child: GestureDetector(
          onTap: () => context.read<InsightsProvider>().setMapMode(value),
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 220),
            curve: Curves.easeInOut,
            decoration: BoxDecoration(
              color: isSelected ? ValoraColors.primary : Colors.transparent,
              borderRadius: BorderRadius.circular(9),
              boxShadow: isSelected
                  ? [
                      BoxShadow(
                        color: ValoraColors.primary.withValues(alpha: 0.30),
                        blurRadius: 8,
                        offset: const Offset(0, 2),
                      ),
                    ]
                  : null,
            ),
            alignment: Alignment.center,
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(
                  icon,
                  size: 13,
                  color: isSelected
                      ? Colors.white
                      : (isDark ? ValoraColors.neutral400 : ValoraColors.neutral500),
                ),
                const SizedBox(width: 5),
                AnimatedDefaultTextStyle(
                  duration: const Duration(milliseconds: 220),
                  style: TextStyle(
                    fontSize: 12.5,
                    fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
                    color: isSelected
                        ? Colors.white
                        : (isDark ? ValoraColors.neutral400 : ValoraColors.neutral500),
                  ),
                  child: Text(label),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  (String, IconData) _getModeInfo(MapMode mode) {
    return switch (mode) {
      MapMode.cities    => ('Cities',    Icons.location_city_rounded),
      MapMode.overlays  => ('Overlays',  Icons.layers_rounded),
      MapMode.amenities => ('Amenities', Icons.storefront_rounded),
    };
  }
}
