import 'dart:math' as math;

class PropertyPhotoService {
  static const String _baseUrl =
      'https://service.pdok.nl/hwh/luchtfotorgb/wms/v1_0';
  static const String _layer = 'Actueel_orthoHR';
  static const int _imageWidth = 1280;
  static const int _imageHeight = 720;

  Future<List<String>> getPropertyPhotos({
    required double latitude,
    required double longitude,
    int limit = 3,
  }) async {
    if (!_isCoordinateValid(latitude, longitude) || limit <= 0) {
      return const <String>[];
    }

    final (centerX, centerY) = _latLonToWebMercator(latitude, longitude);
    final zoomWindowsInMeters = <double>[40, 90, 180];

    final urls = <String>[];
    for (final halfSize in zoomWindowsInMeters.take(limit)) {
      final bbox = _buildBbox(
        minX: centerX - halfSize,
        minY: centerY - halfSize,
        maxX: centerX + halfSize,
        maxY: centerY + halfSize,
      );

      final uri = Uri.parse(_baseUrl).replace(
        queryParameters: <String, String>{
          'SERVICE': 'WMS',
          'REQUEST': 'GetMap',
          'VERSION': '1.3.0',
          'LAYERS': _layer,
          'STYLES': '',
          'FORMAT': 'image/png',
          'TRANSPARENT': 'false',
          'CRS': 'EPSG:3857',
          'BBOX': bbox,
          'WIDTH': '$_imageWidth',
          'HEIGHT': '$_imageHeight',
        },
      );
      urls.add(uri.toString());
    }

    return urls;
  }

  String _buildBbox({
    required double minX,
    required double minY,
    required double maxX,
    required double maxY,
  }) {
    return '${minX.toStringAsFixed(2)},'
        '${minY.toStringAsFixed(2)},'
        '${maxX.toStringAsFixed(2)},'
        '${maxY.toStringAsFixed(2)}';
  }

  (double, double) _latLonToWebMercator(double latitude, double longitude) {
    const originShift = 20037508.34;
    final x = longitude * originShift / 180.0;
    final y =
        math.log(math.tan((90.0 + latitude) * math.pi / 360.0)) /
        (math.pi / 180.0);
    final mercatorY = y * originShift / 180.0;
    return (x, mercatorY);
  }

  bool _isCoordinateValid(double latitude, double longitude) {
    return latitude >= -90 &&
        latitude <= 90 &&
        longitude >= -180 &&
        longitude <= 180;
  }
}
