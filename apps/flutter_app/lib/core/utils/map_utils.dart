import 'package:flutter/material.dart';
import '../theme/valora_colors.dart';
import 'package:latlong2/latlong.dart';
import 'package:logging/logging.dart';
import '../../models/map_overlay.dart';

class MapUtils {
  static final _log = Logger('MapUtils');
  static List<LatLng> parsePolygonGeometry(Map<String, dynamic>? geometry) {
    if (geometry == null) return [];

    List<LatLng> points = [];
    try {
      final type = geometry['type'];
      final coordinates = geometry['coordinates'];

      if (coordinates is! List || coordinates.isEmpty) return [];

      if (type == 'Polygon') {
        final List<dynamic> ring = coordinates[0];
        points = ring
            .map((coord) => LatLng(coord[1].toDouble(), coord[0].toDouble()))
            .toList();
      } else if (type == 'MultiPolygon') {
        final List<dynamic> poly = coordinates[0];
        if (poly.isNotEmpty && poly[0] is List) {
          final List<dynamic> ring = poly[0];
          points = ring
              .map((coord) => LatLng(coord[1].toDouble(), coord[0].toDouble()))
              .toList();
        }
      }
    } catch (e) {
      _log.warning('Failed to parse polygon', e);
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
      if (value > 6000) return ValoraColors.error;
      if (value > 4500) return ValoraColors.warning;
      if (value > 3000) return ValoraColors.info;
      return ValoraColors.success;
    }

    if (metric == MapOverlayMetric.crimeRate) {
      // For crime rate, higher is WORSE (invert scale)
      if (value > 100) return ValoraColors.error;
      if (value > 50) return ValoraColors.warning;
      if (value > 20) return ValoraColors.info;
      return ValoraColors.success;
    }

    // Default gradient (higher is better)
    if (value > 80) return ValoraColors.success;
    if (value > 50) return ValoraColors.warning;
    return ValoraColors.error;
  }

  static Color getColorForScore(double? score) {
    if (score == null) return ValoraColors.neutral400;
    if (score >= 80) return ValoraColors.success;
    if (score >= 60) return ValoraColors.warning;
    if (score >= 40) return ValoraColors.warning;
    return ValoraColors.error;
  }
}
