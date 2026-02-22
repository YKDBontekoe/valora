import 'map_overlay.dart';
import 'map_amenity.dart';
import 'map_city_insight.dart';

class MapQueryResponse {
  final String explanation;
  final List<String> followUpQuestions;
  final List<MapOverlay>? overlays;
  final List<MapAmenity>? amenities;
  final List<MapCityInsight>? cityInsights;
  final double? suggestCenterLat;
  final double? suggestCenterLon;
  final double? suggestZoom;

  MapQueryResponse({
    required this.explanation,
    this.followUpQuestions = const [],
    this.overlays,
    this.amenities,
    this.cityInsights,
    this.suggestCenterLat,
    this.suggestCenterLon,
    this.suggestZoom,
  });

  factory MapQueryResponse.fromJson(Map<String, dynamic> json) {
    return MapQueryResponse(
      explanation: json['explanation'] as String? ?? '',
      followUpQuestions: (json['followUpQuestions'] as List<dynamic>?)
              ?.map((e) => e as String)
              .toList() ??
          [],
      overlays: (json['overlays'] as List<dynamic>?)
          ?.map((e) => MapOverlay.fromJson(e as Map<String, dynamic>))
          .toList(),
      amenities: (json['amenities'] as List<dynamic>?)
          ?.map((e) => MapAmenity.fromJson(e as Map<String, dynamic>))
          .toList(),
      cityInsights: (json['cityInsights'] as List<dynamic>?)
          ?.map((e) => MapCityInsight.fromJson(e as Map<String, dynamic>))
          .toList(),
      suggestCenterLat: (json['suggestCenterLat'] as num?)?.toDouble(),
      suggestCenterLon: (json['suggestCenterLon'] as num?)?.toDouble(),
      suggestZoom: (json['suggestZoom'] as num?)?.toDouble(),
    );
  }
}
