import '../../../models/map_amenity.dart';
import '../../../models/map_city_insight.dart';
import '../../../models/map_overlay.dart';
import '../core/http_transport.dart';
import '../mappers/map_api_mapper.dart';

class MapApiClient {
  MapApiClient({required HttpTransport transport}) : _transport = transport;

  final HttpTransport _transport;

  Future<List<MapCityInsight>> getCityInsights() {
    final Uri uri = Uri.parse('${_transport.baseUrl}/map/cities');
    return _transport.get<List<MapCityInsight>>(
      uri: uri,
      responseHandler: (response) => _transport.parseOrThrow(
        response,
        MapApiMapper.parseCityInsights,
      ),
    );
  }

  Future<List<MapAmenity>> getMapAmenities({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    List<String>? types,
  }) {
    final Uri uri = Uri.parse('${_transport.baseUrl}/map/amenities').replace(
      queryParameters: <String, String>{
        'minLat': minLat.toString(),
        'minLon': minLon.toString(),
        'maxLat': maxLat.toString(),
        'maxLon': maxLon.toString(),
        if (types != null) 'types': types.join(','),
      },
    );

    return _transport.get<List<MapAmenity>>(
      uri: uri,
      responseHandler: (response) => _transport.parseOrThrow(
        response,
        MapApiMapper.parseAmenities,
      ),
    );
  }

  Future<List<MapOverlay>> getMapOverlays({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    required String metric,
  }) {
    final Uri uri = Uri.parse('${_transport.baseUrl}/map/overlays').replace(
      queryParameters: <String, String>{
        'minLat': minLat.toString(),
        'minLon': minLon.toString(),
        'maxLat': maxLat.toString(),
        'maxLon': maxLon.toString(),
        'metric': metric,
      },
    );

    return _transport.get<List<MapOverlay>>(
      uri: uri,
      responseHandler: (response) => _transport.parseOrThrow(
        response,
        MapApiMapper.parseOverlays,
      ),
    );
  }
}
