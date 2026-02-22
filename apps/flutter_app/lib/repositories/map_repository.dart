import 'dart:convert';
import '../models/map_city_insight.dart';
import '../models/map_amenity.dart';
import '../models/map_amenity_cluster.dart';
import '../models/map_overlay.dart';
import '../models/map_overlay_tile.dart';
import '../services/api_client.dart';

class MapRepository {
  final ApiClient _client;

  MapRepository(this._client);

  Future<List<MapCityInsight>> getCityInsights() async {
    final response = await _client.get('/map/cities');
    return _client.handleResponse(
      response,
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => MapCityInsight.fromJson(e)).toList();
      },
    );
  }

  Future<List<MapAmenity>> getMapAmenities({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    List<String>? types,
  }) async {
    final response = await _client.get(
      '/map/amenities',
      queryParameters: {
        'minLat': minLat.toString(),
        'minLon': minLon.toString(),
        'maxLat': maxLat.toString(),
        'maxLon': maxLon.toString(),
        if (types != null) 'types': types.join(','),
      },
    );

    return _client.handleResponse(
      response,
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => MapAmenity.fromJson(e)).toList();
      },
    );
  }

  Future<List<MapAmenityCluster>> getMapAmenityClusters({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    required double zoom,
    List<String>? types,
  }) async {
    final response = await _client.get(
      '/map/amenities/clusters',
      queryParameters: {
        'minLat': minLat.toString(),
        'minLon': minLon.toString(),
        'maxLat': maxLat.toString(),
        'maxLon': maxLon.toString(),
        'zoom': zoom.toString(),
        if (types != null) 'types': types.join(','),
      },
    );

    return _client.handleResponse(
      response,
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => MapAmenityCluster.fromJson(e)).toList();
      },
    );
  }

  Future<List<MapOverlay>> getMapOverlays({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    required String metric,
  }) async {
    final response = await _client.get(
      '/map/overlays',
      queryParameters: {
        'minLat': minLat.toString(),
        'minLon': minLon.toString(),
        'maxLat': maxLat.toString(),
        'maxLon': maxLon.toString(),
        'metric': metric,
      },
    );

    return _client.handleResponse(
      response,
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => MapOverlay.fromJson(e)).toList();
      },
    );
  }

  Future<List<MapOverlayTile>> getMapOverlayTiles({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    required double zoom,
    required String metric,
  }) async {
    final response = await _client.get(
      '/map/overlays/tiles',
      queryParameters: {
        'minLat': minLat.toString(),
        'minLon': minLon.toString(),
        'maxLat': maxLat.toString(),
        'maxLon': maxLon.toString(),
        'zoom': zoom.toString(),
        'metric': metric,
      },
    );

    return _client.handleResponse(
      response,
      (body) {
        final List<dynamic> jsonList = json.decode(body);
        return jsonList.map((e) => MapOverlayTile.fromJson(e)).toList();
      },
    );
  }
}
