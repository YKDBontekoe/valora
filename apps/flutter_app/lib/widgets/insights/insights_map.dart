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
        final showOverlays = data.$1;
        final showAmenities = data.$2;
        final overlays = data.$3;
        final amenities = data.$4;
        final cities = data.$5;

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
                  'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png',
              userAgentPackageName: 'com.valora.app',
              subdomains: const ['a', 'b', 'c', 'd'],
              retinaMode: RetinaMode.isHighDensity(context),
            ),
            if (showOverlays)
              PolygonLayer(
                polygons:
                    overlays.map((overlay) {
                      return _buildPolygon(context, overlay);
                    }).toList(),
              ),
            if (showAmenities)
              MarkerLayer(
                markers:
                    amenities.map((amenity) {
                      return _buildAmenityMarker(context, amenity);
                    }).toList(),
              ),
            MarkerLayer(
              markers:
                  cities.map((city) {
                    return _buildCityMarker(
                      context,
                      city,
                      context.read<InsightsProvider>(),
                    );
                  }).toList(),
            ),
          ],
        );
      },
    );
  }

  Polygon _buildPolygon(BuildContext context, MapOverlay overlay) {
    final points = MapUtils.parsePolygonGeometry(overlay.geoJson['geometry']);

    final color = MapUtils.getOverlayColor(
      overlay.metricValue,
      context.read<InsightsProvider>().selectedOverlayMetric,
    );

    return Polygon(
      points: points,
      color: color.withValues(alpha: 0.26),
      borderColor: color,
      borderStrokeWidth: 1.5,
      label: overlay.displayValue,
    );
  }

  Marker _buildAmenityMarker(BuildContext context, MapAmenity amenity) {
    return Marker(
      point: amenity.location,
      width: 36,
      height: 36,
      child: GestureDetector(
        onTap: () => _showAmenityDetails(context, amenity),
        child: DecoratedBox(
          decoration: const BoxDecoration(
            color: Colors.white,
            shape: BoxShape.circle,
            boxShadow: [
              BoxShadow(
                color: Colors.black26,
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

  Marker _buildCityMarker(
    BuildContext context,
    MapCityInsight city,
    InsightsProvider provider,
  ) {
    final score = provider.getScore(city);
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
        onTap: () => _showCityDetails(context, city),
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
            border: Border.all(color: Colors.white, width: 2.2),
            boxShadow: const [
              BoxShadow(
                color: Colors.black26,
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

  void _showAmenityDetails(BuildContext context, MapAmenity amenity) {
    showModalBottomSheet(
      context: context,
      builder: (context) => Padding(
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
              style: const TextStyle(
                color: Colors.grey,
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
      ),
    );
  }

  void _showCityDetails(BuildContext context, MapCityInsight city) {
    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) => Container(
        padding: const EdgeInsets.all(24),
        decoration: const BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(city.city, style: Theme.of(context).textTheme.headlineSmall),
            const SizedBox(height: 16),
            _buildDetailRow('Listings', city.count.toString()),
            _buildDetailRow(
              'Composite Score',
              city.compositeScore?.toStringAsFixed(1),
            ),
            _buildDetailRow(
              'Safety Score',
              city.safetyScore?.toStringAsFixed(1),
            ),
            _buildDetailRow(
              'Social Score',
              city.socialScore?.toStringAsFixed(1),
            ),
            _buildDetailRow(
              'Amenities Score',
              city.amenitiesScore?.toStringAsFixed(1),
            ),
            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }

  Widget _buildDetailRow(String label, String? value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(color: Colors.grey)),
          Text(
            value ?? '-',
            style: const TextStyle(fontWeight: FontWeight.bold),
          ),
        ],
      ),
    );
  }
}
