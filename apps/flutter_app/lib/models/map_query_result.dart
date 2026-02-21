import 'map_overlay.dart';

class MapQueryResult {
  final String explanation;
  final MapLocation? targetLocation;
  final MapFilter? filter;

  MapQueryResult({
    required this.explanation,
    this.targetLocation,
    this.filter,
  });

  factory MapQueryResult.fromJson(Map<String, dynamic> json) {
    return MapQueryResult(
      explanation: json['explanation'] as String,
      targetLocation: json['targetLocation'] != null
          ? MapLocation.fromJson(json['targetLocation'])
          : null,
      filter: json['filter'] != null
          ? MapFilter.fromJson(json['filter'])
          : null,
    );
  }
}

class MapLocation {
  final double lat;
  final double lon;
  final double zoom;

  MapLocation({
    required this.lat,
    required this.lon,
    required this.zoom,
  });

  factory MapLocation.fromJson(Map<String, dynamic> json) {
    return MapLocation(
      lat: (json['lat'] as num).toDouble(),
      lon: (json['lon'] as num).toDouble(),
      zoom: (json['zoom'] as num).toDouble(),
    );
  }
}

class MapFilter {
  final MapOverlayMetric? metric;
  final List<String>? amenityTypes;

  MapFilter({
    this.metric,
    this.amenityTypes,
  });

  factory MapFilter.fromJson(Map<String, dynamic> json) {
    return MapFilter(
      metric: json['metric'] != null
          ? MapOverlayMetric.values.firstWhere(
              (e) => e.name.toLowerCase() == (json['metric'] as String).toLowerCase(),
              orElse: () => MapOverlayMetric.pricePerSquareMeter,
            )
          : null,
      amenityTypes: (json['amenityTypes'] as List<dynamic>?)
          ?.map((e) => e as String)
          .toList(),
    );
  }
}
