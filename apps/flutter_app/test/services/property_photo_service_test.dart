import 'package:flutter_test/flutter_test.dart';
import 'package:valora_app/services/property_photo_service.dart';

void main() {
  group('PropertyPhotoService', () {
    test('builds PDOK WMS image URLs for valid coordinates', () async {
      final service = PropertyPhotoService();

      final urls = await service.getPropertyPhotos(
        latitude: 52.3731,
        longitude: 4.8922,
      );

      expect(urls, hasLength(3));
      expect(urls.first, contains('service.pdok.nl/hwh/luchtfotorgb/wms/v1_0'));
      expect(urls.first, contains('REQUEST=GetMap'));
      expect(urls.first, contains('LAYERS=Actueel_orthoHR'));
      expect(urls.first, contains('CRS=EPSG%3A3857'));
      expect(urls.first, contains('BBOX='));
    });

    test('respects limit parameter', () async {
      final service = PropertyPhotoService();

      final urls = await service.getPropertyPhotos(
        latitude: 52.3731,
        longitude: 4.8922,
        limit: 2,
      );

      expect(urls, hasLength(2));
    });

    test('returns empty for invalid coordinates', () async {
      final service = PropertyPhotoService();

      final urls = await service.getPropertyPhotos(
        latitude: 123.0,
        longitude: 4.8922,
      );

      expect(urls, isEmpty);
    });
  });
}
