import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/utils/map_utils.dart';
import '../../providers/insights_provider.dart';
import '../../models/map_overlay.dart';
import '../../models/map_amenity.dart';
import '../../models/map_city_insight.dart';

class InsightsMap extends StatelessWidget {
  final MapController mapController;
  final VoidCallback onMapChanged;

  const InsightsMap({
    super.key,
    required this.mapController,
    required this.onMapChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Selector<
      InsightsProvider,
      (
        bool,
        bool,
        List<MapOverlay>,
        List<MapAmenity>,
        List<MapCityInsight>,
        MapOverlayMetric
      )
    >(
      selector:
          (_, p) => (
            p.showOverlays,
            p.showAmenities,
            p.overlays,
            p.amenities,
            p.cities,
            p.selectedOverlayMetric,
          ),
      builder: (context, data, _) {
        final isDark = Theme.of(context).brightness == Brightness.dark;
        final showOverlays = data.$1;
        final showAmenities = data.$2;
        final overlays = data.$3;
        final amenities = data.$4;
        final cities = data.$5;
        // Access provider for methods used in builder
        final provider = context.read<InsightsProvider>();

        return FlutterMap(
          mapController: mapController,
          options: MapOptions(
            initialCenter: const LatLng(52.1326, 5.2913),
            initialZoom: 7.5,
            minZoom: 6.0,
            maxZoom: 18.0,
            onPositionChanged: (position, hasGesture) {
              if (hasGesture) onMapChanged();
            },
            interactionOptions: const InteractionOptions(
              flags: InteractiveFlag.all & ~InteractiveFlag.rotate,
            ),
          ),
          children: [
            TileLayer(
              urlTemplate:
                  isDark ? 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png' : 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png',
              userAgentPackageName: 'com.valora.app',
              subdomains: const ['a', 'b', 'c', 'd'],
              retinaMode: RetinaMode.isHighDensity(context),
            ),
            if (showOverlays)
              PolygonLayer(
                polygons:
                    overlays.map((overlay) {
                      return buildPolygon(overlay, provider.selectedOverlayMetric);
                    }).toList(),
              ),
            if (showAmenities)
              MarkerLayer(
                markers:
                    amenities.map((amenity) {
                      return buildAmenityMarker(context, amenity);
                    }).toList(),
              ),
            MarkerLayer(
              markers:
                  cities.map((city) {
                    return buildCityMarker(context, city, provider.getScore(city));
                  }).toList(),
            ),
          ],
        );
      },
    );
  }

  static Polygon buildPolygon(MapOverlay overlay, MapOverlayMetric metric) {
    final points = MapUtils.parsePolygonGeometry(overlay.geoJson['geometry']);

    final color = MapUtils.getOverlayColor(
      overlay.metricValue,
      metric,
    );

    return Polygon(
      points: points,
      color: color.withValues(alpha: 0.26),
      borderColor: color,
      borderStrokeWidth: 1.5,
      label: overlay.displayValue,
    );
  }

  static Marker buildAmenityMarker(BuildContext context, MapAmenity amenity) {
    return Marker(
      point: amenity.location,
      width: 36,
      height: 36,
      child: GestureDetector(
        onTap: () => showModalBottomSheet(
          context: context,
          builder: (context) => buildAmenityDetailsSheet(context, amenity),
        ),
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            shape: BoxShape.circle,
            boxShadow: [
              BoxShadow(
                color: Theme.of(context).shadowColor.withValues(alpha: 0.2),
                blurRadius: 8,
                offset: Offset(0, 2),
              ),
            ],
          ),
          child: Icon(
            MapUtils.getAmenityIcon(amenity.type),
            size: 18,
            color: ValoraColors.primary,
          ),
        ),
      ),
    );
  }

  static Marker buildCityMarker(
    BuildContext context,
    MapCityInsight city,
    double? score,
  ) {
    final color = MapUtils.getColorForScore(score);
    final size = city.count >= 120
        ? 58.0
        : city.count >= 60
        ? 52.0
        : 46.0;

    return Marker(
      point: city.location,
      width: size,
      height: size,
      child: GestureDetector(
        onTap: () => showModalBottomSheet(
          context: context,
          backgroundColor: Colors.transparent,
          builder: (context) => buildCityDetailsSheet(context, city),
        ),
        child: DecoratedBox(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                color.withValues(alpha: 0.95),
                color.withValues(alpha: 0.72),
              ],
            ),
            shape: BoxShape.circle,
            border: Border.all(color: Theme.of(context).colorScheme.surface, width: 2.2),
            boxShadow: [
              BoxShadow(
                color: Theme.of(context).shadowColor.withValues(alpha: 0.2),
                blurRadius: 12,
                offset: Offset(0, 4),
              ),
            ],
          ),
          child: Center(
            child: Text(
              score != null ? score.round().toString() : '-',
              style: const TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.bold,
                fontSize: 14,
              ),
            ),
          ),
        ),
      ),
    );
  }

  static Widget buildAmenityDetailsSheet(BuildContext context, MapAmenity amenity) {
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
          const SizedBox(height: 24),
        ],
      ),
    );
  }

  static Widget buildCityDetailsSheet(BuildContext context, MapCityInsight city) {
    return Container(
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(city.city, style: Theme.of(context).textTheme.headlineSmall),
          const SizedBox(height: 16),
          buildDetailRow(context, 'Data points', city.count.toString()),
          buildDetailRow(context,
            'Composite Score',
            city.compositeScore?.toStringAsFixed(1),
          ),
          buildDetailRow(context,
            'Safety Score',
            city.safetyScore?.toStringAsFixed(1),
          ),
          buildDetailRow(context,
            'Social Score',
            city.socialScore?.toStringAsFixed(1),
          ),
          buildDetailRow(context,
            'Amenities Score',
            city.amenitiesScore?.toStringAsFixed(1),
          ),
          const SizedBox(height: 24),
        ],
      ),
    );
  }

  static Widget buildDetailRow(BuildContext context, String label, String? value) {
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
