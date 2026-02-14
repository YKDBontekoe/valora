import 'package:latlong2/latlong.dart';

class MapAmenity {
  final String id;
  final String type;
  final String name;
  final LatLng location;
  final Map<String, String>? metadata;

  MapAmenity({
    required this.id,
    required this.type,
    required this.name,
    required this.location,
    this.metadata,
  });

  factory MapAmenity.fromJson(Map<String, dynamic> json) {
    return MapAmenity(
      id: json['id'] ?? '',
      type: json['type'] ?? 'other',
      name: json['name'] ?? 'Amenity',
      location: LatLng(
        (json['latitude'] as num).toDouble(),
        (json['longitude'] as num).toDouble(),
      ),
      metadata: json['metadata'] != null
          ? Map<String, String>.from(json['metadata'])
          : null,
    );
  }
}
