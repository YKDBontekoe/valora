import 'package:latlong2/latlong.dart';

class MapProperty {
  final String id;
  final double? price;
  final LatLng location;
  final String? status;

  MapProperty({
    required this.id,
    this.price,
    required this.location,
    this.status,
  });

  factory MapProperty.fromJson(Map<String, dynamic> json) {
    return MapProperty(
      id: json['id'],
      price: (json['price'] as num?)?.toDouble(),
      location: LatLng(
        (json['latitude'] as num).toDouble(),
        (json['longitude'] as num).toDouble(),
      ),
      status: json['status'],
    );
  }
}
