import 'dart:convert';
import 'package:flutter/foundation.dart';
import '../models/map_city_insight.dart';
import '../models/map_amenity.dart';
import '../models/map_amenity_cluster.dart';
import '../models/map_overlay.dart';
import '../models/map_overlay_tile.dart';
import '../services/api_client.dart';

// ---------------------------------------------------------------------------
// Isolate-friendly parse functions (must be top-level for compute())
// ---------------------------------------------------------------------------

List<MapCityInsight> _parseCityInsights(String body) {
  final list = json.decode(body) as List<dynamic>;
  return list.map((e) => MapCityInsight.fromJson(e as Map<String, dynamic>)).toList();
}

List<MapAmenity> _parseAmenities(String body) {
  final list = json.decode(body) as List<dynamic>;
  return list.map((e) => MapAmenity.fromJson(e as Map<String, dynamic>)).toList();
}

List<MapAmenityCluster> _parseAmenityClusters(String body) {
  final list = json.decode(body) as List<dynamic>;
  return list.map((e) => MapAmenityCluster.fromJson(e as Map<String, dynamic>)).toList();
}

List<MapOverlay> _parseOverlays(String body) {
  final list = json.decode(body) as List<dynamic>;
  return list.map((e) => MapOverlay.fromJson(e as Map<String, dynamic>)).toList();
}

List<MapOverlayTile> _parseOverlayTiles(String body) {
  final list = json.decode(body) as List<dynamic>;
  return list.map((e) => MapOverlayTile.fromJson(e as Map<String, dynamic>)).toList();
}

// ---------------------------------------------------------------------------

class MapRepository {
  final ApiClient _client;

  /// Session-level cache for city insights. These rarely change (batch job runs
  /// every 30–60 min on the server), so we keep the result for the whole
  /// app session and never re-fetch unless explicitly invalidated.
  List<MapCityInsight>? _cachedCityInsights;

  MapRepository(this._client);

  /// Returns city insights, pulling from a session-level in-memory cache on
  /// subsequent calls. Pass [forceRefresh] to bypass the cache.
  Future<List<MapCityInsight>> getCityInsights({bool forceRefresh = false}) async {
    if (!forceRefresh && _cachedCityInsights != null) {
      return _cachedCityInsights!;
    }
    final response = await _client.get('/map/cities');
    final body = await _client.handleResponse<String>(
      response,
      (body) => body,
    );
    final result = await compute(_parseCityInsights, body);
    _cachedCityInsights = result;
    return result;
  }

  /// Invalidates the session cache so the next [getCityInsights] call fetches fresh data.
  void invalidateCityInsightsCache() => _cachedCityInsights = null;

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

    final body = await _client.handleResponse<String>(
      response,
      (body) => body,
    );
    return compute(_parseAmenities, body);
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

    final body = await _client.handleResponse<String>(
      response,
      (body) => body,
    );
    return compute(_parseAmenityClusters, body);
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

    final body = await _client.handleResponse<String>(
      response,
      (body) => body,
    );
    return compute(_parseOverlays, body);
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

    final body = await _client.handleResponse<String>(
      response,
      (body) => body,
    );
    return compute(_parseOverlayTiles, body);
  }
}
