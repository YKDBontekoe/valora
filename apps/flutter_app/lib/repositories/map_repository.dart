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
    final uri = Uri.parse('${ApiClient.baseUrl}/map/cities');
    try {
      final response = await _client.requestWithRetry(
        () => _client.authenticatedRequest(
          (headers) =>
              _client.client.get(uri, headers: headers).timeout(ApiClient.timeoutDuration),
        ),
      );

      return _client.handleResponse(
        response,
        (body) {
          final List<dynamic> jsonList = json.decode(body);
          return jsonList.map((e) => MapCityInsight.fromJson(e)).toList();
        },
      );
    } catch (e, stack) {
      throw _client.handleException(e, stack, uri);
    }
  }

  Future<List<MapAmenity>> getMapAmenities({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    List<String>? types,
  }) async {
    final uri = Uri.parse('${ApiClient.baseUrl}/map/amenities').replace(queryParameters: {
      'minLat': minLat.toString(),
      'minLon': minLon.toString(),
      'maxLat': maxLat.toString(),
      'maxLon': maxLon.toString(),
      if (types != null) 'types': types.join(','),
    });
    try {
      final response = await _client.requestWithRetry(
        () => _client.authenticatedRequest(
          (headers) =>
              _client.client.get(uri, headers: headers).timeout(ApiClient.timeoutDuration),
        ),
      );

      return _client.handleResponse(
        response,
        (body) {
          final List<dynamic> jsonList = json.decode(body);
          return jsonList.map((e) => MapAmenity.fromJson(e)).toList();
        },
      );
    } catch (e, stack) {
      throw _client.handleException(e, stack, Uri.parse('${ApiClient.baseUrl}/map/amenities'));
    }
  }

  Future<List<MapAmenityCluster>> getMapAmenityClusters({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    required double zoom,
    List<String>? types,
  }) async {
    final uri = Uri.parse('${ApiClient.baseUrl}/map/amenities/clusters').replace(queryParameters: {
      'minLat': minLat.toString(),
      'minLon': minLon.toString(),
      'maxLat': maxLat.toString(),
      'maxLon': maxLon.toString(),
      'zoom': zoom.toString(),
      if (types != null) 'types': types.join(','),
    });
    try {
      final response = await _client.requestWithRetry(
        () => _client.authenticatedRequest(
          (headers) =>
              _client.client.get(uri, headers: headers).timeout(ApiClient.timeoutDuration),
        ),
      );

      return _client.handleResponse(
        response,
        (body) {
          final List<dynamic> jsonList = json.decode(body);
          return jsonList.map((e) => MapAmenityCluster.fromJson(e)).toList();
        },
      );
    } catch (e, stack) {
      throw _client.handleException(e, stack, Uri.parse('${ApiClient.baseUrl}/map/amenities/clusters'));
    }
  }

  Future<List<MapOverlay>> getMapOverlays({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    required String metric,
  }) async {
    final uri = Uri.parse('${ApiClient.baseUrl}/map/overlays').replace(queryParameters: {
      'minLat': minLat.toString(),
      'minLon': minLon.toString(),
      'maxLat': maxLat.toString(),
      'maxLon': maxLon.toString(),
      'metric': metric,
    });
    try {
      final response = await _client.requestWithRetry(
        () => _client.authenticatedRequest(
          (headers) =>
              _client.client.get(uri, headers: headers).timeout(ApiClient.timeoutDuration),
        ),
      );

      return _client.handleResponse(
        response,
        (body) {
          final List<dynamic> jsonList = json.decode(body);
          return jsonList.map((e) => MapOverlay.fromJson(e)).toList();
        },
      );
    } catch (e, stack) {
      throw _client.handleException(e, stack, Uri.parse('${ApiClient.baseUrl}/map/overlays'));
    }
  }

  Future<List<MapOverlayTile>> getMapOverlayTiles({
    required double minLat,
    required double minLon,
    required double maxLat,
    required double maxLon,
    required double zoom,
    required String metric,
  }) async {
    final uri = Uri.parse('${ApiClient.baseUrl}/map/overlays/tiles').replace(queryParameters: {
      'minLat': minLat.toString(),
      'minLon': minLon.toString(),
      'maxLat': maxLat.toString(),
      'maxLon': maxLon.toString(),
      'zoom': zoom.toString(),
      'metric': metric,
    });
    try {
      final response = await _client.requestWithRetry(
        () => _client.authenticatedRequest(
          (headers) =>
              _client.client.get(uri, headers: headers).timeout(ApiClient.timeoutDuration),
        ),
      );

      return _client.handleResponse(
        response,
        (body) {
          final List<dynamic> jsonList = json.decode(body);
          return jsonList.map((e) => MapOverlayTile.fromJson(e)).toList();
        },
      );
    } catch (e, stack) {
      throw _client.handleException(e, stack, Uri.parse('${ApiClient.baseUrl}/map/overlays/tiles'));
    }
  }
}
