import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_shadows.dart';
import '../../core/utils/map_utils.dart';
import '../../models/map_amenity.dart';
import '../../models/map_city_insight.dart';
import '../../providers/insights_provider.dart';

class PersistentDetailsPanel extends StatelessWidget {
  const PersistentDetailsPanel({super.key});

  @override
  Widget build(BuildContext context) {
    return Selector<InsightsProvider, Object?>(
      selector: (_, p) => p.selectedFeature,
      builder: (context, feature, _) {
        if (feature == null) {
          return const SizedBox.shrink();
        }

        return Positioned(
          left: 12,
          right: 12,
          bottom: 24,
          child: Container(
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.surface,
              borderRadius: BorderRadius.circular(24),
              boxShadow: ValoraShadows.lg,
              border: Border.all(
                color: Theme.of(context).dividerColor.withValues(alpha: 0.1),
              ),
            ),
            child: Stack(
              children: [
                _buildContent(context, feature),
                Positioned(
                  top: 12,
                  right: 12,
                  child: IconButton(
                    key: const Key('panel_close_button'),
                    icon: const Icon(Icons.close_rounded, size: 20),
                    onPressed: () => context.read<InsightsProvider>().clearSelection(),
                    style: IconButton.styleFrom(
                      backgroundColor: Theme.of(context).scaffoldBackgroundColor.withValues(alpha: 0.5),
                      padding: const EdgeInsets.all(8),
                      minimumSize: const Size(32, 32),
                    ),
                  ),
                ),
              ],
            ),
          ).animate().slideY(begin: 1.0, end: 0.0, duration: 300.ms, curve: Curves.easeOutQuint).fadeIn(),
        );
      },
    );
  }

  Widget _buildContent(BuildContext context, Object feature) {
    if (feature is MapCityInsight) {
      return _buildCityDetails(context, feature);
    } else if (feature is MapAmenity) {
      return _buildAmenityDetails(context, feature);
    }
    return const SizedBox.shrink();
  }

  Widget _buildCityDetails(BuildContext context, MapCityInsight city) {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(city.city, style: Theme.of(context).textTheme.headlineSmall),
          const SizedBox(height: 16),
          _buildDetailRow(context, 'Data points', city.count.toString()),
          _buildDetailRow(context,
            'Composite Score',
            city.compositeScore?.toStringAsFixed(1),
          ),
          _buildDetailRow(context,
            'Safety Score',
            city.safetyScore?.toStringAsFixed(1),
          ),
          _buildDetailRow(context,
            'Social Score',
            city.socialScore?.toStringAsFixed(1),
          ),
          _buildDetailRow(context,
            'Amenities Score',
            city.amenitiesScore?.toStringAsFixed(1),
          ),
        ],
      ),
    );
  }

  Widget _buildAmenityDetails(BuildContext context, MapAmenity amenity) {
    return Padding(
      padding: const EdgeInsets.all(24.0),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(
                MapUtils.getAmenityIcon(amenity.type),
                color: ValoraColors.primary,
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  amenity.name,
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            amenity.type.toUpperCase(),
            style: TextStyle(
              color: Theme.of(context).textTheme.bodySmall?.color,
              letterSpacing: 1.2,
              fontSize: 12,
            ),
          ),
          const SizedBox(height: 16),
          if (amenity.metadata != null)
            ...amenity.metadata!.entries
                .take(5)
                .map(
                  (e) => Padding(
                    padding: const EdgeInsets.symmetric(vertical: 2.0),
                    child: Text('${e.key}: ${e.value}'),
                  ),
                ),
        ],
      ),
    );
  }

  Widget _buildDetailRow(BuildContext context, String label, String? value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(color: Theme.of(context).textTheme.bodySmall?.color)),
          Text(
            value ?? '-',
            style: const TextStyle(fontWeight: FontWeight.bold),
          ),
        ],
      ),
    );
  }
}
