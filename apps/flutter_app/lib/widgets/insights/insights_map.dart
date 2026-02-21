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
import '../../models/map_property.dart';
import '../../screens/property_detail_screen.dart';
import '../../core/formatters/currency_formatter.dart';

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
    return Consumer<InsightsProvider>(
      builder: (context, provider, _) {
        final isDark = Theme.of(context).brightness == Brightness.dark;
        final showOverlays = provider.showOverlays;
        final showAmenities = provider.showAmenities;
        final showProperties = provider.showProperties;
        final overlays = provider.overlays;
        final overlayTiles = provider.overlayTiles;
        final amenities = provider.amenities;
        final amenityClusters = provider.amenityClusters;
        final cities = provider.cities;
        final properties = provider.properties;

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
                  isDark ? "https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png" : "https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png",
              userAgentPackageName: "com.valora.app",
              subdomains: const ["a", "b", "c", "d"],
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
                        return buildAmenityMarker(context, amenity);
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
            if (showProperties && properties.isNotEmpty)
              MarkerLayer(
                markers: properties.map((property) {
                  return buildPropertyMarker(context, property);
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

  static Marker buildPropertyMarker(BuildContext context, MapProperty property) {
    return Marker(
      point: property.location,
      width: 50,
      height: 30,
      child: GestureDetector(
        onTap: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => PropertyDetailScreen(propertyId: property.id),
            ),
          );
        },
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: ValoraColors.priceTag,
            borderRadius: BorderRadius.circular(8),
             boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.2),
                blurRadius: 4,
                offset: Offset(0, 2),
              ),
            ],
          ),
          child: Center(
            child: Text(
               property.price != null ?
                 CurrencyFormatter.formatCompact(property.price!) : '?',
              style: const TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.bold,
                fontSize: 11,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
