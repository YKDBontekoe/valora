import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../providers/insights_provider.dart';
import '../../models/map_city_insight.dart';
import '../../models/map_amenity.dart';
import '../../models/map_overlay.dart';
import '../../widgets/insights/map_legend.dart';
import '../../widgets/valora_widgets.dart';

class InsightsScreen extends StatefulWidget {
  const InsightsScreen({super.key});

  @override
  State<InsightsScreen> createState() => _InsightsScreenState();
}

class _InsightsScreenState extends State<InsightsScreen> {
  final MapController _mapController = MapController();
  Timer? _debounceTimer;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<InsightsProvider>().loadInsights();
    });
  }

  @override
  void dispose() {
    _debounceTimer?.cancel();
    _mapController.dispose();
    super.dispose();
  }

  void _onMapChanged() {
    if (_debounceTimer?.isActive ?? false) _debounceTimer!.cancel();
    _debounceTimer = Timer(const Duration(milliseconds: 500), () {
      if (!mounted) return;
      final bounds = _mapController.camera.visibleBounds;
      context.read<InsightsProvider>().fetchMapData(
            minLat: bounds.south,
            minLon: bounds.west,
            maxLat: bounds.north,
            maxLon: bounds.east,
            zoom: _mapController.camera.zoom,
          );
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Consumer<InsightsProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading && provider.cities.isEmpty) {
            return const Center(child: CircularProgressIndicator());
          }
          if (provider.error != null && provider.cities.isEmpty) {
            return Center(
              child: ValoraEmptyState(
                icon: Icons.error_outline_rounded,
                title: 'Failed to load insights',
                subtitle: provider.error,
                action: ValoraButton(
                  label: 'Retry',
                  onPressed: provider.loadInsights,
                ),
              ),
            );
          }

          return Stack(
            children: [
              FlutterMap(
                mapController: _mapController,
                options: MapOptions(
                  initialCenter: const LatLng(52.1326, 5.2913),
                  initialZoom: 7.5,
                  minZoom: 6.0,
                  maxZoom: 18.0,
                  onPositionChanged: (position, hasGesture) {
                    if (hasGesture) { _onMapChanged(); setState(() {}); }
                  },
                  interactionOptions: const InteractionOptions(
                    flags: InteractiveFlag.all & ~InteractiveFlag.rotate,
                  ),
                ),
                children: [
                  TileLayer(
                    urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                    userAgentPackageName: 'com.valora.app',
                  ),
                  if (provider.showOverlays)
                    PolygonLayer(
                      polygons: provider.overlays.map((overlay) {
                        return _buildPolygon(context, overlay);
                      }).toList(),
                    ),
                  if (provider.showAmenities)
                    MarkerLayer(
                      markers: provider.amenities.map((amenity) {
                        return _buildAmenityMarker(context, amenity);
                      }).toList(),
                    ),
                  MarkerLayer(
                    markers: provider.cities.map((city) {
                      return _buildCityMarker(context, city, provider);
                    }).toList(),
                  ),
                ],
              ),
              _buildMetricSelector(context, provider),
              _buildLayerToggle(context, provider),
              if (provider.showOverlays)
                Positioned(
                  left: 16,
                  bottom: 24,
                  child: MapLegend(metric: provider.selectedOverlayMetric),
                ),
              _buildZoomWarning(provider),
              if (provider.mapError != null)
                Positioned(
                  bottom: 120,
                  left: 16,
                  right: 16,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                    decoration: BoxDecoration(
                      color: Colors.black87,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      provider.mapError!,
                      style: const TextStyle(color: Colors.white, fontSize: 12),
                      textAlign: TextAlign.center,
                    ),
                  ),
                )
            ],
          );
        },
      ),
    );
  }

  Polygon _buildPolygon(BuildContext context, MapOverlay overlay) {
    List<LatLng> points = [];
    try {
      final geometry = overlay.geoJson['geometry'];
      if (geometry != null && geometry['type'] == 'Polygon') {
        final List<dynamic> ring = geometry['coordinates'][0];
        points = ring.map((coord) => LatLng(coord[1].toDouble(), coord[0].toDouble())).toList();
      } else if (geometry != null && geometry['type'] == 'MultiPolygon') {
        final List<dynamic> poly = geometry['coordinates'][0];
        final List<dynamic> ring = poly[0];
        points = ring.map((coord) => LatLng(coord[1].toDouble(), coord[0].toDouble())).toList();
      }
    } catch (e) {
      debugPrint('Failed to parse polygon: $e');
    }

    final color = _getOverlayColor(overlay.metricValue, context.read<InsightsProvider>().selectedOverlayMetric);

    return Polygon(
      points: points,
      color: color.withValues(alpha: 0.4),
      borderColor: color,
      borderStrokeWidth: 1,
      label: overlay.displayValue,
    );
  }

  Marker _buildAmenityMarker(BuildContext context, MapAmenity amenity) {
    return Marker(
      point: amenity.location,
      width: 40,
      height: 40,
      child: GestureDetector(
        onTap: () => _showAmenityDetails(context, amenity),
        child: Container(
          decoration: const BoxDecoration(
            color: Colors.white,
            shape: BoxShape.circle,
            boxShadow: [BoxShadow(color: Colors.black26, blurRadius: 4)],
          ),
          child: Icon(
            _getAmenityIcon(amenity.type),
            size: 20,
            color: ValoraColors.primary,
          ),
        ),
      ),
    );
  }

  Marker _buildCityMarker(
      BuildContext context, MapCityInsight city, InsightsProvider provider) {
    final score = provider.getScore(city);
    final color = _getColorForScore(score);
    final size = 30.0 + (city.count > 100 ? 10 : 0);

    return Marker(
      point: city.location,
      width: size,
      height: size,
      child: GestureDetector(
        onTap: () => _showCityDetails(context, city),
        child: Container(
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.8),
            shape: BoxShape.circle,
            border: Border.all(color: Colors.white, width: 2),
          ),
          child: Center(
            child: Text(
              score != null ? score.toStringAsFixed(1) : '-',
              style: const TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.bold,
                fontSize: 10,
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
                Icon(_getAmenityIcon(amenity.type), color: ValoraColors.primary),
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
              style: const TextStyle(color: Colors.grey, letterSpacing: 1.2, fontSize: 12),
            ),
            const SizedBox(height: 16),
            if (amenity.metadata != null)
              ...amenity.metadata!.entries.take(5).map((e) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 2.0),
                child: Text('${e.key}: ${e.value}'),
              )),
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
            Text(
              city.city,
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            const SizedBox(height: 16),
            _buildDetailRow('Listings', city.count.toString()),
            _buildDetailRow('Composite Score', city.compositeScore?.toStringAsFixed(1)),
            _buildDetailRow('Safety Score', city.safetyScore?.toStringAsFixed(1)),
            _buildDetailRow('Social Score', city.socialScore?.toStringAsFixed(1)),
            _buildDetailRow('Amenities Score', city.amenitiesScore?.toStringAsFixed(1)),
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
          Text(value ?? '-', style: const TextStyle(fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }

  IconData _getAmenityIcon(String type) {
    switch (type) {
      case 'school': return Icons.school_rounded;
      case 'supermarket': return Icons.shopping_basket_rounded;
      case 'park': return Icons.park_rounded;
      case 'healthcare': return Icons.medical_services_rounded;
      case 'transit': return Icons.directions_bus_rounded;
      case 'charging_station': return Icons.ev_station_rounded;
      default: return Icons.place_rounded;
    }
  }

  Color _getOverlayColor(double value, MapOverlayMetric metric) {
    if (metric == MapOverlayMetric.pricePerSquareMeter) {
      if (value > 6000) return Colors.red;
      if (value > 4500) return Colors.orange;
      if (value > 3000) return Colors.yellow;
      return Colors.green;
    }

    if (metric == MapOverlayMetric.crimeRate) {
      // For crime rate, higher is WORSE (invert scale)
      if (value > 100) return Colors.red;
      if (value > 50) return Colors.orange;
      if (value > 20) return Colors.yellow;
      return Colors.green;
    }

    // Default gradient (higher is better)
    if (value > 80) return Colors.green;
    if (value > 50) return Colors.orange;
    return Colors.red;
  }

  Color _getColorForScore(double? score) {
    if (score == null) return Colors.grey;
    if (score >= 80) return ValoraColors.success;
    if (score >= 60) return ValoraColors.warning;
    if (score >= 40) return Colors.orange;
    return ValoraColors.error;
  }

  Widget _buildMetricSelector(BuildContext context, InsightsProvider provider) {
    return Positioned(
      top: 50,
      left: 16,
      right: 16,
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: InsightMetric.values.map((metric) {
            final isSelected = provider.selectedMetric == metric;
            return Padding(
              padding: const EdgeInsets.only(right: 8),
              child: FilterChip(
                label: Text(_getMetricLabel(metric)),
                selected: isSelected,
                onSelected: (_) => provider.setMetric(metric),
                backgroundColor: Colors.white.withValues(alpha: 0.9),
                selectedColor: ValoraColors.primary.withValues(alpha: 0.2),
              ),
            );
          }).toList(),
        ),
      ),
    );
  }

  Widget _buildLayerToggle(BuildContext context, InsightsProvider provider) {
    return Positioned(
      bottom: 24,
      right: 16,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          if (provider.showOverlays)
            Container(
              margin: const EdgeInsets.only(bottom: 8),
              padding: const EdgeInsets.symmetric(horizontal: 12),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(24),
                boxShadow: const [BoxShadow(color: Colors.black12, blurRadius: 4)],
              ),
              child: DropdownButton<MapOverlayMetric>(
                value: provider.selectedOverlayMetric,
                underline: const SizedBox(),
                items: MapOverlayMetric.values.map((m) {
                  return DropdownMenuItem(
                    value: m,
                    child: Text(_getOverlayLabel(m), style: const TextStyle(fontSize: 12)),
                  );
                }).toList(),
                onChanged: (m) {
                  if (m != null) {
                    provider.setOverlayMetric(m);
                    _onMapChanged();
                  }
                },
              ),
            ),
          FloatingActionButton.small(
            heroTag: 'toggle_amenities',
            onPressed: () {
              provider.toggleAmenities();
              _onMapChanged();
            },
            backgroundColor: provider.showAmenities ? ValoraColors.primary : Colors.white,
            child: Icon(Icons.place, color: provider.showAmenities ? Colors.white : Colors.black),
          ),
          const SizedBox(height: 8),
          FloatingActionButton.small(
            heroTag: 'toggle_overlays',
            onPressed: () {
              provider.toggleOverlays();
              _onMapChanged();
            },
            backgroundColor: provider.showOverlays ? ValoraColors.primary : Colors.white,
            child: Icon(Icons.layers, color: provider.showOverlays ? Colors.white : Colors.black),
          ),
        ],
      ),
    );
  }

  String _getMetricLabel(InsightMetric metric) {
    switch (metric) {
      case InsightMetric.composite: return 'Overall';
      case InsightMetric.safety: return 'Safety';
      case InsightMetric.social: return 'Social';
      case InsightMetric.amenities: return 'Amenities';
    }
  }


  Widget _buildZoomWarning(InsightsProvider provider) {
    if (!mounted) return const SizedBox.shrink();
    double zoom;
    try {
      zoom = _mapController.camera.zoom;
    } catch (_) {
      return const SizedBox.shrink();
    }
    final needsZoomForAmenities = provider.showAmenities && zoom < 13;
    final needsZoomForOverlays = provider.showOverlays && zoom < 11;

    if (!needsZoomForAmenities && !needsZoomForOverlays) return const SizedBox.shrink();

    return Positioned(
      top: 110,
      left: 16,
      right: 16,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        decoration: BoxDecoration(
          color: Colors.amber.withValues(alpha: 0.9),
          borderRadius: BorderRadius.circular(12),
          boxShadow: const [BoxShadow(color: Colors.black12, blurRadius: 4)],
        ),
        child: Row(
          children: [
            const Icon(Icons.zoom_in_rounded, size: 20, color: Colors.black87),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                needsZoomForAmenities
                  ? 'Zoom in further to see amenities'
                  : 'Zoom in further to see overlays',
                style: const TextStyle(
                  color: Colors.black87,
                  fontSize: 12,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  String _getOverlayLabel(MapOverlayMetric metric) {
    switch (metric) {
      case MapOverlayMetric.pricePerSquareMeter: return 'Price/m²';
      case MapOverlayMetric.crimeRate: return 'Crime Rate';
      case MapOverlayMetric.populationDensity: return 'Pop. Density';
      case MapOverlayMetric.averageWoz: return 'Avg WOZ';
    }
  }
}
