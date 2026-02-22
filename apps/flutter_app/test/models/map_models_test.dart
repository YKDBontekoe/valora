import 'package:flutter_test/flutter_test.dart';
import 'package:latlong2/latlong.dart';
import 'package:valora_app/models/map_amenity.dart';
import 'package:valora_app/models/map_overlay.dart';

void main() {
  group('MapAmenity', () {
    test('fromJson handles valid data', () {
      final json = {
        'id': '123',
        'type': 'school',
        'name': 'Test School',
        'latitude': 52.0,
        'longitude': 4.0,
        'metadata': {'key': 'value'},
      };
      final amenity = MapAmenity.fromJson(json);
      expect(amenity.id, '123');
      expect(amenity.location, const LatLng(52.0, 4.0));
      expect(amenity.metadata?['key'], 'value');
    });

    test('fromJson handles null values gracefully', () {
      final json = {'id': null, 'latitude': null, 'longitude': null};
      final amenity = MapAmenity.fromJson(json);
      expect(amenity.id, '');
      expect(amenity.location, const LatLng(0.0, 0.0));
      expect(amenity.type, 'other');
    });
  });

  group('MapOverlay', () {
    test('fromJson handles valid data', () {
      final json = {
        'id': 'BU01',
        'name': 'Neighborhood',
        'metricName': 'Price',
        'metricValue': 123.45,
        'displayValue': '€ 123',
        'geoJson': {'type': 'Feature'},
      };
      final overlay = MapOverlay.fromJson(json);
      expect(overlay.id, 'BU01');
      expect(overlay.metricValue, 123.45);
      expect(overlay.geoJson['type'], 'Feature');
    });

    test('fromJson handles null metricValue', () {
      final json = {'metricValue': null};
      final overlay = MapOverlay.fromJson(json);
      expect(overlay.metricValue, 0.0);
    });
  });
}
