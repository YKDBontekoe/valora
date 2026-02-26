import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/core/utils/map_utils.dart';
import 'package:valora_app/core/theme/valora_colors.dart';
import 'package:valora_app/models/map_overlay.dart';

void main() {
  group('MapUtils.parsePolygonGeometry', () {
    test('returns empty list for null input', () {
      final points = MapUtils.parsePolygonGeometry(null);
      expect(points, isEmpty);
    });

    test('returns empty list for empty map', () {
      final points = MapUtils.parsePolygonGeometry({});
      expect(points, isEmpty);
    });

    test('returns empty list for missing coordinates', () {
      final points = MapUtils.parsePolygonGeometry({'type': 'Polygon'});
      expect(points, isEmpty);
    });

    test('returns empty list for non-list coordinates', () {
      final points = MapUtils.parsePolygonGeometry({
        'type': 'Polygon',
        'coordinates': 'invalid',
      });
      expect(points, isEmpty);
    });

    test('parses simple Polygon correctly', () {
      final points = MapUtils.parsePolygonGeometry({
        'type': 'Polygon',
        'coordinates': [
          [
            [1.0, 2.0],
            [3.0, 4.0],
            [5.0, 6.0],
          ]
        ],
      });

      expect(points.length, 3);
      // LatLng constructor is (latitude, longitude). GeoJSON is [lon, lat].
      expect(points[0].latitude, 2.0);
      expect(points[0].longitude, 1.0);
      expect(points[1].latitude, 4.0);
      expect(points[1].longitude, 3.0);
      expect(points[2].latitude, 6.0);
      expect(points[2].longitude, 5.0);
    });

    test('parses simple MultiPolygon correctly', () {
      final points = MapUtils.parsePolygonGeometry({
        'type': 'MultiPolygon',
        'coordinates': [
          [
            [
              [10.0, 20.0],
              [30.0, 40.0],
            ]
          ]
        ],
      });

      expect(points.length, 2);
      expect(points[0].latitude, 20.0);
      expect(points[0].longitude, 10.0);
      expect(points[1].latitude, 40.0);
      expect(points[1].longitude, 30.0);
    });

    test('handles malformed coordinates gracefully', () {
      final points = MapUtils.parsePolygonGeometry({
        'type': 'Polygon',
        'coordinates': [
          ['invalid', 'data']
        ],
      });
      expect(points, isEmpty);
    });
  });

  group('MapUtils.getAmenityIcon', () {
    test('returns school icon for school', () {
      expect(MapUtils.getAmenityIcon('school'), Icons.school_rounded);
    });
    test('returns supermarket icon for supermarket', () {
      expect(MapUtils.getAmenityIcon('supermarket'), Icons.shopping_basket_rounded);
    });
    test('returns park icon for park', () {
      expect(MapUtils.getAmenityIcon('park'), Icons.park_rounded);
    });
    test('returns healthcare icon for healthcare', () {
      expect(MapUtils.getAmenityIcon('healthcare'), Icons.medical_services_rounded);
    });
    test('returns transit icon for transit', () {
      expect(MapUtils.getAmenityIcon('transit'), Icons.directions_bus_rounded);
    });
    test('returns charging_station icon for charging_station', () {
      expect(MapUtils.getAmenityIcon('charging_station'), Icons.ev_station_rounded);
    });
    test('returns place icon for unknown type', () {
      expect(MapUtils.getAmenityIcon('unknown'), Icons.place_rounded);
    });
  });

  group('MapUtils.getOverlayColor', () {
    test('returns correct colors for PricePerSquareMeter', () {
      expect(MapUtils.getOverlayColor(6001, MapOverlayMetric.pricePerSquareMeter), ValoraColors.scorePoor);
      expect(MapUtils.getOverlayColor(4501, MapOverlayMetric.pricePerSquareMeter), ValoraColors.scoreAverage);
      expect(MapUtils.getOverlayColor(3001, MapOverlayMetric.pricePerSquareMeter), ValoraColors.scoreGood);
      expect(MapUtils.getOverlayColor(1000, MapOverlayMetric.pricePerSquareMeter), ValoraColors.scoreExcellent);
    });

    test('returns correct colors for CrimeRate (inverted)', () {
      expect(MapUtils.getOverlayColor(101, MapOverlayMetric.crimeRate), ValoraColors.scorePoor);
      expect(MapUtils.getOverlayColor(51, MapOverlayMetric.crimeRate), ValoraColors.scoreAverage);
      expect(MapUtils.getOverlayColor(21, MapOverlayMetric.crimeRate), ValoraColors.scoreGood);
      expect(MapUtils.getOverlayColor(10, MapOverlayMetric.crimeRate), ValoraColors.scoreExcellent);
    });

    test('returns correct colors for default metrics (higher is better)', () {
      // Testing with PopulationDensity as a default case
      expect(MapUtils.getOverlayColor(81, MapOverlayMetric.populationDensity), ValoraColors.scoreExcellent);
      expect(MapUtils.getOverlayColor(61, MapOverlayMetric.populationDensity), ValoraColors.scoreGood);
      expect(MapUtils.getOverlayColor(41, MapOverlayMetric.populationDensity), ValoraColors.scoreAverage);
      expect(MapUtils.getOverlayColor(10, MapOverlayMetric.populationDensity), ValoraColors.scorePoor);
    });
  });

  group('MapUtils.getColorForScore', () {
    test('returns grey for null score', () {
      expect(MapUtils.getColorForScore(null), ValoraColors.neutral400);
    });

    test('returns success for >= 80', () {
      expect(MapUtils.getColorForScore(80), ValoraColors.scoreExcellent);
      expect(MapUtils.getColorForScore(100), ValoraColors.scoreExcellent);
    });

    test('returns warning for >= 60', () {
      expect(MapUtils.getColorForScore(60), ValoraColors.scoreGood);
      expect(MapUtils.getColorForScore(79), ValoraColors.scoreGood);
    });

    test('returns orange for >= 40', () {
      expect(MapUtils.getColorForScore(40), ValoraColors.scoreAverage);
      expect(MapUtils.getColorForScore(59), ValoraColors.scoreAverage);
    });

    test('returns error for < 40', () {
      expect(MapUtils.getColorForScore(39), ValoraColors.scorePoor);
      expect(MapUtils.getColorForScore(0), ValoraColors.scorePoor);
    });
  });
}
