import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';
import '../../core/theme/valora_colors.dart';
import '../../providers/insights_provider.dart';
import '../../models/map_city_insight.dart';

class InsightsScreen extends StatefulWidget {
  const InsightsScreen({super.key});

  @override
  State<InsightsScreen> createState() => _InsightsScreenState();
}

class _InsightsScreenState extends State<InsightsScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<InsightsProvider>().loadInsights();
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
            return Center(child: Text('Error: ${provider.error}'));
          }

          return Stack(
            children: [
              FlutterMap(
                options: MapOptions(
                  initialCenter: const LatLng(52.1326, 5.2913),
                  initialZoom: 7.5,
                  minZoom: 6.0,
                  maxZoom: 18.0,
                  interactionOptions: const InteractionOptions(
                    flags: InteractiveFlag.all & ~InteractiveFlag.rotate,
                  ),
                ),
                children: [
                  TileLayer(
                    urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                    userAgentPackageName: 'com.valora.app',
                  ),
                  MarkerLayer(
                    markers: provider.cities.map((city) {
                      return _buildMarker(context, city, provider);
                    }).toList(),
                  ),
                ],
              ),
              _buildMetricSelector(context, provider),
            ],
          );
        },
      ),
    );
  }

  Marker _buildMarker(
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
            boxShadow: const [
              BoxShadow(
                color: Colors.black26,
                blurRadius: 4,
                offset: Offset(0, 2),
              ),
            ],
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
                labelStyle: TextStyle(
                  color: isSelected ? ValoraColors.primary : Colors.black87,
                  fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                ),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(20),
                  side: BorderSide(
                    color: isSelected ? ValoraColors.primary : Colors.grey.shade300,
                  ),
                ),
              ),
            );
          }).toList(),
        ),
      ),
    );
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
}
