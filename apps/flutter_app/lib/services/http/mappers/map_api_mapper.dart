import 'dart:convert';

import '../../../models/map_amenity.dart';
import '../../../models/map_city_insight.dart';
import '../../../models/map_overlay.dart';

class MapApiMapper {
  const MapApiMapper._();

  static List<MapCityInsight> parseCityInsights(String body) {
    final List<dynamic> jsonList = json.decode(body) as List<dynamic>;
    return jsonList
        .map((dynamic item) => MapCityInsight.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  static List<MapAmenity> parseAmenities(String body) {
    final List<dynamic> jsonList = json.decode(body) as List<dynamic>;
    return jsonList
        .map((dynamic item) => MapAmenity.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  static List<MapOverlay> parseOverlays(String body) {
    final List<dynamic> jsonList = json.decode(body) as List<dynamic>;
    return jsonList
        .map((dynamic item) => MapOverlay.fromJson(item as Map<String, dynamic>))
        .toList();
  }
}
