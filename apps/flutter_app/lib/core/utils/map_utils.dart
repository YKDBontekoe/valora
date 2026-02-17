import 'package:flutter/material.dart';
import 'package:latlong2/latlong.dart';
import '../../core/theme/valora_colors.dart';
import '../../models/map_overlay.dart';

class MapUtils {
  static List<LatLng> parsePolygonGeometry(Map<String, dynamic> geometry) {
    List<LatLng> points = [];
    try {
      if (geometry['type'] == 'Polygon') {
        final List<dynamic> ring = geometry['coordinates'][0];
        points = ring
            .map((coord) => LatLng(coord[1].toDouble(), coord[0].toDouble()))
            .toList();
      } else if (geometry['type'] == 'MultiPolygon') {
        final List<dynamic> poly = geometry['coordinates'][0];
        final List<dynamic> ring = poly[0];
        points = ring
            .map((coord) => LatLng(coord[1].toDouble(), coord[0].toDouble()))
            .toList();
      }
    } catch (e) {
      debugPrint('Failed to parse polygon: $e');
    }
    return points;
  }

  static IconData getAmenityIcon(String type) {
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

  static Color getOverlayColor(double value, MapOverlayMetric metric) {
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

  static Color getColorForScore(double? score) {
    if (score == null) return Colors.grey;
    if (score >= 80) return ValoraColors.success;
    if (score >= 60) return ValoraColors.warning;
    if (score >= 40) return Colors.orange;
    return ValoraColors.error;
  }
}
