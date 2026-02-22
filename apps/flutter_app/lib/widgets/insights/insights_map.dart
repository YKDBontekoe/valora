import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/utils/map_utils.dart';
import '../../providers/insights_provider.dart';
import '../../models/map_overlay.dart';
import '../../models/map_overlay_tile.dart';
import '../../models/map_amenity.dart';
import '../../models/map_amenity_cluster.dart';
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
        List<MapOverlayTile>,
        List<MapAmenity>,
        List<MapAmenityCluster>,
        List<MapCityInsight>,
        MapOverlayMetric,
        Object?
      )
    >(
      selector:
          (_, p) => (
            p.showOverlays,
            p.showAmenities,
            p.overlays,
            p.overlayTiles,
            p.amenities,
            p.amenityClusters,
            p.cities,
            p.selectedOverlayMetric,
            p.selectedFeature,
          ),
      builder: (context, data, _) {
        final isDark = Theme.of(context).brightness == Brightness.dark;
        final showOverlays = data.$1;
        final showAmenities = data.$2;
        final overlays = data.$3;
        final overlayTiles = data.$4;
        final amenities = data.$5;
        final amenityClusters = data.$6;
        final cities = data.$7;
        final selectedFeature = data.$9;

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
            onTap: (_, __) => provider.clearSelection(),
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
            if (showOverlays) ...[
              if (overlays.isNotEmpty)
                PolygonLayer(
                  polygons:
                      overlays.map((overlay) {
                        return buildPolygon(overlay, provider.selectedOverlayMetric);
                      }).toList(),
                ),
              if (overlayTiles.isNotEmpty)
                PolygonLayer(
                  polygons:
                      overlayTiles.map((tile) {
                        return buildTilePolygon(tile, provider.selectedOverlayMetric);
                      }).toList(),
                ),
            ],
            if (showAmenities) ...[
              if (amenities.isNotEmpty)
                MarkerLayer(
                  markers:
                      amenities.map((amenity) {
                        return buildAmenityMarker(context, amenity, amenity == selectedFeature, provider);
                      }).toList(),
                ),
              if (amenityClusters.isNotEmpty)
                MarkerLayer(
                  markers:
                      amenityClusters.map((cluster) {
                        return buildClusterMarker(context, cluster);
                      }).toList(),
                ),
            ],
            MarkerLayer(
              markers:
                  cities.map((city) {
                    return buildCityMarker(context, city, provider.getScore(city), city == selectedFeature, provider);
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

  static Polygon buildTilePolygon(MapOverlayTile tile, MapOverlayMetric metric) {
    // Create a square polygon around the center
    final halfSize = tile.size / 2;
    final points = [
      LatLng(tile.latitude - halfSize, tile.longitude - halfSize),
      LatLng(tile.latitude - halfSize, tile.longitude + halfSize),
      LatLng(tile.latitude + halfSize, tile.longitude + halfSize),
      LatLng(tile.latitude + halfSize, tile.longitude - halfSize),
    ];

    final color = MapUtils.getOverlayColor(
      tile.value,
      metric,
    );

    return Polygon(
      points: points,
      color: color.withValues(alpha: 0.26),
      borderColor: color.withValues(alpha: 0.1), // Faint border for tiles
      borderStrokeWidth: 0.5,
      label: tile.displayValue,
    );
  }

  static Marker buildAmenityMarker(BuildContext context, MapAmenity amenity, bool isSelected, InsightsProvider provider) {
    return Marker(
      point: amenity.location,
      width: isSelected ? 48 : 36,
      height: isSelected ? 48 : 36,
      child: GestureDetector(
        onTap: () => provider.selectFeature(amenity),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            shape: BoxShape.circle,
            border: isSelected ? Border.all(color: ValoraColors.primary, width: 3) : null,
            boxShadow: [
              BoxShadow(
                color: isSelected
                  ? ValoraColors.primary.withValues(alpha: 0.4)
                  : Theme.of(context).shadowColor.withValues(alpha: 0.2),
                blurRadius: isSelected ? 12 : 8,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          child: Icon(
            MapUtils.getAmenityIcon(amenity.type),
            size: isSelected ? 24 : 18,
            color: ValoraColors.primary,
          ),
        ),
      ),
    );
  }

  static Marker buildClusterMarker(BuildContext context, MapAmenityCluster cluster) {
    return Marker(
      point: LatLng(cluster.latitude, cluster.longitude),
      width: 40,
      height: 40,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: ValoraColors.primary,
          shape: BoxShape.circle,
          boxShadow: [
            BoxShadow(
              color: ValoraColors.primary.withValues(alpha: 0.4),
              blurRadius: 8,
              offset: Offset(0, 2),
            ),
          ],
        ),
        child: Center(
          child: Text(
            cluster.count.toString(),
            style: const TextStyle(
              color: Colors.white,
              fontWeight: FontWeight.bold,
              fontSize: 14,
            ),
          ),
        ),
      ),
    );
  }

  static Marker buildCityMarker(
    BuildContext context,
    MapCityInsight city,
    double? score,
    bool isSelected,
    InsightsProvider provider,
  ) {
    final color = MapUtils.getColorForScore(score);
    final size = (city.count >= 120 ? 58.0 : city.count >= 60 ? 52.0 : 46.0) * (isSelected ? 1.2 : 1.0);

    return Marker(
      point: city.location,
      width: size,
      height: size,
      child: GestureDetector(
        onTap: () => provider.selectFeature(city),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
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
            border: Border.all(
              color: isSelected ? Colors.white : Theme.of(context).colorScheme.surface,
              width: isSelected ? 4.0 : 2.2,
            ),
            boxShadow: [
              BoxShadow(
                color: isSelected
                  ? color.withValues(alpha: 0.5)
                  : Theme.of(context).shadowColor.withValues(alpha: 0.2),
                blurRadius: isSelected ? 16 : 12,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          child: Center(
            child: Text(
              score != null ? score.round().toString() : '-',
              style: TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.bold,
                fontSize: isSelected ? 16 : 14,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
