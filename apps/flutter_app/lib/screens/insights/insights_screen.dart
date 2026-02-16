import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../core/theme/valora_shadows.dart';
import '../../providers/insights_provider.dart';
import '../../models/map_city_insight.dart';
import '../../models/map_amenity.dart';
import '../../models/map_overlay.dart';
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
      backgroundColor: ValoraColors.backgroundLight,
      body: Selector<InsightsProvider, (bool, bool, String?)>(
        selector: (_, p) => (p.isLoading, p.cities.isEmpty, p.error),
        builder: (context, state, child) {
          final isLoading = state.$1;
          final isEmpty = state.$2;
          final error = state.$3;

          if (isLoading && isEmpty) {
            return const Center(child: CircularProgressIndicator());
          }
          if (error != null && isEmpty) {
            return Center(
              child: ValoraEmptyState(
                icon: Icons.error_outline_rounded,
                title: 'Failed to load insights',
                subtitle: 'An unexpected error occurred while loading data.',
                actionLabel: 'Retry',
                onAction: context.read<InsightsProvider>().loadInsights,
              ),
            );
          }

          return SafeArea(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(12, 12, 12, 6),
              child: DecoratedBox(
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(26),
                  boxShadow: ValoraShadows.lg,
                ),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(26),
                  child: Stack(
                    children: [
                      _buildMap(context),
                      IgnorePointer(
                        child: DecoratedBox(
                          decoration: BoxDecoration(
                            gradient: LinearGradient(
                              begin: Alignment.topCenter,
                              end: Alignment.bottomCenter,
                              colors: [
                                Colors.white.withValues(alpha: 0.72),
                                Colors.white.withValues(alpha: 0.02),
                                Colors.white.withValues(alpha: 0.14),
                              ],
                              stops: const [0, 0.42, 1],
                            ),
                          ),
                          child: const SizedBox.expand(),
                        ),
                      ),
                      _buildMapHeader(context),
                      _buildMetricSelector(context),
                      _buildMapLegend(context),
                      _buildLayerToggle(context),
                      Selector<InsightsProvider, String?>(
                        selector: (_, p) => p.mapError,
                        builder: (context, mapError, _) {
                          if (mapError == null) return const SizedBox.shrink();
                          return Positioned(
                            bottom: 152,
                            left: 16,
                            right: 16,
                            child: _buildMapError(mapError),
                          );
                        },
                      ),
                    ],
                  ),
                ),
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildMap(BuildContext context) {
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
        // metric needed for polygon color, assumed accessed via provider in _buildPolygon or passed here
        // _buildPolygon uses context.read(), which is fine for data that changes less often or is acceptable
        // However, selectedOverlayMetric IS in the selector to trigger rebuild.

        return FlutterMap(
          mapController: _mapController,
          options: MapOptions(
            initialCenter: const LatLng(52.1326, 5.2913),
            initialZoom: 7.5,
            minZoom: 6.0,
            maxZoom: 18.0,
            onPositionChanged: (position, hasGesture) {
              if (hasGesture) _onMapChanged();
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

  Widget _buildMapHeader(BuildContext context) {
    return Positioned(
      top: 12,
      left: 12,
      right: 12,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: Colors.white.withValues(alpha: 0.93),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: ValoraColors.neutral200),
          boxShadow: ValoraShadows.md,
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          child: Row(
            children: [
              Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  gradient: ValoraColors.primaryGradient,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Icon(
                  Icons.insights_rounded,
                  color: Colors.white,
                  size: 20,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Area Insights',
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                        color: ValoraColors.neutral900,
                      ),
                    ),
                    Text(
                      '${provider.cities.length} cities',
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: ValoraColors.neutral600,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Polygon _buildPolygon(BuildContext context, MapOverlay overlay) {
    List<LatLng> points = [];
    try {
      final geometry = overlay.geoJson['geometry'];
      if (geometry != null && geometry['type'] == 'Polygon') {
        final List<dynamic> ring = geometry['coordinates'][0];
        points = ring
            .map((coord) => LatLng(coord[1].toDouble(), coord[0].toDouble()))
            .toList();
      } else if (geometry != null && geometry['type'] == 'MultiPolygon') {
        final List<dynamic> poly = geometry['coordinates'][0];
        final List<dynamic> ring = poly[0];
        points = ring
            .map((coord) => LatLng(coord[1].toDouble(), coord[0].toDouble()))
            .toList();
      }
    } catch (e) {
      debugPrint('Failed to parse polygon: $e');
    }

    final color = _getOverlayColor(
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
            _getAmenityIcon(amenity.type),
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
    final color = _getColorForScore(score);
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
                  _getAmenityIcon(amenity.type),
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

  IconData _getAmenityIcon(String type) {
    switch (type) {
      case 'school':
        return Icons.school_rounded;
      case 'supermarket':
        return Icons.shopping_basket_rounded;
      case 'park':
        return Icons.park_rounded;
      case 'healthcare':
        return Icons.medical_services_rounded;
      case 'transit':
        return Icons.directions_bus_rounded;
      case 'charging_station':
        return Icons.ev_station_rounded;
      default:
        return Icons.place_rounded;
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

  Widget _buildMetricSelector(BuildContext context) {
    return Positioned(
      top: 92,
      left: 16,
      right: 16,
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Consumer<InsightsProvider>(
          builder: (context, provider, _) {
            return Row(
              children: InsightMetric.values.map((metric) {
                final isSelected = provider.selectedMetric == metric;
                return Padding(
                  padding: const EdgeInsets.only(right: 8),
                  child: FilterChip(
                    label: Text(_getMetricLabel(metric)),
                    selected: isSelected,
                    onSelected: (_) => provider.setMetric(metric),
                    checkmarkColor: ValoraColors.primaryDark,
                    side: BorderSide(
                      color: isSelected
                          ? ValoraColors.primary
                          : ValoraColors.neutral300,
                    ),
                    backgroundColor: Colors.white.withValues(alpha: 0.88),
                    selectedColor: ValoraColors.primaryLight.withValues(
                      alpha: 0.25,
                    ),
                    shadowColor: Colors.black.withValues(alpha: 0.08),
                    elevation: 2,
                  ),
                );
              }).toList(),
            );
          },
        ),
      ),
    );
  }

  Widget _buildMapLegend(BuildContext context) {
    return Selector<InsightsProvider, InsightMetric>(
      selector: (_, p) => p.selectedMetric,
      builder: (context, metric, _) {
        return Positioned(
          left: 16,
          bottom: 24,
          child: Container(
            key: const Key('insights_map_legend'),
            width: 168,
            padding: const EdgeInsets.fromLTRB(12, 10, 12, 10),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.94),
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: ValoraColors.neutral200),
              boxShadow: ValoraShadows.md,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${_getMetricLabel(metric)} score',
                  style: Theme.of(context).textTheme.labelLarge?.copyWith(
                    color: ValoraColors.neutral900,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 8),
                _buildLegendRow('80+', ValoraColors.success),
                _buildLegendRow('60-79', ValoraColors.warning),
                _buildLegendRow('40-59', Colors.orange),
                _buildLegendRow('<40', ValoraColors.error),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildLegendRow(String label, Color color) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 5),
      child: Row(
        children: [
          Container(
            width: 9,
            height: 9,
            decoration: BoxDecoration(color: color, shape: BoxShape.circle),
          ),
          const SizedBox(width: 8),
          Text(
            label,
            style: const TextStyle(
              fontSize: 12,
              color: ValoraColors.neutral700,
              fontWeight: FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildLayerToggle(BuildContext context) {
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
                          _onMapChanged();
                        }
                      },
                    ),
                  ),
                ),
              _buildActionButton(
                key: const Key('insights_zoom_in_button'),
                icon: Icons.add_rounded,
                onPressed: () => _zoomMap(0.7),
              ),
              const SizedBox(height: 8),
              _buildActionButton(
                key: const Key('insights_zoom_out_button'),
                icon: Icons.remove_rounded,
                onPressed: () => _zoomMap(-0.7),
              ),
              const SizedBox(height: 8),
              _buildActionButton(
                icon: Icons.place_rounded,
                isActive: provider.showAmenities,
                onPressed: () {
                  provider.toggleAmenities();
                  _onMapChanged();
                },
              ),
              const SizedBox(height: 8),
              _buildActionButton(
                icon: Icons.layers_rounded,
                isActive: provider.showOverlays,
                onPressed: () {
                  provider.toggleOverlays();
                  _onMapChanged();
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

  Widget _buildMapError(String error) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: Colors.black.withValues(alpha: 0.82),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        child: Text(
          error,
          style: const TextStyle(color: Colors.white, fontSize: 12.5),
          textAlign: TextAlign.center,
        ),
      ),
    );
  }

  void _zoomMap(double delta) {
    final current = _mapController.camera;
    final targetZoom = (current.zoom + delta).clamp(6.0, 18.0);
    _mapController.move(current.center, targetZoom);
    _onMapChanged();
  }

  String _getMetricLabel(InsightMetric metric) {
    switch (metric) {
      case InsightMetric.composite:
        return 'Overall';
      case InsightMetric.safety:
        return 'Safety';
      case InsightMetric.social:
        return 'Social';
      case InsightMetric.amenities:
        return 'Amenities';
    }
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
